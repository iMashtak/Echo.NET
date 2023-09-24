namespace Echo.UnitTests;

public class Tests
{
    private Bus _bus;

    [SetUp]
    public void SetUp()
    {
        _bus = new Bus();
    }

    [Test]
    public async Task TestPublishSubscribe()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestEventA),
            async e => { Interlocked.Increment(ref counter); },
            async (e, ex) => { }
        );
        Thread.Sleep(100);
        await _bus.Publish(new TestEventA());
        Thread.Sleep(100);
        Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public async Task TestPublishSubscribeWithException()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestEventA),
            async e => { throw new Exception(); },
            async (e, ex) => { Interlocked.Increment(ref counter); }
        );
        Thread.Sleep(100);
        await _bus.Publish(new TestEventA());
        Thread.Sleep(100);
        Assert.That(counter, Is.EqualTo(1));
    }

    [Test]
    public async Task TestSuspend()
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
        Thread.Sleep(100);
        var result = await _bus.Suspend(new TestCommandA());
        Thread.Sleep(100);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(counter, Is.EqualTo(11));
    }
    
    [Test]
    public async Task TestSuspendWithFailure()
    {
        var counter = 0;
        _bus.Subscribe(typeof(TestCommandA),
            async e => { throw new Exception(); },
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
        Thread.Sleep(100);
        var result = await _bus.Suspend(new TestCommandA());
        Thread.Sleep(100);
        Assert.That(result.IsFailure, Is.True);
        Assert.That(counter, Is.EqualTo(100));
    }
}