using SimplePomodoro.Infrastructure;
using SimplePomodoro.Models;
using System.Diagnostics;

namespace SimplePomodoro.Core;

/// <summary>
///     Core Pomodoro engine that manages the timing of work and break phases.
///     Handles phase execution, cancellation, pause/resume, and events for UI/audio updates.
/// </summary>
internal class Engine
{
    // --- Fields ---
    private readonly ApplicationConfig _appConfig;
    private readonly PomodoroConfig _pomodoroConfig;

    private readonly Stopwatch _stopwatch = new();
    private int _currentCycle = 0;

    private CancellationTokenSource? _cts;
    private bool _isCountdown = false;

    // --- Events ---
    /// <summary>Raised when a new phase (work or break) starts.</summary>
    public event EventHandler? OnPhaseStart;

    /// <summary>Raised when a phase ends naturally or is skipped.</summary>
    public event EventHandler? OnPhaseEnd;

    /// <summary>Raised during the last 4 seconds of a phase to trigger countdown sounds/effects.</summary>
    public event EventHandler? OnCountdown;

    // --- Constructor ---
    /// <summary>
    ///     Initializes a new instance of the <see cref="Engine"/> class.
    /// </summary>
    /// <param name="appConfig">Application configuration (volumes, music dir, etc.).</param>
    /// <param name="pomodoroConfig">Pomodoro‑specific settings (durations, cycles).</param>
    public Engine(ApplicationConfig appConfig, PomodoroConfig pomodoroConfig)
    {
        _appConfig = appConfig;
        _pomodoroConfig = pomodoroConfig;
    }

    // --- Public methods ---
    /// <summary>
    ///     Starts the Pomodoro session. Runs work and break phases sequentially
    ///     according to the configured cycle count and long‑break interval.
    /// </summary>
    public async Task StartAsync()
    {
        _ = Task.Run(Handler.Init);
        Console.Clear();
        Console.CursorVisible = false;

        while (_currentCycle < _pomodoroConfig.CyclesCount)
        {
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // Work phase
            await RunPhaseAsync("Working", TimeSpan.FromMinutes(_pomodoroConfig.WorkPhaseDuration), _cts.Token);

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // Determine break duration (short or long)
            TimeSpan breakDuration = TimeSpan.FromMinutes(_pomodoroConfig.BreakPhaseDuration);

            if (_pomodoroConfig.CyclesBeforeLongBreak != 0 && _pomodoroConfig.LongBreakDuration != 0)
            {
                if (_currentCycle % _pomodoroConfig.CyclesBeforeLongBreak == 0)
                {
                    breakDuration = TimeSpan.FromMinutes(_pomodoroConfig.LongBreakDuration);
                }
            }

            // Break phase
            await RunPhaseAsync("Break", breakDuration, _cts.Token);

            ++_currentCycle;
        }
    }

    /// <summary>
    ///     Skips the current phase. Called when user requests to skip (e.g., via keyboard).
    /// </summary>
    public void Skip(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    /// <summary>
    ///     Pauses or resumes the current phase timer. Called on user pause toggle.
    /// </summary>
    public void Pause(object? sender, EventArgs e)
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Stop();
        else
            _stopwatch.Start();
    }

    // --- Private helper methods ---
    /// <summary>
    ///     Executes a single phase (work or break) with live console display of remaining time.
    ///     Supports cancellation via <see cref="CancellationToken"/> and triggers countdown events.
    /// </summary>
    /// <param name="phase">Name of the phase (e.g., "Working" or "Break").</param>
    /// <param name="duration">Total duration of the phase.</param>
    /// <param name="token">Cancellation token to allow skipping the phase.</param>
    private async Task RunPhaseAsync(string phase, TimeSpan duration, CancellationToken token)
    {
        OnPhaseStart?.Invoke(null, EventArgs.Empty);

        _stopwatch.Restart();
        Console.Clear();
        Console.WriteLine(phase + " – " + duration.ToString(@"mm\:ss"));

        int startTopPosition = Console.CursorTop;

        Console.WriteLine($"\nCycle – {_currentCycle + 1}/{_pomodoroConfig.CyclesCount}");

        _isCountdown = false;

        while (_stopwatch.Elapsed < duration)
        {
            if (token.IsCancellationRequested)
            {
                Console.Clear();
                _stopwatch.Reset();
                _isCountdown = false;
                OnPhaseEnd?.Invoke(null, EventArgs.Empty);
                return;
            }

            // Trigger countdown event during the last 4 seconds
            if ((duration - _stopwatch.Elapsed).TotalSeconds <= 4 && !_isCountdown)
            {
                OnCountdown?.Invoke(null, EventArgs.Empty);
                _isCountdown = true;
            }

            Console.CursorTop = startTopPosition;
            Console.WriteLine((duration - _stopwatch.Elapsed).ToString(@"mm\:ss"));

            await Task.Delay(100, CancellationToken.None);
        }

        _stopwatch.Reset();
        _isCountdown = false;
        OnPhaseEnd?.Invoke(null, EventArgs.Empty);
    }
}