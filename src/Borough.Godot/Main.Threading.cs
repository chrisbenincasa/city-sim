using System;
using System.Collections.Generic;
using Borough.Core;
using Borough.Core.Entities;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private SimulationThread? _stepThread;
    private readonly Queue<Action> _atBoundary = new();
    private World _ownedWorld = null!;
    private Simulation _ownedSimulation = null!;
    private ulong _presentedTick;
    private int _routeWorkers = 1;
    private int _batchTicks = 1;
    private bool _threaded = true;

    // These guards cover every existing shell reader, including inspector and signal callbacks.
    private World _world
    {
        get { RequireWorld(); return _ownedWorld; }
        set { RequireWorld(); _ownedWorld = value; }
    }
    private Simulation _simulation
    {
        get { RequireWorld(); return _ownedSimulation; }
        set { RequireWorld(); _ownedSimulation = value; }
    }

    private void RequireWorld()
    {
        if (_stepThread?.OwnsWorld == true)
            throw new InvalidOperationException("The simulation thread owns the World until its batch is collected.");
    }

    private void AtBoundary(Action action)
    {
        if (_stopping || _preparation is not null) return;
        if (_stepThread?.OwnsWorld == true) _atBoundary.Enqueue(action);
        else action();
    }

    private void DeferredClick(InputEventMouseButton button)
    {
        var copy = (InputEventMouseButton)button.Duplicate();
        Transform3D camera = _camera.GlobalTransform;
        AtBoundary(() =>
        {
            Transform3D current = _camera.GlobalTransform;
            try
            {
                _camera.GlobalTransform = camera;
                _UnhandledInput(copy);
            }
            finally { _camera.GlobalTransform = current; copy.Dispose(); }
        });
    }

    private bool ThreadArguments()
    {
        string[] args = OS.GetCmdlineUserArgs();
        for (int at = 0; at < args.Length; at++)
        {
            if (args[at] == "--main-thread-sim") _threaded = false;
            if (args[at] != "--route-workers") continue;
            if (++at < args.Length && int.TryParse(args[at], out _routeWorkers)
                && _routeWorkers is >= 1 and <= 8) continue;
            GD.PrintErr("--route-workers requires an integer from 1 through 8 (including the simulation thread).");
            return false;
        }
        return true;
    }
}
