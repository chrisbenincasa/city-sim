using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using Borough.Core;
using Borough.Core.Input;

namespace Borough.Shell;

// A single outstanding batch transfers ownership until TryComplete returns it. No Godot calls
// or World readers run here; the shell retains its prepared scene during the handoff.
internal delegate void SimulationStep(in TickInput input);

internal sealed class SimulationThread : IDisposable
{
    private readonly AutoResetEvent _ready = new(false);
    private readonly Thread _thread;
    private readonly int _caller = Environment.CurrentManagedThreadId;
    private SimulationStep? _step;
    private Command[] _commands = [];
    private ulong _rulesetHash;
    private int _ticks, _state;
    private double _milliseconds;
    private ExceptionDispatchInfo? _error;
    private volatile bool _stopping;
    private bool _disposed;

    public SimulationThread()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "Borough simulation" };
        _thread.Start();
    }

    public bool OwnsWorld => Volatile.Read(ref _state) != 0;

    public void Start(SimulationStep step, TickInput input, int ticks)
    {
        Caller();
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(step);
        if (ticks is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(ticks));
        _error?.Throw();
        if (OwnsWorld) throw new InvalidOperationException("Collect the preceding batch first.");
        _step = step;
        _commands = input.Commands.ToArray();
        _rulesetHash = input.RulesetHash;
        _ticks = ticks;
        Volatile.Write(ref _state, 1);
        _ready.Set();
    }

    public bool TryComplete(out double milliseconds, out int ticks)
    {
        Caller();
        milliseconds = 0;
        ticks = 0;
        if (Volatile.Read(ref _state) != 2) return false;
        milliseconds = _milliseconds;
        ticks = _ticks;
        _step = null;
        _commands = [];
        Volatile.Write(ref _state, 0);
        _error?.Throw();
        return true;
    }

    private void Run()
    {
        while (true)
        {
            _ready.WaitOne();
            if (_stopping) return;
            long started = Stopwatch.GetTimestamp();
            try
            {
                for (int tick = 0; tick < _ticks; tick++) _step!(tick == 0 ? new TickInput(_commands, _rulesetHash) : default);
            }
            catch (Exception error) { _error = ExceptionDispatchInfo.Capture(error); }
            _milliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Volatile.Write(ref _state, 2);
        }
    }

    private void Caller()
    {
        if (Environment.CurrentManagedThreadId != _caller)
            throw new InvalidOperationException("Only the shell owner may hand off the simulation.");
    }

    public void Dispose()
    {
        Caller();
        if (_disposed) return;
        _disposed = true;
        _stopping = true;
        _ready.Set();
        _thread.Join(); // Shutdown only; ordinary frames never wait for Step.
        _ready.Dispose();
    }
}
