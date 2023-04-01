namespace Echo;

public class Flow
{
    private Guid Id { get; }
    private DateTime CreatedAt { get; }
    private object? Creator { get; }

    public Flow()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        Creator = null;
    }

    public Flow(object creator)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        Creator = creator;
    }
}

public abstract class Event
{
    public Guid Id { get; }
    public DateTime CreatedAt { get; }
    public Flow Flow { get; }

    public Event()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        Flow = new Flow();
    }

    public Event(Event parent)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        Flow = parent.Flow;
    }

    public Event(Flow flow)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        Flow = flow;
    }
}

public abstract class Command : Event
{
    
}

public abstract class Result : Event
{
    public Guid CommandId { get; }

    public Result(Command command) : base(command)
    {
        CommandId = command.Id;
    }

    public bool IsSuccess => GetType().IsAssignableTo(typeof(Success));
    public bool IsFailure => GetType().IsAssignableTo(typeof(Failure));
}

public abstract class Failure : Result
{
    protected Failure(Command command) : base(command)
    {
    }
}

public abstract class Success : Result
{
    protected Success(Command command) : base(command)
    {
    }
}