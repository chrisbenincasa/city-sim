using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Shell;

internal sealed class CityPreparation : IDisposable
{
    private readonly CancellationTokenSource _cancel = new();
    private readonly Task<Simulation> _work;
    private Task _checked = Task.CompletedTask;
    private long _tick;
    private volatile bool _reading;
    private bool _disposed;

    public CityPreparation(int citizens, Ruleset rules, ulong rulesetHash, WorldKey key, bool empty, ulong until, int routeWorkers)
    {
        _work = Start(() =>
        {
            var world = new World(citizens, rules, key) { Changes = new WorldChanges() };
            var simulation = new Simulation(world, key)
            { VerifyDecideWritesNothing = false, RouteWorkerCount = routeWorkers };
            _cancel.Token.ThrowIfCancellationRequested();
            var boot = new Command(empty ? CommandKind.Ground : CommandKind.Populate, default, default);
            simulation.Step(new TickInput([boot], rulesetHash));
            Interlocked.Exchange(ref _tick, 1);
            StepTo(simulation, until, rulesetHash);
            return simulation;
        });
    }

    /// <summary>Reads a saved city, then steps it to <paramref name="until"/> when one is given.</summary>
    /// <remarks>A Tick before the saved one is refused; a saved city is never rewound.</remarks>
    public CityPreparation(string path, ulong? until, int routeWorkers)
    {
        _reading = true;
        _work = Start(() =>
        {
            SavedCity city = CitySave.Read(path);
            ulong saved = city.World.Tick.Raw;
            if (until < saved)
                throw new InvalidDataException(
                    $"--start-at {until} is before Tick {saved}, where the saved city begins.");
            var simulation = new Simulation(city.World, city.Header.Key)
            { VerifyDecideWritesNothing = false, RouteWorkerCount = routeWorkers };
            if ((until ?? saved) == saved)
            {
                // The market rows are the one cache the checks fill lazily. Built here, the check
                // only reads, so the shell can build its scene from the same World meanwhile.
                city.World.Markets.Rebuild(city.World);
                _checked = Task.Run(simulation.CheckEndOfRun);
            }
            else
            {
                simulation.CheckEndOfRun();
            }
            city.World.Changes = new WorldChanges();
            City = city;
            SavedTick = saved;
            Interlocked.Exchange(ref _tick, unchecked((long)saved));
            _reading = false;
            StepTo(simulation, until ?? saved, city.Header.RulesetInForce);
            city.World.Changes.Invalidate();
            return simulation;
        });
    }

    /// <summary>
    /// The end-of-run invariant check of a saved city that is not stepped on load. It may still be
    /// running when <see cref="Complete"/> returns, and no Tick may be stepped until it finishes.
    /// </summary>
    public Task Checked => _checked;

    /// <summary>True while a saved city is still being read.</summary>
    public bool Reading => _reading;

    /// <summary>The saved city this preparation read, once reading has finished.</summary>
    public SavedCity? City { get; private set; }

    /// <summary>The Tick the saved city was written at.</summary>
    public ulong SavedTick { get; private set; }

    private Task<Simulation> Start(Func<Simulation> work) => Task.Factory.StartNew(() =>
    {
        _cancel.Token.ThrowIfCancellationRequested();
        Simulation simulation = work();
        _cancel.Token.ThrowIfCancellationRequested();
        return simulation;
    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private void StepTo(Simulation simulation, ulong until, ulong rulesetHash)
    {
        while (simulation.World.Tick.Raw < until)
        {
            _cancel.Token.ThrowIfCancellationRequested();
            simulation.Step(new TickInput([], rulesetHash));
            Interlocked.Exchange(ref _tick, unchecked((long)simulation.World.Tick.Raw));
        }
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
