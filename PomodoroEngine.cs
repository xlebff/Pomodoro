using Spectre.Console;
using System.Diagnostics;

namespace Pomodoro
{
    internal enum PomodoroPhase { Working, Resting }

    internal class PomodoroEngine
    {
        private readonly Stopwatch _stopwatch = new();

        private readonly TimeSpan _workingPhaseDuration;
        private readonly TimeSpan _restingPhaseDuration;
        private readonly TimeSpan? _longRestingPhaseDuration;
        private TimeSpan _accumulatedTime = TimeSpan.Zero;
        private TimeSpan _currentDuration;

        private readonly int _setsCount;
        private readonly int? _setsUntilLongRest;
        private int _currentSet = 0;

        private bool _isActive;
        private bool _isPaused = false;

        public event EventHandler? OnPhaseStart;
        public event EventHandler? OnPhaseOver;
        public event EventHandler? OnPaused;
        public event EventHandler? OnResumed;
        public event EventHandler? OnPomodoroEnd;

        private PomodoroPhase _phase;

        private CancellationTokenSource _cts = new();

        public PomodoroEngine(TimeSpan workingPhaseDuration, 
            TimeSpan restingPhaseDuration, 
            int setsCount,
            TimeSpan? longRestingPhaseDuration,
            int? setsUntilLingRest)
        {
            _workingPhaseDuration = workingPhaseDuration;
            _restingPhaseDuration = restingPhaseDuration;
            _setsCount = setsCount;
            _longRestingPhaseDuration = longRestingPhaseDuration;
            _setsUntilLongRest = setsUntilLingRest;
            _currentDuration = _workingPhaseDuration;
        }

        public PomodoroPhase Phase => _phase;
        public CancellationTokenSource CancellationToken => _cts;
        public TimeSpan CurrentDuration => _currentDuration;
        public TimeSpan Elapsed => _accumulatedTime + _stopwatch.Elapsed;

        public bool ConfCheck()
        {
            if (_setsUntilLongRest is not null &&
                _longRestingPhaseDuration is not null)
            {
                if (!(_setsUntilLongRest > 0 &&
                    _setsUntilLongRest < 50 &&
                    _longRestingPhaseDuration < TimeSpan.FromHours(3) &&
                    _longRestingPhaseDuration > TimeSpan.FromMinutes(3)))
                    return false;
            }

            return (_setsCount > 0 &&
                _setsCount <= 50 &&
                _workingPhaseDuration <= TimeSpan.FromHours(3) &&
                _workingPhaseDuration >= TimeSpan.FromMinutes(1) &&
                _restingPhaseDuration <= TimeSpan.FromHours(1) &&
                _restingPhaseDuration >= TimeSpan.FromMinutes(1));
        }

        private async Task RunTimer(TimeSpan duration, CancellationToken cancellationToken)
        {
            _stopwatch.Start();
            OnPhaseStart?.Invoke(this, EventArgs.Empty);

            while ((_accumulatedTime + _stopwatch.Elapsed) < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _accumulatedTime += _stopwatch.Elapsed;
                    _stopwatch.Stop();
                    return;
                }

                await Task.Delay(100);
            }

            await Task.Delay(300);
            _accumulatedTime = TimeSpan.Zero;
            _stopwatch.Reset();
        }

        public async Task Pause()
        {
            if (!_isPaused)
            {
                _isPaused = true;
                OnPaused?.Invoke(this, EventArgs.Empty);
                _cts.Cancel();
            } else
            {
                _isPaused = false;
                _cts = new();
                OnResumed?.Invoke(this, EventArgs.Empty);
                await RunTimer(_currentDuration, _cts.Token);
            }
        }

        public async Task Start()
        {
            _isActive = true;

            while (_isActive)
            {
                if (_currentSet >= _setsCount)
                {
                    _isActive = false;
                    OnPomodoroEnd?.Invoke(this, EventArgs.Empty);
                    break;
                }

                _phase = PomodoroPhase.Working;
                _currentDuration = _workingPhaseDuration;
                _cts = new();
                await RunTimer(_currentDuration, _cts.Token);
                OnPhaseOver?.Invoke(this, EventArgs.Empty);

                _phase = PomodoroPhase.Resting;

                _currentDuration = _restingPhaseDuration;

                if (_setsUntilLongRest.HasValue && (_currentSet + 1) % _setsUntilLongRest.Value == 0)
                    _currentDuration = (TimeSpan)_longRestingPhaseDuration!;

                _cts = new();
                await RunTimer(_currentDuration, _cts.Token);
                OnPhaseOver?.Invoke(this, EventArgs.Empty);

                ++_currentSet;
            }
        }

        public void Stop()
        {
            _isActive = false;
            _isPaused = false;
            _cts.Cancel();
            OnPomodoroEnd?.Invoke(this, EventArgs.Empty);
        }
    }
}