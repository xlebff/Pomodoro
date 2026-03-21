using Pomodoro.Core.Engine;
using Pomodoro.Core.Interfaces;
using Pomodoro.Core.Models;
using Pomodoro.Resources;

namespace Pomodoro
{
    internal class Application(IInputHandler inputHandler, 
        IUserInterface ui,
        ISettingsRepository settingsRepo,
        IAudioService audio)
    {
        private readonly IInputHandler _inputHandler = inputHandler;

        private readonly IUserInterface _ui = ui;

        private readonly ISettingsRepository _settingsRepo = settingsRepo;

        private readonly IAudioService _audioService = audio;

        private PomodoroEngine? _engine;

        private bool isActive = false;


        private const string CommandWork = "work",
            CommandRest = "rest",
            CommandLong = "long",
            CommandSets = "cycles",
            CommandLongEvery = "long-every",
            CommandSet = "set",
            CommandGet = "get",
            CommandStart = "start",
            CommandQuit = "quit",
            CommandClear = "clear",
            CommandHelp = "help";

        private const float WorkingPhaseDurationMin = 1,
            WorkingPhaseDurationMax = 60,
            RestingPhaseDurationMin = 1,
            RestingPhaseDurationMax = 15,
            LongRestingPhaseDurationMin = 5,
            LongRestingPhaseDurationMax = 30;

        private const int SetsMin = 1,
            SetsMax = 99,
            SetsUntilLongRestMin = 1,
            SetsUntilLongRestMax = 10;


        public async Task RunAsync(string[]? args,
            CancellationToken cancellationToken)
        {
            _inputHandler.KeyPressed += OnMenuKeyPressed;

            await _settingsRepo.LoadAsync();

            _inputHandler.StartListening(cancellationToken);

            if (args != null && args.Length > 0)
                await CommandHandler(args);

            while (!cancellationToken.IsCancellationRequested
                && !isActive)
            {
                await CommandHandler(Console.ReadLine()?.Split(' '));
            }
        }


        private async Task<int> CommandHandler(string[]? args)
        {
            if (args == null || args.Length == 0) return 1;

            switch (args[0].ToLower())
            {
                case CommandStart:
                    await StartAsync();
                    break;
                case CommandQuit:
                    Environment.Exit(0);
                    break;
                case CommandSet:
                    await SetHandlerAsync(args[1..]);
                    break;
                case CommandGet:
                    await GetHandlerAsync(args[1..]);
                    break;
                case CommandHelp:
                    Console.WriteLine();
                    await _ui.WriteMessageAsync(Messages.Help);
                    break;
                case CommandClear:
                    Console.Clear();
                    break;
                default:
                    await _ui.WriteMessageAsync(Messages.CommandHandleError);
                    break;
            }

            return 0;
        }

        private async Task GetHandlerAsync(string[]? args)
        {
            Console.WriteLine();
            if (args == null || args.Length == 0)
            {
                await _ui.WriteMessageAsync(Messages.GetHelp);
                return;
            }

            switch (args[0].ToLower())
            {
                case CommandWork:
                    await HandleGetCommand(
                        _settingsRepo.GetCurrentSettings()!.WorkingPhaseMinutes,
                        Messages.CurrentWorkingDuration);
                    break;
                case CommandRest:
                    await HandleGetCommand(
                        _settingsRepo.GetCurrentSettings()!.RestingPhaseMinutes,
                        Messages.CurrentRestingDuration);
                    break;
                case CommandLong:
                    await HandleGetCommand(
                        _settingsRepo.GetCurrentSettings()!.LongRestingPhaseMinutes,
                        Messages.CurrentLongRestingDuration);
                    break;
                case CommandSets:
                    await HandleGetCommand(
                        _settingsRepo.GetCurrentSettings()!.SetsCount,
                        Messages.CurrentCycles);
                    break;
                case CommandLongEvery:
                    await HandleGetCommand(
                        _settingsRepo.GetCurrentSettings()!.SetsUntilLongResting,
                        Messages.CurrentLongEvery);
                    break;
                default:
                    await _ui.WriteMessageAsync(Messages.CommandHandleError);
                    break;

            }

            Console.WriteLine();
        }

        private async Task HandleGetCommand(float? value,
            string message)
        {
            if (value.HasValue)
                await _ui.WriteMessageAsync(message + value);
            else await _ui.WriteMessageAsync(Messages.NoValue);
        }

        private async Task HandleGetCommand(int? value,
            string message)
        {
            if (value.HasValue)
                await _ui.WriteMessageAsync(message + value);
            else await _ui.WriteMessageAsync(Messages.NoValue);
        }

        private async Task SetHandlerAsync(string[]? args)
        {
            Console.WriteLine();
            if (args == null || args.Length <= 1)
            {
                await _ui.WriteMessageAsync(Messages.SetHelp);
                return;
            }

            switch (args[0].ToLower())
            {
                case CommandWork:
                    await HandleSetCommand(args[1],
                        WorkingPhaseDurationMin, WorkingPhaseDurationMax,
                        () => _settingsRepo.GetCurrentSettings()!.WorkingPhaseMinutes,
                        v => _settingsRepo.GetCurrentSettings()!.WorkingPhaseMinutes = v,
                        Messages.WorkingDurationSetError);
                    break;
                case CommandRest:
                    await HandleSetCommand(args[1],
                        RestingPhaseDurationMin, RestingPhaseDurationMax,
                        () => _settingsRepo.GetCurrentSettings()!.RestingPhaseMinutes,
                        v => _settingsRepo.GetCurrentSettings()!.RestingPhaseMinutes = v,
                        Messages.RestingDurationSetError);
                    break;
                case CommandLong:
                    await HandleSetCommand(args[1],
                        LongRestingPhaseDurationMin,
                        LongRestingPhaseDurationMax,
                        () => _settingsRepo.GetCurrentSettings()!.LongRestingPhaseMinutes,
                        v => _settingsRepo.GetCurrentSettings()!.LongRestingPhaseMinutes = v,
                        Messages.LongRestingDurationSetError);
                    break;
                case CommandSets:
                    await HandleSetCommand(args[1],
                        SetsMin, SetsMax,
                        () => _settingsRepo.GetCurrentSettings()!.SetsCount,
                        v => _settingsRepo.GetCurrentSettings()!.SetsCount = v,
                        Messages.CyclesSetError);
                    break;
                case CommandLongEvery:
                    await HandleSetCommand(args[1],
                        SetsUntilLongRestMin,
                        SetsUntilLongRestMax,
                        () => _settingsRepo.GetCurrentSettings()!.SetsUntilLongResting,
                        v => _settingsRepo.GetCurrentSettings()!.SetsUntilLongResting = v,
                        Messages.LongEverySetError);
                    break;
                default:
                    await _ui.WriteMessageAsync(Messages.CommandHandleError);
                    break;

            }

            Console.WriteLine();
        }

        private async Task HandleSetCommand(
            string valueStr,
            float min, float max,
            Func<float?> getter,
            Action<float> setter,
            string errorMessage)
        {
            if (float.TryParse(valueStr, out float newValue)
                && newValue >= min && newValue <= max)
            {
                if (getter() != newValue)
                {
                    setter(newValue);
                    await _settingsRepo.SaveAsync();
                }
                await _ui.WriteMessageAsync(Messages.SuccessfullySet +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
            else
            {
                await _ui.WriteMessageAsync(errorMessage +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
        }

        private async Task HandleSetCommand(
            string valueStr,
            int min, int max,
            Func<int?> getter,
            Action<int> setter,
            string errorMessage)
        {
            if (int.TryParse(valueStr, out int newValue)
                && newValue >= min && newValue <= max)
            {
                if (getter() != newValue)
                {
                    setter(newValue);
                    await _settingsRepo.SaveAsync();
                }
                await _ui.WriteMessageAsync(Messages.SuccessfullySet +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
            else
            {
                await _ui.WriteMessageAsync(errorMessage +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
        }


        private async Task<int> StartAsync()
        {
            isActive = true;
            Console.Clear();

            PomodoroSettings? settings = 
                _settingsRepo.GetCurrentSettings();

            if (settings == null)
                return 1;

            _engine = new PomodoroEngine(
                TimeSpan.FromMinutes(settings.WorkingPhaseMinutes),
                TimeSpan.FromMinutes(settings.RestingPhaseMinutes),
                settings.SetsCount,
                settings.LongRestingPhaseMinutes is not null ?
                    TimeSpan.FromMinutes((double)settings.LongRestingPhaseMinutes) :
                    null,
                settings.SetsUntilLongResting);

            _engine.OnPhaseStart += _ui.OnPhaseStartAsync;
            _engine.OnPhaseEnd += _ui.OnPhaseEndAsync;
            _engine.OnPomodoroStart += _ui.OnPomodoroStartAsync;
            _engine.OnPomodoroEnd += _ui.OnPomodoroEndAsync;
            _engine.OnPomodoroInt += _ui.OnPomodoroIntAsync;

            _engine.OnPhaseCountdown += _audioService.PlayTickAsync;
            _engine.OnPhaseEnd += _audioService.PlayAlarmAsync;

            _inputHandler.KeyPressed += OnPomodoroKeyPressed;

            await _engine.StartAsync();

            return 0;
        }


        private Task OnMenuKeyPressed(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.S:
                    _ui.Skip();
                    break;
                case ConsoleKey.Q:
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }

        private async Task OnPomodoroKeyPressed(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.S:
                    await _engine!.Skip();
                    break;
                case ConsoleKey.Q:
                    await _engine!.Quit();
                    break;
                default:
                    break;
            }
        }
    }
}
