using SimplePomodoro.Infrastructure;
using SimplePomodoro.Models;

namespace SimplePomodoro.Core;

internal class Program
{
    private const float WorkPhaseDurationMin = 5.0f;
    private const float WorkPhaseDurationMax = 60.0f;

    private const float BreakPhaseDurationMin = 1.0f;
    private const float BreakPhaseDurationMax = 15.0f;

    private const float LongBreakPhaseDurationMin = 0f;
    private const float LongBreakPhaseDurationMax = 30.0f;

    private const int CyclesBeforeLongBreakMin = 0;
    private const int CyclesBeforeLongBreakMax = 10;

    private const int CyclesCountMin = 1;
    private const int CyclesCountMax = 99;

    private static string PomodroConfigPath = string.Empty;
    private static string AppConfigPath = string.Empty;

    private static ApplicationConfig? _appConfig;
    private static PomodoroConfig? _pomodoroConfig;

    public static async Task Main(string[] args)
    {
        string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        AppConfigPath = AppDir + "Config/appConfig.json";
        PomodroConfigPath = AppDir + "Config/pomodoroConfig.json";

        // json reader
        _appConfig = await JsonReader.ReadAsync<ApplicationConfig>(AppConfigPath);
        _pomodoroConfig = await JsonReader.ReadAsync<PomodoroConfig>(PomodroConfigPath);

        if (_appConfig == null ||
            _pomodoroConfig == null)
        {
            return;
        }

        // commands handler
        if (args.Length != 0)
        {
            switch (args[0])
            {
                case Commands.StartCommandName:
                    break;
                case Commands.SetCommandName:
                    if (args.Length >= 3)
                        await SetHandlerAsync(args[1..]);
                    return;
                case Commands.GetCommandName:
                    GetHandler(args[1]);
                    return;
                default:
                    break;
            }
        }

        string tickingPath = AppDir + "Assets/Audio/Timer/ticking.mp3";
        string endBellPath = AppDir + "Assets/Audio/Timer/end_bell.mp3";

        string musicPath;

        if (_appConfig.MusicDir is not null &&
            System.IO.Path.IsPathFullyQualified(_appConfig.MusicDir))
        {
            musicPath = _appConfig.MusicDir;
        }
        else musicPath = AppDir + "Assets/Audio/Music";

        Engine engine = new(_appConfig, _pomodoroConfig);
        AudioControl audioControl = new(tickingPath,
                                        endBellPath,
                                        musicPath,
                                        _appConfig.DefaultPhaseEndBellVolume,
                                        _appConfig.DefaultTickingVolume,
                                        _appConfig.DefaultMusicVolume);

        audioControl.Init();

        Handler.OnPause += engine.Pause;
        Handler.OnPause += audioControl.OnPause;

        Handler.OnSkip += engine.Skip;

        Handler.OnNext += audioControl.NextTrack;

        Handler.OnPrevious += audioControl.PreviousTrack;

        Handler.OnUp += audioControl.OnVolumeIncrease;

        Handler.OnDown += audioControl.OnVolumeDecrease;

        engine.OnPhaseStart += audioControl.OnPhaseStart;

        engine.OnPhaseEnd += audioControl.OnPhaseEnd;

        engine.OnCountdown += audioControl.OnPhaseCountdown;

        await engine.StartAsync();
    }

    private static async Task SetHandlerAsync(string[] args)
    {
        if (string.IsNullOrEmpty(args[0]))
        {
            Console.Error.WriteLine("Unknown field.");
            return;
        }

        if (string.IsNullOrEmpty(args[1]))
        {
            Console.Error.WriteLine(("Incorrect value."));
            return;
        }

        string fieldName = args[0];
        string value = args[1];

        switch (fieldName)
        {
            case Commands.WorkSubcommandName:
                await Setting<PomodoroConfig>(value,
                    WorkPhaseDurationMin,
                    WorkPhaseDurationMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    (value => _pomodoroConfig!.WorkPhaseDuration = value),
                    "The duration of the work phase must be more than 1 and less than 99.");
                break;

            case Commands.BreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    BreakPhaseDurationMin,
                    BreakPhaseDurationMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    (value => _pomodoroConfig!.BreakPhaseDuration = value),
                    "The duration of the break phase must be more than 1 and less than 15.");
                break;

            case Commands.LongBreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    LongBreakPhaseDurationMin,
                    LongBreakPhaseDurationMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    (value => _pomodoroConfig!.LongBreakDuration = value),
                    "The duration of the long break phase must be more than 5 and less than 30.");
                break;

