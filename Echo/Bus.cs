using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;

namespace Echo;

public class Bus
{
    private readonly IDictionary<Type, ICollection<Channel<Event>>> _channels;
    private readonly IDictionary<Guid, Channel<Result>> _results;

    public int TypeCount => _channels.Count;

    public Bus()
    {
        _channels = new ConcurrentDictionary<Type, ICollection<Channel<Event>>>();
        _results = new ConcurrentDictionary<Guid, Channel<Result>>();
    }

    private void RegisterEventType(Type type)
    {
        if (!_channels.ContainsKey(type))
        {
            _channels[type] = new List<Channel<Event>>();
        }
    }

    private ChannelReader<Event> AcquireChannel(Type type)
    {
        var channel = Channel.CreateUnbounded<Event>();
        _channels[type].Add(channel);
        return channel.Reader;
    }

    public async Task Publish(Event e)
    {
        var type = e.GetType();
        RegisterEventType(type);
        foreach (var channel in _channels[type])
        {
            await channel.Writer.WriteAsync(e);
        }
    }

    public async Task Publish(Command c)
    {
        await Publish(c as Event);
        var channel = Channel.CreateBounded<Result>(1);
        _results[c.Id] = channel;
    }

    public async Task Publish(Result r)
    {
        await Publish(r as Event);
        var channel = _results[r.CommandId];
        await channel.Writer.WriteAsync(r);
        channel.Writer.Complete();
    }

    public async Task<Result> Await(Command c)
    {
        var channel = _results[c.Id];
        var result = await channel.Reader.ReadAsync();
        _results.Remove(c.Id);
        return result;
    }

    public async Task<Result> Suspend(Command c)
    {
        await Publish(c);
        return await Await(c);
    }

    public async void Subscribe(Type type, Func<Event, Task> action, Func<Event, Exception, Task> onException)
    {
        RegisterEventType(type);
        await foreach (var e in AcquireChannel(type).ReadAllAsync())
        {
            try
            {
                var task = action.Invoke(e);
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var task = onException.Invoke(e, ex);
                await task.ConfigureAwait(false);
            }
        }
    }

    public void RegisterHandlers(
        IEnumerable<Type> types,
        Func<Type, object> instanceProvider
    )
    {
        foreach (var type in types)
        {
            if (!Attribute.IsDefined(type, typeof(Handler))) continue;
            var handlerInstance = instanceProvider.Invoke(type);
            var handleMethods = type.GetMethods()
                .Where(x => Attribute.IsDefined(x, typeof(HandlesEvent)));
            var exHandleMethods = type.GetMethods()
                .Where(x => Attribute.IsDefined(x, typeof(HandlesExceptions)));
            var exHandleMethodsDictionary = new Dictionary<Type, MethodInfo>();
            MethodInfo? defaultExHandler = null;
            foreach (var exHandleMethod in exHandleMethods)
            {
                var attr = (exHandleMethod.GetCustomAttribute(typeof(HandlesExceptions)) as HandlesExceptions)!;
                if (attr.Types.Length == 0)
                {
                    defaultExHandler = exHandleMethod;
                }
                else
                {
                    foreach (var t in attr.Types)
                    {
                        exHandleMethodsDictionary[t] = exHandleMethod;
                    }
                }
            }
            foreach (var handleMethod in handleMethods)
            {
                var eventType = handleMethod.GetParameters()[0].ParameterType;
                MethodInfo exHandleMethod;
                if (exHandleMethodsDictionary.ContainsKey(eventType))
                {
                    exHandleMethod = exHandleMethodsDictionary[eventType];
                }
                else
                {
                    exHandleMethod = defaultExHandler ?? throw new InvalidOperationException();
                }
                Subscribe(
                    eventType,
                    async e => { await (Task)handleMethod.Invoke(handlerInstance, new object[] { e })!; },
                    async (e, ex) => { await (Task)exHandleMethod.Invoke(handlerInstance, new object[] { e, ex })!; }
                );
            }
        }
    }
}