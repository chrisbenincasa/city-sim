using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Tests.Shell;

public sealed class SimulationThreadTests
{
    [Fact]
    public void Busy_work_never_blocks_a_poll_and_inputs_are_owned_in_order()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var worker = new SimulationThread();
        int caller = Environment.CurrentManagedThreadId;
        int executing = caller;
        var received = new List<Command[]>();
        var hashes = new List<ulong>();
        Command[] commands = [new(CommandKind.Ground, default, default), new(CommandKind.Populate, default, default)];
        Command[] expected = [.. commands];
        void Step(in TickInput input)
        {
            executing = Environment.CurrentManagedThreadId;
            entered.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            received.Add(input.Commands.ToArray());
            hashes.Add(input.RulesetHash);
        }
        worker.Start(Step, new TickInput(commands, 123), 4);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            Assert.True(worker.OwnsWorld);
            Assert.False(worker.TryComplete(out _, out _));
            Assert.Throws<InvalidOperationException>(() => worker.Start(Step, default, 1));
            commands[0] = default;
        }
        finally { release.Set(); }
        Complete(worker, 4);
        Assert.NotEqual(caller, executing);
        Assert.Equal(expected, received[0]);
        Assert.All(received.Skip(1), Assert.Empty);
        Assert.Equal(new ulong[] { 123, 0, 0, 0 }, hashes);
        Assert.False(worker.OwnsWorld);
        worker.Start(Step, default, 1);
        Complete(worker, 1);
    }

    [Fact]
    public void Only_the_creating_thread_can_submit_or_collect_work()
    {
        using var worker = new SimulationThread();
        Exception? submit = null, collect = null;
        static void Step(in TickInput _) { }
        var other = new Thread(() =>
        {
            submit = Record.Exception(() => worker.Start(Step, default, 1));
            collect = Record.Exception(() => worker.TryComplete(out _, out _));
        });
        other.Start();
        Assert.True(other.Join(TimeSpan.FromSeconds(10)));
        Assert.IsType<InvalidOperationException>(submit);
        Assert.IsType<InvalidOperationException>(collect);
        Assert.False(worker.OwnsWorld);
    }

    [Fact]
    public void Worker_failures_reach_the_owner_and_prevent_resubmission()
    {
        using var worker = new SimulationThread();
        static void Fail(in TickInput _) => throw new FormatException("worker failure");
        worker.Start(Fail, default, 1);
        var error = Assert.Throws<FormatException>(() => Complete(worker, 1));
        Assert.Equal("worker failure", error.Message);
        Assert.Contains(nameof(Fail), error.StackTrace);
        Assert.Throws<FormatException>(() => worker.Start(Fail, default, 1));
    }

    [Fact]
    public void Disposal_joins_active_work_and_refuses_more_work()
    {
        int writes = 0;
        var worker = new SimulationThread();
        using var entered = new ManualResetEventSlim();
        void Step(in TickInput _)
        {
            entered.Set();
            Interlocked.Increment(ref writes);
        }
        worker.Start(Step, default, 4);
        Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
        worker.Dispose();
        Assert.Equal(4, writes);
        worker.Dispose();
        Assert.Throws<ObjectDisposedException>(() => worker.Start(Step, default, 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void Threaded_steps_match_replay_at_every_published_boundary(int routeWorkers)
    {
        var loaded = RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "stress-shopping.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var builder = new InputLogBuilder(0, new WorldConfiguration(4000), 0);
        builder.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        var log = builder.Build();
        var serial = Replay.Start(log, loaded.Ruleset!);
        var threaded = Replay.Start(log, loaded.Ruleset!);
        threaded.RouteWorkerCount = routeWorkers;
        using var worker = new SimulationThread();
        long used = 0;
        void Step(in TickInput input)
        {
            threaded.Step(input);
            used += threaded.LastRouteBatch.Used;
        }
        for (ulong tick = 0; tick < 512;)
        {
            int count = (int)Math.Min(512 - tick, tick % 4 + 1);
            worker.Start(Step, new TickInput(log.At(new Ticks(tick)), 0), count);
            for (int i = 0; i < count; i++) serial.Step(new TickInput(log.At(new Ticks(tick + (ulong)i)), 0));
            Complete(worker, count);
            Assert.Equal(serial.World.HashState(), threaded.World.HashState());
            tick += (ulong)count;
        }
        if (routeWorkers > 1) Assert.True(used > 0);
        serial.CheckEndOfRun();
        threaded.CheckEndOfRun();
    }

    private static void Complete(SimulationThread worker, int expected)
    {
        int ticks = 0;
        Assert.True(SpinWait.SpinUntil(() => worker.TryComplete(out _, out ticks), TimeSpan.FromSeconds(20)));
        Assert.Equal(expected, ticks);
    }
}
