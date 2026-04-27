using SimplePomodoro.Infrastructure;
using SimplePomodoro.Models;
using System.Diagnostics;

namespace SimplePomodoro.Core;

internal class Engine(ApplicationConfig appConfig,
    PomodoroConfig pomodoroConfig)
{
    private ApplicationConfig _appConfig = appConfig;
    private PomodoroConfig _pomodoroConfig = pomodoroConfig;

    private readonly Stopwatch _stopwatch = new();
    private int _currentCycle = 0;

    private CancellationTokenSource? _cts;

    private bool _isCountdown;

    public async Task StartAsync()
    {
        _ = Task.Run(Handler.Init);
        Console.Clear();
        Console.CursorVisible = false;

        while (_currentCycle < _pomodoroConfig.CyclesCount)
        {
            _cts?.Dispose();
            _cts = new();

            await RunPhaseAsync("Working", TimeSpan.FromMinutes(_pomodoroConfig.WorkPhaseDuration), _cts.Token);

            _cts?.Dispose();
            _cts = new();

            TimeSpan breakDuration = TimeSpan.FromMinutes(_pomodoroConfig.BreakPhaseDuration);

            if (_pomodoroConfig.CyclesBeforeLongBreak != 0 &&
                _pomodoroConfig.LongBreakDuration != 0)
            {
                if (_currentCycle % _pomodoroConfig.CyclesBeforeLongBreak == 0)
                {
                    breakDuration = TimeSpan.FromMinutes(_pomodoroConfig.LongBreakDuration);
                }
            }

            await RunPhaseAsync("Break", breakDuration, _cts.Token);

            ++_currentCycle;
        }
    }

    private async Task RunPhaseAsync(string phase, TimeSpan duration, CancellationToken token)
    {
        OnPhaseStart?.Invoke(null, EventArgs.Empty);

        _stopwatch.Restart();

        Console.Clear();
        Console.WriteLine(phase);

        int startTopPosition = Console.CursorTop;

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

    public void Skip(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    public void Pause(object? sender, EventArgs e)
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Stop();
        else _stopwatch.Start();
    }

    public event EventHandler? OnPhaseStart;
    public event EventHandler? OnPhaseEnd;
    public event EventHandler? OnCountdown;
}
