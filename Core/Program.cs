using SimplePomodoro.Infrastructure;
using SimplePomodoro.Models;
using SimplePomodoro.Resources;

namespace SimplePomodoro.Core;

/// <summary>
///     The main entry point of the Pomodoro application.
///     Handles configuration loading, command-line arguments, engine setup, and event wiring.
/// </summary>
internal class Program
{
    // --- Validation constraints for user settings ---

    /// <summary>Minimum allowed duration (minutes) for a work phase.</summary>
    private const float WorkPhaseDurationMin = 5.0f;
    /// <summary>Maximum allowed duration (minutes) for a work phase.</summary>
    private const float WorkPhaseDurationMax = 60.0f;

    /// <summary>Minimum allowed duration (minutes) for a short break phase.</summary>
    private const float BreakPhaseDurationMin = 1.0f;
    /// <summary>Maximum allowed duration (minutes) for a short break phase.</summary>
    private const float BreakPhaseDurationMax = 15.0f;

    /// <summary>Minimum allowed duration (minutes) for a long break phase.</summary>
    private const float LongBreakPhaseDurationMin = 0f;
    /// <summary>Maximum allowed duration (minutes) for a long break phase.</summary>
    private const float LongBreakPhaseDurationMax = 30.0f;

    /// <summary>Minimum number of cycles before a long break.</summary>
    private const int CyclesBeforeLongBreakMin = 0;
    /// <summary>Maximum number of cycles before a long break.</summary>
    private const int CyclesBeforeLongBreakMax = 10;

    /// <summary>Minimum total cycles in a Pomodoro session.</summary>
    private const int CyclesCountMin = 1;
    /// <summary>Maximum total cycles in a Pomodoro session.</summary>
    private const int CyclesCountMax = 99;

    // --- File paths ---

    /// <summary>Path to the pomodoro configuration JSON file.</summary>
    private static string PomodroConfigPath = string.Empty;
    /// <summary>Path to the application configuration JSON file.</summary>
    private static string AppConfigPath = string.Empty;

    // --- Loaded configuration objects ---

    /// <summary>Application-wide settings (music dir, volumes, etc.).</summary>
    private static ApplicationConfig? _appConfig;
    /// <summary>Pomodoro specific settings (durations, cycles, etc.).</summary>
    private static PomodoroConfig? _pomodoroConfig;

    /// <summary>
    ///     Application entry point. Loads configurations, processes command-line arguments,
    ///     creates the engine and audio controller, wires events, and starts the Pomodoro session.
    /// </summary>
    /// <param name="args">Command-line arguments (start/set/get).</param>
    public static async Task Main(string[] args)
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        AppConfigPath = appDir + "Config/appConfig.json";
        PomodroConfigPath = appDir + "Config/pomodoroConfig.json";

        // Load JSON configurations
        _appConfig =
            await JsonReader.ReadAsync<ApplicationConfig>(AppConfigPath);
        _pomodoroConfig =
            await JsonReader.ReadAsync<PomodoroConfig>(PomodroConfigPath);

        if (_appConfig == null || _pomodoroConfig == null)
        {
            return; // missing configuration – cannot proceed
        }

        // --- Command-line handling (set/get/start) ---
        if (args.Length != 0)
        {
            switch (args[0])
            {
                case Commands.StartCommandName:
                    // Continue to normal startup
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

        // --- Build asset paths ---
        string tickingPath = appDir + "Assets/Audio/Timer/ticking.mp3";
        string endBellPath = appDir + "Assets/Audio/Timer/end_bell.mp3";

        string musicPath;
        if (_appConfig.MusicDir is not null &&
            Path.IsPathFullyQualified(_appConfig.MusicDir))
            musicPath = _appConfig.MusicDir;
        else
            musicPath = appDir + "Assets/Audio/Music";

        // --- Core components ---
        Engine engine = new(_appConfig, _pomodoroConfig);
        AudioControl audioControl = new(tickingPath,
                                        endBellPath,
                                        musicPath,
                                        _appConfig.DefaultPhaseEndBellVolume,
                                        _appConfig.DefaultTickingVolume,
                                        _appConfig.DefaultMusicVolume);

        audioControl.Init();

        // --- Event wiring (UI / keyboard handlers) ---
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

        // --- Start the Pomodoro session ---
        await engine.StartAsync();
    }

    /// <summary>Handles the 'set' command: updates configuration values and saves them to disk.</summary>
    /// <param name="args">Array containing [fieldName, newValue].</param>
    private static async Task SetHandlerAsync(string[] args)
    {
        if (string.IsNullOrEmpty(args[0]))
        {
            Console.Error.WriteLine(Messages.Error_UnknownField);
            return;
        }

        if (string.IsNullOrEmpty(args[1]))
        {
            Console.Error.WriteLine(Messages.Error_IncorrectValue);
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
                    v => _pomodoroConfig!.WorkPhaseDuration = v,
                    String.Format(Messages.Error_WorkPhaseOutOfRange,
                                  WorkPhaseDurationMin,
                                  WorkPhaseDurationMax));
                break;

            case Commands.BreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    BreakPhaseDurationMin,
                    BreakPhaseDurationMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    v => _pomodoroConfig!.BreakPhaseDuration = v,
                    String.Format(Messages.Error_BreakPhaseOutOfRange,
                                  BreakPhaseDurationMin,
                                  BreakPhaseDurationMax));
                break;

            case Commands.LongBreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    LongBreakPhaseDurationMin,
                    LongBreakPhaseDurationMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    v => _pomodoroConfig!.LongBreakDuration = v,
                    String.Format(Messages.Error_LongBreakPhaseOutOfRange,
                                  LongBreakPhaseDurationMin,
                                  LongBreakPhaseDurationMax));
                break;

