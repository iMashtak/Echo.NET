using LightInject;

namespace Echo.Tests;

public class Tests : IClassFixture<LightInjectContainerFixture>
{
    private ServiceContainer _container;
    private Bus _bus;

    public Tests(LightInjectContainerFixture fixture)
    {
        _container = fixture.Container;
        _bus = new Bus();
    }

    [Fact]
    public void TestRegistration()
    {
        _bus.RegisterHandlers(
            new[] { typeof(ITestEventsHandler) },
            type => _container.GetInstance(type)
        );
        Assert.Equal(3, _bus.TypeCount);
    }

    [Fact]
    public async void TestPublishSubscribe()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestEventA),
            async e => { Interlocked.Increment(ref counter); },
            async (e, ex) => { }
        );
        Thread.Sleep(10);
        await _bus.Publish(new TestEventA());
        Thread.Sleep(10);
        Assert.Equal(1, counter);
    }

    [Fact]
    public async void TestPublishSubscribeWithException()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestEventA),
            async e => { throw new Exception(); },
            async (e, ex) => { Interlocked.Increment(ref counter); }
        );
        Thread.Sleep(10);
        await _bus.Publish(new TestEventA());
        Thread.Sleep(10);
        Assert.Equal(1, counter);
    }

    [Fact]
    public async void TestSuspend()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestCommandA),
            async e =>
            {
                Interlocked.Increment(ref counter);
                _bus.Publish(new TestSuccessA((TestCommandA)e));
            },
            async (e, ex) => { }
        );
        _bus.Subscribe(typeof(TestSuccessA),
            async e => { Interlocked.Add(ref counter, 10); },
            async (e, ex) => { }
        );
        Thread.Sleep(10);
        var result = await _bus.Suspend(new TestCommandA());
        Thread.Sleep(10);
        Assert.True(result.IsSuccess);
        Assert.Equal(11, counter);
    }

    [Fact]
    public async void TestSuspendWithFailure()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestCommandA),
            async e => { throw new Exception();},
            async (e, ex) => { await _bus.Publish(new TestFailureA((TestCommandA)e)); }
        );
        _bus.Subscribe(typeof(TestSuccessA),
            async e => { Interlocked.Add(ref counter, 10); },
            async (e, ex) => { }
        );
        _bus.Subscribe(typeof(TestFailureA),
            async e => { Interlocked.Add(ref counter, 100); },
            async (e, ex) => { }
        );
        Thread.Sleep(10);
        var result = await _bus.Suspend(new TestCommandA());
        Thread.Sleep(10);
        Assert.True(result.IsFailure);
        Assert.Equal(100, counter);
    }
}

public class LightInjectContainerFixture : IDisposable
{
    public ServiceContainer Container { get; }

    public LightInjectContainerFixture()
    {
        Container = new ServiceContainer();
        Container.Register<ITestEventsHandler, TestEventsHandler>();
        Container.RegisterInstance(typeof(Bus), new Bus());
    }

    public void Dispose()
    {
    }
}