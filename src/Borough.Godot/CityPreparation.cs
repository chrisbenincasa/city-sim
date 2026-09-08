using System;
using System.Threading;
using System.Threading.Tasks;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;

namespace Borough.Shell;

internal sealed class CityPreparation : IDisposable
{
    private readonly CancellationTokenSource _cancel = new();
    private readonly Task<Simulation> _work;
    private long _tick;
    private bool _disposed;

    public CityPreparation(int citizens, Ruleset rules, WorldKey key, bool empty, ulong until, int routeWorkers)
    {
        _work = Task.Factory.StartNew(() =>
        {
            _cancel.Token.ThrowIfCancellationRequested();
            var world = new World(citizens, rules, key) { Changes = new WorldChanges() };
            var simulation = new Simulation(world, key)
                { VerifyDecideWritesNothing = false, RouteWorkerCount = routeWorkers };
            _cancel.Token.ThrowIfCancellationRequested();
            var boot = new Command(empty ? CommandKind.Ground : CommandKind.Populate, default, default);
            simulation.Step(new TickInput([boot], 0));
            Interlocked.Exchange(ref _tick, 1);
            while (world.Tick.Raw < until)
            {
                _cancel.Token.ThrowIfCancellationRequested();
                simulation.Step(default);
                Interlocked.Exchange(ref _tick, unchecked((long)world.Tick.Raw));
            }
            _cancel.Token.ThrowIfCancellationRequested();
            return simulation;
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public ulong Tick => unchecked((ulong)Interlocked.Read(ref _tick));
    public bool IsCompleted => _work.IsCompleted;
    public void Cancel() => _cancel.Cancel();
    public Simulation Complete()
    {
        if (!IsCompleted) throw new InvalidOperationException("The city is still being prepared.");
        return _work.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancel.Cancel();
        try { _work.GetAwaiter().GetResult(); }
        catch (Exception) { /* Completion reports failures; shutdown still releases the worker. */ }
        _cancel.Dispose();
    }
}
