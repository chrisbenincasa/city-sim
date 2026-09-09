using System;
using System.Diagnostics;
using System.Globalization;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private Label _fpsLabel = null!;
    private double _performanceSeconds, _stepTime, _shellTime, _renderCpuTime, _renderGpuTime;
    private int _busyFrames;
    private int _performanceFrames, _performanceTicks, _gpuSamples;
    private long _performanceLastFrame;
    private TimeSpan _processCpu;
    private string _performanceReading = "Performance: warming up";
    private readonly string? _performanceLog = System.Environment.GetEnvironmentVariable("BOROUGH_PERFORMANCE_LOG");

    /// <summary>
    /// The frame reading, in the top-right trim. <b>A debug build only.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The instrument keeps running when the label is hidden.</b> Only the reading is a
    /// developer's; <c>BOROUGH_PERFORMANCE_LOG</c> and the tooltip's six lines are what a driven
    /// run collects, and gating the sampling on the build would make a release export unmeasurable.
    /// </remarks>
    private void PerformanceDisplay(Container into)
    {
        _fpsLabel = ConsoleLabel("— FPS", SecondaryPoints);
        _fpsLabel.ThemeTypeVariation = InformationUi.Reading;
        _fpsLabel.MouseFilter = Control.MouseFilterEnum.Stop;
        _fpsLabel.CustomMinimumSize = new Vector2(84, 0);
        _fpsLabel.Visible = OS.IsDebugBuild();
        into.AddChild(_fpsLabel);
        RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
        _performanceLastFrame = Stopwatch.GetTimestamp();
        _processCpu = ProcessCpuTime();
        if (_performanceLog is not null)
            System.IO.File.WriteAllText(Globalize(_performanceLog),
                "tick,frames,seconds,fps,ticks,step_ms,shell_ms,render_cpu_ms,gpu_ms,probe,process_cpu_ms,busy_frames,threaded,route_workers\n");
    }

    private void FinishFrame(long started, double stepMilliseconds, int ticks, bool busy = false)
    {
        double elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        _frameCount++;
        _frameMilliseconds += elapsed;
        _frameMaximum = Math.Max(_frameMaximum, elapsed);
        SamplePerformance(started, stepMilliseconds, ticks, busy);
    }

    private void SamplePerformance(long started, double stepMilliseconds, int ticks, bool busy)
    {
        long now = Stopwatch.GetTimestamp();
        _performanceSeconds += Stopwatch.GetElapsedTime(_performanceLastFrame, now).TotalSeconds;
        _performanceLastFrame = now;
        _performanceFrames++;
        if (busy) _busyFrames++;
        _performanceTicks += ticks;
        _stepTime += stepMilliseconds;
        _shellTime += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        Rid viewport = GetViewport().GetViewportRid();
        _renderCpuTime += RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport);
        double gpu = RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport);
        if (gpu > 0) { _renderGpuTime += gpu; _gpuSamples++; }
        if (_performanceSeconds < 1) return;

        double fps = _performanceFrames / _performanceSeconds;
        TimeSpan cpu = ProcessCpuTime();
        double processMilliseconds = (cpu - _processCpu).TotalMilliseconds;
        _processCpu = cpu;
        string gpuReading = _gpuSamples == 0 ? "unavailable" : $"{_renderGpuTime / _gpuSamples:F2} ms/frame";
        _fpsLabel.Text = $"{fps:F0} FPS";
        _performanceReading = $"FPS {fps:F1} · frame interval {1000 / fps:F2} ms\n"
            + $"Process CPU {processMilliseconds / _performanceSeconds / 10:F1}% (100% = one core)\n"
            + $"Simulation {_stepTime / _performanceSeconds:F2} ms/s · {_performanceTicks / _performanceSeconds:F1} Ticks/s\n"
            + $"Shell CPU {_shellTime / _performanceFrames:F2} ms/frame\n"
            + $"Render CPU {_renderCpuTime / _performanceFrames:F2} ms/frame · GPU {gpuReading}\n"
            + $"Frame limit {(Engine.MaxFps == 0 ? "unlimited" : Engine.MaxFps.ToString())} · VSync {DisplayServer.WindowGetVsyncMode()}";
        _fpsLabel.TooltipText = _performanceReading;
        if (_performanceLog is not null)
            System.IO.File.AppendAllText(Globalize(_performanceLog), string.Create(CultureInfo.InvariantCulture,
                $"{_presentedTick},{_performanceFrames},{_performanceSeconds:F6},{fps:F3},{_performanceTicks},{_stepTime:F4},{_shellTime:F4},{_renderCpuTime / _performanceFrames:F4},{(_gpuSamples == 0 ? "" : (_renderGpuTime / _gpuSamples).ToString("F4", CultureInfo.InvariantCulture))},{_renderProbe},{processMilliseconds:F4},{_busyFrames},{_threaded},{_routeWorkers}\n"));
        _performanceSeconds = _stepTime = _shellTime = _renderCpuTime = _renderGpuTime = 0;
        _performanceFrames = _performanceTicks = _gpuSamples = _busyFrames = 0;
    }

    private static TimeSpan ProcessCpuTime()
    {
        using Process process = Process.GetCurrentProcess();
        return process.TotalProcessorTime;
    }
}
