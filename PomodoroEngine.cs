using System.Diagnostics;

namespace Pomodoro
{
    internal enum PomodoroPhase { Working, Resting }
    internal enum PomodoroState { Running, Paused, Completed, Interrupted }

    internal class PomodoroEngine
    {
        private readonly Stopwatch _stopwatch = new();

        private readonly TimeSpan _workingPhaseDuration;
        private readonly TimeSpan _restingPhaseDuration;
        private readonly TimeSpan? _longRestingPhaseDuration;
        private TimeSpan _currentDuration;

        private readonly int _setsCount;
        private readonly int? _setsUntilLongRest;
        private int _currentSet = 0;

        private bool _isActive;
        private bool _isPaused = false;
        private bool _isComplete = true;

        private PomodoroPhase _phase;

        private readonly CancellationTokenSource _globalCTS = new();
        private CancellationTokenSource? _cts = new();


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
        public TimeSpan CurrentDuration => _currentDuration;
        public TimeSpan Elapsed => _stopwatch.Elapsed;
        public bool IsCompleted => _isComplete;


        public bool ConfCheck()
        {
            //if (_setsUntilLongRest is not null &&
            //    _longRestingPhaseDuration is not null)
            //{
            //    if (!(_setsUntilLongRest > 0 &&
            //        _setsUntilLongRest < 50 &&
            //        _longRestingPhaseDuration < TimeSpan.FromHours(3) &&
            //        _longRestingPhaseDuration > TimeSpan.FromMinutes(3)))
            //        return false;
            //}

            //return (_setsCount > 0 &&
            //    _setsCount <= 50 &&
            //    _workingPhaseDuration <= TimeSpan.FromHours(3) &&
            //    _workingPhaseDuration >= TimeSpan.FromMinutes(1) &&
            //    _restingPhaseDuration <= TimeSpan.FromHours(1) &&
            //    _restingPhaseDuration >= TimeSpan.FromMinutes(1));

            return true;
        }

        public async Task StartAsync()
        {
            _isActive = true;

            while (_isActive)
            {
                if (_currentSet >= _setsCount)
                {
                    _isActive = false;
                    OnPomodoroEnd?.Invoke(this, EventArgs.Empty);
                    return;
                }

                await RunPhase(PomodoroPhase.Working,
                    _workingPhaseDuration);

                if (_globalCTS.IsCancellationRequested) break;

                await RunPhase(PomodoroPhase.Resting,
                    GetRestDuration());

                if (_globalCTS.IsCancellationRequested) break;

                ++_currentSet;
            }

            _isComplete = false;
        }

        public void Pause()
        {
            if (!_isPaused)
            {
                _isPaused = true;
                _stopwatch.Stop();
                OnPaused?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isPaused = false;
                _stopwatch.Start();
                OnResumed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Skip()
        {
            _cts?.Cancel();
            _isPaused = false;
        }

        public void Quit()
        {
            _stopwatch.Stop();
            _isActive = false;
            _isPaused = false;
            _cts?.Cancel();
            _globalCTS.Cancel();

            OnPomodoroInt?.Invoke(this, EventArgs.Empty);
        }


        private async Task RunPhase(PomodoroPhase phase, TimeSpan duration)
        {
            _phase = phase;
            _currentDuration = duration;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await RunTimer(duration, _cts.Token);

            OnPhaseEnd?.Invoke(this, EventArgs.Empty);
        }

        private async Task RunTimer(TimeSpan duration,
            CancellationToken cancellationToken)
        {
            _stopwatch.Start();
            OnPhaseStart?.Invoke(this, EventArgs.Empty);

            while (_stopwatch.Elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(100);
            }

            _stopwatch.Reset();
        }

        private TimeSpan GetRestDuration()
        {
            TimeSpan restDuration;

            if ((_longRestingPhaseDuration.HasValue
                && _setsUntilLongRest.HasValue) &&
                (_currentSet + 1) % _setsUntilLongRest.Value == 0)
            {
                restDuration = (TimeSpan)_longRestingPhaseDuration!;
            } else
            {
                restDuration = _restingPhaseDuration;
            }

            return restDuration;
        }

        
        public event EventHandler?
            OnPomodoroEnd,
            OnPomodoroInt,
            OnPhaseStart,
            OnPhaseEnd,
            OnPaused,
            OnResumed;
    }
}