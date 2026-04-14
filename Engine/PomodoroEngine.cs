using System.Diagnostics;
using ConsolePomodoro.Core.Contracts;

namespace ConsolePomodoro.Engine
{
    internal class PomodoroEngine : IPomodoroEngine
    {
        private readonly Stopwatch _stopwatch = new();

        private readonly TimeSpan _workPhaseDuration;
        private readonly TimeSpan _breakPhaseDuration;
        private readonly TimeSpan? _longBreakPhaseDuration;
        private TimeSpan _currentDuration;

        private readonly int _cyclesCount;
        private readonly int? _cyclesBeforeLongBreak;
        private int _currentCycle = 0;

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
            _workPhaseDuration = workingPhaseDuration;
            _breakPhaseDuration = restingPhaseDuration;
            _cyclesCount = setsCount;
            _longBreakPhaseDuration = longRestingPhaseDuration;
            _cyclesBeforeLongBreak = setsUntilLingRest;
            _currentDuration = _workPhaseDuration;
        }


        public PomodoroPhase Phase => _phase;
        public TimeSpan CurrentDuration => _currentDuration;
        public bool IsCompleted => _isComplete;


        public TimeSpan GetElapsed() => _stopwatch.Elapsed;

        public async Task StartAsync()
        {
            _isActive = true;
            await AsyncEvent(OnPomodoroStart?.GetInvocationList());

            while (_isActive)
            {
                // если циклы закончились, триггерим ивенты и выходим
                if (_currentCycle >= _cyclesCount)
                {
                    _isActive = false;
                    await AsyncEvent(OnPomodoroEnd?.GetInvocationList());
                    return;
                }

                // запускаем фазу работы
                await RunPhase(PomodoroPhase.Working,
                    _workPhaseDuration);

                if (_globalCTS.IsCancellationRequested) break;

                await RunPhase(PomodoroPhase.Resting,
                    GetRestDuration());

                if (_globalCTS.IsCancellationRequested) break;

                ++_currentCycle;
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
            // сбрасываем состояния
            _isTicking = false;
            _isPaused = false;

            // устанавливаем состояния
            _phase = phase;
            _currentDuration = duration;

            // обновляем токен отмены таймера
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            // запуск самого таймера с переданной длительностью и токеном
            await RunTimer(duration, _cts.Token);

            // триггерим ивенты
            OnPhaseEnd?.Invoke(this, EventArgs.Empty);
        }

        private async Task RunTimer(TimeSpan duration,
            CancellationToken cancellationToken)
        {
            // запускае стопвач
            _stopwatch.Start();
            // триггерим ивенты синхронно
            OnPhaseStart?.Invoke(this, EventArgs.Empty);

            while (_stopwatch.Elapsed < duration)
            {
                // если отменили – сбрасываем тиканье и выходим
                if (cancellationToken.IsCancellationRequested)
                {
                    _isTicking = false;
                    break;
                }

                // проверка на обратный отсчёт и на то, тикаем ли уже
                if ((duration - _stopwatch.Elapsed)
                    <= TimeSpan.FromSeconds(4) &&
                    !_isTicking)
                {
                    // поднимаем тиканье и триггерим ивенты синхронно
                    _isTicking = true;
                    OnPhaseCountdown?.Invoke(this, EventArgs.Empty);
                }

                // небольшая задержка без токена отмены
                await Task.Delay(100, CancellationToken.None);
            }

            // сбрасываемся
            _stopwatch.Reset();
        }

        private TimeSpan GetRestDuration()
        {
            TimeSpan restDuration;

            if ((_longBreakPhaseDuration.HasValue
                && _cyclesBeforeLongBreak.HasValue) &&
                (_currentCycle + 1) % _cyclesBeforeLongBreak.Value == 0)
            {
                restDuration = (TimeSpan)_longBreakPhaseDuration!;
            }
            else
            {
                restDuration = _breakPhaseDuration;
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
            OnPomodoroEnd;

        public event EventHandler?
            OnPaused,
            OnResumed,
            OnPhaseStart,
            OnPhaseEnd,
            OnPhaseCountdown;
    }
}