            case Commands.CyclesBeforeLongBreakSubcommandName:
                await Setting<PomodoroConfig>(value,
                    CyclesBeforeLongBreakMin,
                    CyclesBeforeLongBreakMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    v => _pomodoroConfig!.CyclesBeforeLongBreak = v,
                    String.Format(Messages.Error_CyclesBeforeLongBreakOutOfRange,
                                  CyclesBeforeLongBreakMin,
                                  CyclesBeforeLongBreakMax));
                break;

            case Commands.CyclesCountSubcommandName:
                await Setting<PomodoroConfig>(value,
                    CyclesCountMin,
                    CyclesCountMax,
                    PomodroConfigPath,
                    _pomodoroConfig!,
                    v => _pomodoroConfig!.CyclesCount = v,
                    String.Format(Messages.Error_CyclesCountOutOfRange,
                                  CyclesCountMin,
                                  CyclesCountMax));
                break;

            case Commands.MusicDirSubcommandName:
                if (Directory.Exists(value))
                {
                    _appConfig!.MusicDir = value;
                    if (await JsonReader.SaveAsync(AppConfigPath, _appConfig))
                        Console.WriteLine(Messages.Info_ValueSetSuccessfully);
                }
                else Console.Error.WriteLine(Messages.Error_PathNotFound);
                break;
        }
    }

    /// <summary>Handles the 'get' command: displays current configuration values.</summary>
    /// <param name="field">Configuration field name.</param>
    private static void GetHandler(string field)
    {
        if (string.IsNullOrEmpty(field))
            return;

        if (_pomodoroConfig is null || _appConfig is null)
            return;

        switch (field)
        {
            case Commands.WorkSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_WorkPhaseValue,
                    _pomodoroConfig.WorkPhaseDuration));
                break;
            case Commands.BreakSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_BreakPhaseValue,
                    _pomodoroConfig.BreakPhaseDuration));
                break;
            case Commands.LongBreakSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_LongBreakPhaseValue,
                    _pomodoroConfig.LongBreakDuration));
                break;
            case Commands.CyclesBeforeLongBreakSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_CyclesBeforeLongBreakValue,
                    _pomodoroConfig.CyclesBeforeLongBreak));
                break;
            case Commands.CyclesCountSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_CyclesCountValue,
                    _pomodoroConfig.CyclesCount));
                break;
            case Commands.MusicDirSubcommandName:
                Console.WriteLine(String.Format(
                    Messages.Info_MusicDirValue,
                    _appConfig.MusicDir));
                break;
        }
    }

    /// <summary>Generic helper to update a <see cref="float"/> setting, validate bounds, and persist.</summary>
    /// <typeparam name="T">Configuration type (usually <see cref="PomodoroConfig"/>).</typeparam>
    /// <param name="newValueStr">String representation of the new value.</param>
    /// <param name="minValue">Minimum allowed value.</param>
    /// <param name="maxValue">Maximum allowed value.</param>
    /// <param name="configPath">Path to the JSON file to save.</param>
    /// <param name="config">Configuration object to update.</param>
    /// <param name="setter">Action that applies the new value to the config.</param>
    /// <param name="correctedValue">Error message shown when validation fails.</param>
    private static async Task Setting<T>(string newValueStr,
        float minValue,
        float maxValue,
        string configPath,
        T config,
        Action<float> setter,
        string correctedValue)
    {
        if (!float.TryParse(newValueStr, out float newValue))
        {
            Console.Error.WriteLine(String.Format(
                Messages.Error_CannotParse,
                newValueStr));
            return;
        }

        if (newValue >= minValue && newValue <= maxValue)
        {
            setter(newValue);
            if (await JsonReader.SaveAsync(configPath, config))
                Console.WriteLine(Messages.Info_ValueSetSuccessfully);
        }
        else Console.Error.WriteLine(correctedValue);
    }

    /// <summary>Generic helper to update an <see cref="int"/> setting, validate bounds, and persist.</summary>
    /// <typeparam name="T">Configuration type.</typeparam>
    /// <param name="newValueStr">String representation of the new value.</param>
    /// <param name="minValue">Minimum allowed value.</param>
    /// <param name="maxValue">Maximum allowed value.</param>
    /// <param name="configPath">Path to the JSON file to save.</param>
    /// <param name="config">Configuration object to update.</param>
    /// <param name="setter">Action that applies the new integer value.</param>
    /// <param name="correctedValue">Error message shown when validation fails.</param>
    private static async Task Setting<T>(string newValueStr,
        int minValue,
        int maxValue,
        string configPath,
        T config,
        Action<int> setter,
        string correctedValue)
    {
        if (!int.TryParse(newValueStr, out int newValue))
        {
            Console.Error.WriteLine(String.Format(
                Messages.Error_CannotParse,
                newValueStr));
            return;
        }

        if (newValue >= minValue && newValue <= maxValue)
        {
            setter(newValue);
            if (await JsonReader.SaveAsync(configPath, config))
                Console.WriteLine(Messages.Info_ValueSetSuccessfully);
        }
        else Console.Error.WriteLine(correctedValue);
    }
}