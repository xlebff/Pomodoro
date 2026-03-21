using Pomodoro.Core.Interfaces;
using System.Diagnostics;

namespace Pomodoro.Core.Engine
{
    internal enum PomodoroPhase { Working, Resting }

    internal class PomodoroEngine : IPomodoroEngine
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
        private bool _isTicking = false;

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


        public async Task StartAsync()
        {
            _isActive = true;
            await AsyncEvent(OnPomodoroStart?.GetInvocationList());

            while (_isActive)
            {
                if (_currentSet >= _setsCount)
                {
                    _isActive = false;
                    await AsyncEvent(OnPomodoroEnd?.GetInvocationList());
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

        public async Task Pause()
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

        public Task Skip()
        {
            _cts?.Cancel();
            _isPaused = false;

            return Task.CompletedTask;
        }

        public async Task Quit()
        {
            _stopwatch.Stop();
            _isActive = false;
            _isPaused = false;
            _cts?.Cancel();
            _globalCTS.Cancel();

            await AsyncEvent(OnPomodoroInt?.GetInvocationList());
        }


        private async Task RunPhase(PomodoroPhase phase, TimeSpan duration)
        {
            _isTicking = false;
            _isPaused = false;

            _phase = phase;
            _currentDuration = duration;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            await RunTimer(duration, _cts.Token);

            await AsyncEvent(OnPhaseEnd?.GetInvocationList());
        }

        private async Task RunTimer(TimeSpan duration,
            CancellationToken cancellationToken)
        {
            _stopwatch.Start();
            await AsyncEvent(OnPhaseStart?.GetInvocationList());

            while (_stopwatch.Elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _isTicking = false;
                    break;
                }

                if ((duration - _stopwatch.Elapsed) 
                    <= TimeSpan.FromSeconds(4) &&
                    !_isTicking)
                {
                    _isTicking = true;
                    _ = Task.Run(() =>
                        AsyncEvent(OnPhaseCountdown?.GetInvocationList()));
                }

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

        private async Task AsyncEvent(Delegate[]? handlers)
        {
            if (handlers != null)
            {
                var tasks = handlers
                    .Cast<AsyncEventHandler<EventArgs>>()
                    .Select(h => h(this, EventArgs.Empty));
                await Task.WhenAll(tasks);
            }
        }


        public delegate Task AsyncEventHandler<TEventArgs>(object sendler,
            TEventArgs e) where TEventArgs : EventArgs;

        public event AsyncEventHandler<EventArgs>?
            OnPomodoroStart,
            OnPomodoroInt,
            OnPomodoroEnd,
            OnPhaseStart,
            OnPhaseEnd,
            OnPhaseCountdown;

        public event EventHandler?
            OnPaused,
            OnResumed;
    }
}