            case Commands.CyclesBeforeLongBreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    CyclesBeforeLongBreakMin,
                    CyclesBeforeLongBreakMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    (value => _pomodoroConfig!.CyclesBeforeLongBreak = value),
                    "The amount of the cycles before long break phase must be more than 2 and less than 10.");
                break;

            case Commands.CyclesCountSubcommandName:
                await Setting<PomodoroConfig>(value,
                    CyclesCountMin,
                    CyclesCountMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    (value => _pomodoroConfig!.CyclesCount = value),
                    "The amount of the cycles must be more than 1 and less than 99.");
                break;

            case Commands.MusicDirSubcommandName:
                if (Directory.Exists(value))
                {
                    _appConfig!.MusicDir = value;

                    if (await JsonReader.SaveAsync<ApplicationConfig>(AppConfigPath, _appConfig))
                        Console.WriteLine("The new value was set successfully.");
                }
                else Console.Error.WriteLine("Path cannot be found.");
                break;
        }
    }

    private static void GetHandler(string field)
    {
        if (string.IsNullOrEmpty(field))
            return;

        if (_pomodoroConfig is null ||
            _appConfig is null)
            return;

        switch (field)
        {
            case Commands.WorkSubcommandName:
                Console.WriteLine(String.Format("Current value of the work phase: {0}.", _pomodoroConfig.WorkPhaseDuration));
                break;

            case Commands.BreakSubcommandName:
                Console.WriteLine(String.Format("Current value if the break pahse: {0}.", _pomodoroConfig.BreakPhaseDuration));
                break;

            case Commands.LongBreakSubcommandName:
                Console.WriteLine(String.Format("Current value of the long break duration phase: {0}.", _pomodoroConfig.LongBreakDuration));
                break;

            case Commands.CyclesBeforeLongBreakSubcommandName:
                Console.WriteLine(String.Format("Current amount of cycles before the long break: {0}.", _pomodoroConfig.CyclesBeforeLongBreak));
                break;

            case Commands.CyclesCountSubcommandName:
                Console.WriteLine(String.Format("Current amount of cycles: {0}.", _pomodoroConfig.CyclesCount));
                break;

            case Commands.MusicDirSubcommandName:
                Console.WriteLine(String.Format("Current music directory: {0}.", _appConfig.MusicDir));
                break;
        }
    }

    private static async Task Setting<T>(string newValueStr,
        float minValue,
        float maxValue,
        string configPath,
        T config,
        Action<float> setter,
        string correctedValue)
    {
        float newValue;

        try
        {
            newValue = float.Parse(newValueStr);
        }
        catch
        {
            Console.Error.WriteLine(String.Format("Cannot parse the \"{0}\".", newValueStr));
            return;
        }

        if (newValue >= minValue && newValue <= maxValue)
        {
            setter(newValue);

            if (await JsonReader.SaveAsync<T>(configPath, config))
                Console.WriteLine("The new value was set successfully.");
        }
        else Console.Error.WriteLine(correctedValue);
    }

    private static async Task Setting<T>(string newValueStr,
        int minValue,
        int maxValue,
        string configPath,
        T config,
        Action<int> setter,
        string correctedValue)
    {
        int newValue;

        try
        {
            newValue = int.Parse(newValueStr);
        }
        catch
        {
            Console.Error.WriteLine(String.Format("Cannot parse the \"{0}\".", newValueStr));
            return;
        }

        if (newValue >= minValue && newValue <= maxValue)
        {
            setter(newValue);

            if (await JsonReader.SaveAsync<T>(configPath, config))
                Console.WriteLine("The new value was set successfully.");
        }
        else Console.Error.WriteLine(correctedValue);
    }
}