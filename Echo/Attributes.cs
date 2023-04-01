namespace Echo;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class Handler : Attribute
{
    
}

[AttributeUsage(AttributeTargets.Method)]
public class HandlesEvent : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class HandlesExceptions : Attribute
{
    public Type[] Types { get; }

    public HandlesExceptions(params Type[] types)
    {
        Types = types;
    }
}