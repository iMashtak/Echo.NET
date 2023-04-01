namespace Echo.Tests;

public class TestEventA : Event
{
}

public class TestEventB : Event
{
}

public class TestCommandA : Command
{
}

public class TestSuccessA : Success
{
    public TestSuccessA(TestCommandA command) : base(command)
    {
    }
}

public class TestFailureA : Failure
{
    public TestFailureA(TestCommandA command) : base(command)
    {
    }
}

[Handler]
public interface ITestEventsHandler
{
    [HandlesEvent]
    public Task Handle(TestEventA e);

    [HandlesEvent]
    public Task Handle(TestEventB e);

    [HandlesExceptions]
    public Task HandleException(Event e, Exception ex);

    [HandlesEvent]
    public Task Handle(TestCommandA c);

    [HandlesExceptions(typeof(TestCommandA))]
    public Task HandleException(TestCommandA c, Exception ex);
}

public class TestEventsHandler : ITestEventsHandler
{
    private Bus Bus { get; }

    public TestEventsHandler(Bus bus)
    {
        Bus = bus;
    }

    public async Task Handle(TestEventA e)
    {
        Console.WriteLine("TestEventA");
        await Bus.Publish(new TestEventB());
        throw new Exception();
    }

    public async Task Handle(TestEventB e)
    {
        Console.WriteLine("TestEventB");
    }

    public async Task HandleException(Event e, Exception ex)
    {
        Console.WriteLine("HandleException Default");
    }

    public async Task Handle(TestCommandA c)
    {
        Console.WriteLine("TestCommandA");
        await Bus.Publish(new TestSuccessA(c));
    }

    public async Task HandleException(TestCommandA c, Exception ex)
    {
        Console.WriteLine("HandleException TestCommandA");
    }
}