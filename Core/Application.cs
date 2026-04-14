using System.Globalization;
using System.Text;
using ConsolePomodoro.Core.Contracts;
using ConsolePomodoro.Domain.Models;
using ConsolePomodoro.Engine;
using ConsolePomodoro.Infrastructure.Configuration;
using ConsolePomodoro.Resources;

namespace ConsolePomodoro.Core;

internal class Application(
    IInputHandler inputHandler,
    IUserInterface ui,
    IAudioService audioService,
    IJsonConfigRepository applicationConfigRepository,
    IJsonConfigRepository pomodoroConfigRepository,
    Func<PomodoroEngine> engineFactory)
{
    private readonly IInputHandler _inputHandler = inputHandler;
    private readonly IUserInterface _ui = ui;
    private readonly IAudioService _audioService = audioService;
    private readonly IJsonConfigRepository _applicationConfigRepository = applicationConfigRepository;
    private readonly IJsonConfigRepository _pomodoroConfigRepository = pomodoroConfigRepository;
    private readonly ApplicationConfig _applicationConfig = (ApplicationConfig)applicationConfigRepository.GetConfig();
    private readonly PomodoroConfig _pomodoroConfig = (PomodoroConfig)pomodoroConfigRepository.GetConfig();
    private readonly Func<PomodoroEngine> _engineFactory = engineFactory;

    private PomodoroEngine? _engine;
    private CancellationTokenSource? _pomodoroInputCts;

    private bool _isPomodoroActive;
    private bool _shouldExit;

    private const string CommandStart = "start";
    private const string CommandSet = "set";
    private const string CommandGet = "get";
    private const string CommandHelp = "help";
    private const string CommandClear = "clear";
    private const string CommandQuit = "quit";

    private const string SettingWork = "work";
    private const string SettingRest = "rest";
    private const string SettingLong = "long";
    private const string SettingCycles = "cycles";
    private const string SettingLongEvery = "long-every";
    private const string SettingAll = "all";
    private const string SettingOff = "off";

    private const float WorkMin = 1f;
    private const float WorkMax = 60f;
    private const float RestMin = 1f;
    private const float RestMax = 15f;
    private const float LongRestMin = 5f;
    private const float LongRestMax = 30f;

    private const int CyclesMin = 1;
    private const int CyclesMax = 99;
    private const int LongEveryMin = 2;
    private const int LongEveryMax = 10;

    private bool HasMusic =>
        !string.IsNullOrWhiteSpace(_applicationConfig.MusicDir)
        && Directory.Exists(_applicationConfig.MusicDir);

    public async Task RunAsync(string[]? args, CancellationToken cancellationToken)
    {
        Console.OutputEncoding = Encoding.Unicode;

        _inputHandler.KeyPressed += OnMenuKeyPressed;
        _pomodoroInputCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = Task.Run(() => _inputHandler.StartListening(_pomodoroInputCts.Token));

        await _ui.WriteMessageAsync(
            Messages.Message_ProgramStart);

        if (args is { Length: > 0 })
        {
            await HandleCommandAsync(args, cancellationToken);
        }

        while (!cancellationToken.IsCancellationRequested && !_shouldExit)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            string[] commandParts = input.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            await HandleCommandAsync(commandParts, cancellationToken);
        }
    }

    private async Task HandleCommandAsync(string[]? args, CancellationToken cancellationToken)
    {
        if (args is null || args.Length == 0)
        {
            return;
        }

        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case CommandStart:
                await StartPomodoroAsync(cancellationToken);
                break;

            case CommandSet:
                await HandleSetAsync([.. args.Skip(1)]);
                break;

            case CommandGet:
                await HandleGetAsync([.. args.Skip(1)]);
                break;

            case CommandHelp:
                await _ui.WriteMessageAsync(GetHelpText());
                break;

            case CommandClear:
                Console.Clear();
                await ShowCommandInvitationAsync();
                break;

            case CommandQuit:
                _shouldExit = true;
                if (_isPomodoroActive && _engine is not null)
                {
                    await _engine.Quit();
                }
                break;

            default:
                await _ui.WriteMessageAsync(
                    "Unknown command. Type 'help' to see available commands.");
                break;
        }
    }

    private async Task StartPomodoroAsync(CancellationToken cancellationToken)
    {
        if (_isPomodoroActive)
        {
            await _ui.WriteMessageAsync("Pomodoro session is already running.");
            return;
        }

        _engine = _engineFactory();
        _isPomodoroActive = true;

        AttachEngineHandlers(_engine);
        _inputHandler.KeyPressed += OnPomodoroKeyPressed;

        Console.Clear();

        try
        {
            await _engine.StartAsync();
        }
        finally
        {
            _inputHandler.KeyPressed -= OnPomodoroKeyPressed;

            if (_engine is not null)
            {
                DetachEngineHandlers(_engine);
            }

            _engine = null;
            _isPomodoroActive = false;

            if (!_shouldExit && !cancellationToken.IsCancellationRequested)
            {
                await ShowCommandInvitationAsync();
            }
        }
    }

    private async Task HandleGetAsync(string[] args)
    {
        if (args.Length == 0)
        {
            await _ui.WriteMessageAsync(
                "Usage: get work|rest|long|cycles|long-every|all");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case SettingWork:
                await _ui.WriteMessageAsync($"Work duration: {_pomodoroConfig.WorkPhaseDuration} min");
                break;

            case SettingRest:
                await _ui.WriteMessageAsync($"Break duration: {_pomodoroConfig.BreakPhaseDuration} min");
                break;

            case SettingLong:
                await _ui.WriteMessageAsync(
                    _pomodoroConfig.LongBreakDuration.HasValue
                        ? $"Long break duration: {_pomodoroConfig.LongBreakDuration.Value} min"
                        : "Long break is disabled.");
                break;

            case SettingCycles:
                await _ui.WriteMessageAsync($"Cycles count: {_pomodoroConfig.CyclesCount}");
                break;

            case SettingLongEvery:
                await _ui.WriteMessageAsync(
                    _pomodoroConfig.CyclesBeforeLongBreak.HasValue
                        ? $"Long break every: {_pomodoroConfig.CyclesBeforeLongBreak.Value} cycles"
                        : "Long break is disabled.");
                break;

            case SettingAll:
                await _ui.WriteMessageAsync(GetCurrentSettingsText());
                break;

            default:
                await _ui.WriteMessageAsync(
                    "Unknown setting. Use: work, rest, long, cycles, long-every or all.");
                break;
        }
    }

    private async Task HandleSetAsync(string[] args)
    {
        if (args.Length < 2)
        {
            await _ui.WriteMessageAsync(
                "Usage: set work|rest|long|cycles|long-every <value>. Example: set work 25");
            return;
        }

        string settingName = args[0].ToLowerInvariant();
        string rawValue = args[1].ToLowerInvariant();

        switch (settingName)
        {
            case SettingWork:
                if (TryParseFloat(rawValue, out float workValue) && InRange(workValue, WorkMin, WorkMax))
                {
                    _pomodoroConfig.WorkPhaseDuration = workValue;
                    await _ui.WriteMessageAsync($"Work duration set to {workValue} min.");
                    return;
                }

                await _ui.WriteMessageAsync("Work duration must be in range 1..60 minutes.");
                return;

            case SettingRest:
                if (TryParseFloat(rawValue, out float restValue) && InRange(restValue, RestMin, RestMax))
                {
                    _pomodoroConfig.BreakPhaseDuration = restValue;
                    await _ui.WriteMessageAsync($"Break duration set to {restValue} min.");
                    return;
                }

                await _ui.WriteMessageAsync("Break duration must be in range 1..15 minutes.");
                return;

            case SettingLong:
                if (rawValue == SettingOff)
                {
                    DisableLongBreak();
                    await _ui.WriteMessageAsync("Long break disabled.");
                    return;
                }

                if (TryParseFloat(rawValue, out float longValue) && InRange(longValue, LongRestMin, LongRestMax))
                {
                    _pomodoroConfig.LongBreakDuration = longValue;
                    _pomodoroConfig.CyclesBeforeLongBreak ??= 4;

                    (bool success, string? message) = await _pomodoroConfigRepository.SaveAsync();
                    if (!success)
                    {
                        await _ui.WriteMessageAsync(message!);
                        return;
                    }

                    await _ui.WriteMessageAsync(
                        $"Long break duration set to {longValue} min. " +
                        $"Long break frequency: {_pomodoroConfig.CyclesBeforeLongBreak} cycles.");
                    return;
                }

                await _ui.WriteMessageAsync("Long break duration must be in range 5..30 minutes or 'off'.");
                return;

            case SettingCycles:
                if (TryParseInt(rawValue, out int cyclesValue) && InRange(cyclesValue, CyclesMin, CyclesMax))
                {
                    _pomodoroConfig.CyclesCount = cyclesValue;
                    await _ui.WriteMessageAsync($"Cycles count set to {cyclesValue}.");
                    return;
                }

                await _ui.WriteMessageAsync("Cycles count must be in range 1..99.");
                return;

            case SettingLongEvery:
                if (rawValue == SettingOff)
                {
                    DisableLongBreak();
                    await _ui.WriteMessageAsync("Long break disabled.");
                    return;
                }

                if (TryParseInt(rawValue, out int longEveryValue) && InRange(longEveryValue, LongEveryMin, LongEveryMax))
                {
                    _pomodoroConfig.CyclesBeforeLongBreak = longEveryValue;
                    _pomodoroConfig.LongBreakDuration ??= 15f;
                    await _ui.WriteMessageAsync(
                        $"Long break frequency set to every {longEveryValue} cycles. " +
                        $"Long break duration: {_pomodoroConfig.LongBreakDuration} min.");
                    return;
                }

                await _ui.WriteMessageAsync("Long break frequency must be in range 2..10 cycles or 'off'.");
                return;

            default:
                await _ui.WriteMessageAsync(
                    "Unknown setting. Use: work, rest, long, cycles or long-every.");
                return;
        }
    }

    private void AttachEngineHandlers(PomodoroEngine engine)
    {
        engine.OnPomodoroStart += _ui.OnPomodoroStartAsync;
        engine.OnPomodoroEnd += _ui.OnPomodoroEndAsync;
        engine.OnPomodoroInt += _ui.OnPomodoroIntAsync;

        engine.OnPhaseStart += _ui.OnPhaseStart;
        engine.OnPhaseEnd += _ui.OnPhaseEnd;

        engine.OnPomodoroStart += _audioService.OnPomodoroStart;
        engine.OnPhaseStart += _audioService.OnPhaseStart;
        engine.OnPhaseCountdown += _audioService.OnPhaseCountdown;
        engine.OnPhaseEnd += _audioService.OnPhaseEnd;
    }

    private void DetachEngineHandlers(PomodoroEngine engine)
    {
        engine.OnPomodoroStart -= _ui.OnPomodoroStartAsync;
        engine.OnPomodoroEnd -= _ui.OnPomodoroEndAsync;
        engine.OnPomodoroInt -= _ui.OnPomodoroIntAsync;

        engine.OnPhaseStart -= _ui.OnPhaseStart;
        engine.OnPhaseEnd -= _ui.OnPhaseEnd;

        engine.OnPomodoroStart -= _audioService.OnPomodoroStart;
        engine.OnPhaseStart -= _audioService.OnPhaseStart;
        engine.OnPhaseCountdown -= _audioService.OnPhaseCountdown;
        engine.OnPhaseEnd -= _audioService.OnPhaseEnd;
    }

    private async Task OnPomodoroKeyPressed(ConsoleKey key)
    {
        if (_engine is null)
        {
            return;
        }

        switch (key)
        {
            case ConsoleKey.S:
                _engine.Skip();
                break;

            case ConsoleKey.Spacebar:
            case ConsoleKey.P:
                _engine.Pause();
                break;

            case ConsoleKey.Q:
                await _engine.Quit();
                break;

            case ConsoleKey.UpArrow:
                _audioService.VolumeIncrease();
                break;

            case ConsoleKey.DownArrow:
                _audioService.VolumeDecrease();
                break;
        }
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

    private async Task ShowCommandInvitationAsync()
    {
        await _ui.WriteMessageAsync(
            "Pomodoro timer is ready. Enter a command. Type 'help' to see available commands.");
    }

    private string GetCurrentSettingsText()
    {
        string longDurationText = _pomodoroConfig.LongBreakDuration?.ToString(CultureInfo.InvariantCulture) ?? "off";
        string longEveryText = _pomodoroConfig.CyclesBeforeLongBreak?.ToString(CultureInfo.InvariantCulture) ?? "off";

        return string.Join(Environment.NewLine,
            $"work: {_pomodoroConfig.WorkPhaseDuration} min",
            $"rest: {_pomodoroConfig.BreakPhaseDuration} min",
            $"long: {longDurationText}",
            $"cycles: {_pomodoroConfig.CyclesCount}",
            $"long-every: {longEveryText}");
    }

    private static string GetHelpText()
    {
        return string.Join(Environment.NewLine,
            "Available commands:",
            "start",
            "get work|rest|long|cycles|long-every|all",
            "set work <1..60>",
            "set rest <1..15>",
            "set long <5..30>|off",
            "set cycles <1..99>",
            "set long-every <2..10>|off",
            "clear",
            "quit",
            string.Empty,
            "Hotkeys during a running timer:",
            "S - skip current phase",
            "Space / P - pause or resume",
            "Q - stop current pomodoro",
            "Up / Down - change music volume");
    }

    private void DisableLongBreak()
    {
        _pomodoroConfig.LongBreakDuration = null;
        _pomodoroConfig.CyclesBeforeLongBreak = null;
    }

    private static bool TryParseFloat(string rawValue, out float value)
    {
        string normalizedValue = rawValue.Replace(',', '.');
        return float.TryParse(
            normalizedValue,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseInt(string rawValue, out int value)
    {
        return int.TryParse(
            rawValue,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool InRange(float value, float min, float max) => value >= min && value <= max;

    private static bool InRange(int value, int min, int max) => value >= min && value <= max;
}
