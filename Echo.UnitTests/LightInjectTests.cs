using LightInject;

namespace Echo.UnitTests;

public class LightInjectTests
{
    private Bus _bus;

    private ServiceContainer _container;

    [SetUp]
    public void SetUp()
    {
        _bus = new Bus();
        _container = new ServiceContainer();
        _container.Register<ITestEventsHandler, TestEventsHandler>();
        _container.RegisterInstance(typeof(Bus), _bus);
    }

    [Test]
    public void Test()
    {
        _bus.RegisterHandlers(
            new[] { typeof(ITestEventsHandler) },
            type => _container.GetInstance(type)
        );
        Assert.That(_bus.TypeCount, Is.EqualTo(3));
    }
}