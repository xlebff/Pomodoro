using Pomodoro.Resources;
using System.Text;
using System.Text.Json;

namespace Pomodoro
{
    class Program
    {
        private const string configPath = "config.json";

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


        private static readonly JsonSerializerOptions jso =
                            new() { WriteIndented = true };


        private static PomodoroSettings? settings;

        private static PomodoroConsoleUI? gui;

        private static bool isActive = false;

        private static CancellationTokenSource? cts;


        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if ((settings = await LoadConfidurationAsync(configPath)) == null)
                return 1;

            gui = new PomodoroConsoleUI();
            cts = new();
            _ = Task.Run(() => PomodoroConsoleHandler.HandleTypingInput(
                gui, cts.Token));

            if (args.Length > 0)
            {
                await CommandHandler(args);
                return 0;
            }

            while (!isActive)
            {
                await CommandHandler(Console.ReadLine()?.Split(' '));
            }

            return 0;
        }

        
        private static async Task<int> CommandHandler(string[]? args)
        {
            if (args == null || args.Length == 0) return 1;

            switch (args[0].ToLower())
            {
                case CommandStart:
                    await Start();
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
                    await gui!.Message(Messages.Help);
                    break;
                case CommandClear:
                    Console.Clear();
                    break;
                default:
                    await gui!.Message(Messages.CommandHandleError);
                    break;
            }

            return 0;
        }

        private static async Task GetHandlerAsync(string[]? args)
        {
            Console.WriteLine();
            if (args == null || args.Length == 0)
            {
                await gui!.Message(Messages.GetHelp);
                return;
            }

            switch (args[0].ToLower())
            {
                case CommandWork:
                    await gui!.Message(Messages.CurrentWorkingDuration +
                        settings!.WorkingPhaseMinutes);
                    break;
                case CommandRest:
                    await gui!.Message(Messages.CurrentRestingDuration +
                        settings!.RestingPhaseMinutes);
                    break;
                case CommandLong:
                    await gui!.Message(Messages.CurrentLongRestingDuration +
                        settings!.LongRestingPhaseMinutes);
                    break;
                case CommandSets:
                    await gui!.Message(Messages.CurrentCycles +
                        settings!.SetsCount);
                    break;
                case CommandLongEvery:
                    await gui!.Message(Messages.CurrentLongEvery +
                        settings!.SetsUntilLongResting);
                    break;
                default:
                    await gui!.Message(Messages.CommandHandleError);
                    break;

            }

            Console.WriteLine();
        }

        private static async Task SetHandlerAsync(string[]? args)
        {
            Console.WriteLine();
            if (args == null || args.Length <= 1)
            {
                await gui!.Message(Messages.SetHelp);
                return;
            }

            switch (args[0].ToLower())
            {
                case CommandWork:
                    await HandleSetCommand(args[1],
                        WorkingPhaseDurationMin, WorkingPhaseDurationMax,
                        () => settings!.WorkingPhaseMinutes,
                        v => settings!.WorkingPhaseMinutes = v,
                        Messages.WorkingDurationSetError);
                    break;
                case CommandRest:
                    await HandleSetCommand(args[1],
                        RestingPhaseDurationMin, RestingPhaseDurationMax,
                        () => settings!.RestingPhaseMinutes,
                        v => settings!.RestingPhaseMinutes = v,
                        Messages.RestingDurationSetError);
                    break;
                case CommandLong:
                    await HandleSetCommand(args[1],
                        LongRestingPhaseDurationMin, 
                        LongRestingPhaseDurationMax,
                        () => settings!.LongRestingPhaseMinutes,
                        v => settings!.LongRestingPhaseMinutes = v,
                        Messages.LongRestingDurationSetError);
                    break;
                case CommandSets:
                    await HandleSetCommand(args[1],
                        SetsMin, SetsMax,
                        () => settings!.SetsCount,
                        v => settings!.SetsCount = v,
                        Messages.CyclesSetError);
                    break;
                case CommandLongEvery:
                    await HandleSetCommand(args[1],
                        SetsUntilLongRestMin,
                        SetsUntilLongRestMax,
                        () => settings!.SetsUntilLongResting,
                        v => settings!.SetsUntilLongResting = v,
                        Messages.LongEverySetError);
                    break;
                default:
                    await gui!.Message(Messages.CommandHandleError);
                    break;

            }
            
            Console.WriteLine();
        }

        private static async Task HandleSetCommand(
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
                    File.WriteAllText(configPath, 
                        JsonSerializer.Serialize(settings, jso));
                }
                await gui!.Message(Messages.SuccessfullySet +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
            else
            {
                await gui!.Message(errorMessage +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
        }

        private static async Task HandleSetCommand(
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
                    File.WriteAllText(configPath,
                        JsonSerializer.Serialize(settings, jso));
                }
                await gui!.Message(Messages.SuccessfullySet +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
            else
            {
                await gui!.Message(errorMessage +
                    "\n" +
                    Messages.CurrentValue +
                    getter());
            }
        }


        private static async Task<int> Start()
        {
            isActive = true;
            Console.Clear();

            var engine = new PomodoroEngine(
                TimeSpan.FromMinutes(settings!.WorkingPhaseMinutes),
                TimeSpan.FromMinutes(settings!.RestingPhaseMinutes),
                settings!.SetsCount,
                settings!.LongRestingPhaseMinutes is not null ?
                    TimeSpan.FromMinutes((double)settings!.LongRestingPhaseMinutes) :
                    null,
                settings!.SetsUntilLongResting);

            engine.OnPhaseStart += gui!.OnPhaseStart;
            engine.OnPhaseEnd += gui!.OnPhaseEnd;
            engine.OnPomodoroEnd += gui!.OnPomodoroEnd;
            engine.OnPomodoroInt += gui!.OnPomodoroInt;
            engine.OnPomodoroStart += gui!.WelcomeMessageAsync;

            var audio = new PomodoroAudio();

            engine.OnPhaseCountdown += audio.CountdownPlay;
            engine.OnPhaseEnd += audio.AlarmPlay;

            cts!.Cancel();

            PomodoroConsoleHandler handler = new();
            _ = Task.Run(() => handler.HandleInput(engine, gui));

            await engine.StartAsync();

            return 0;
        }


        private static void CreateDefaultConfig(string filePath)
        {
            var defaultSettings = new PomodoroSettings
            {
                SetsCount = 4,
                WorkingPhaseMinutes = 25,
                RestingPhaseMinutes = 5,
                LongRestingPhaseMinutes = null,
                SetsUntilLongResting = null
            };

            string json = JsonSerializer.Serialize(defaultSettings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        private static bool ConfigurationValidating(PomodoroSettings settings)
        {
            bool ret = true;

            /* if sets until long resing and long resting phase duration 
             has the same values */
            if (!(settings.SetsUntilLongResting.HasValue ^
                settings.LongRestingPhaseMinutes.HasValue))
            {
                /* if it has values */
                if (settings.SetsUntilLongResting.HasValue)
                {
                    /* checking the number of sets */
                    if (!(settings.SetsUntilLongResting is >= 1 and <= 10))
                    {
                        Console.WriteLine(Messages.CyclesSetError);
                        ret = false;
                    }

                    /* checking long rest phase duration */
                    if (!(settings.LongRestingPhaseMinutes is >= 5 and <= 30))
                    {
                        Console.WriteLine(Messages.LongEverySetError);
                        ret = false;
                    }
                } /* else nothing to check */
            }
            else /* if the values are different */
            {
                Console.WriteLine(Messages.LongRestSettingsError);
                settings.LongRestingPhaseMinutes = null;
                settings.SetsUntilLongResting = null;
            }

            if (!(settings.SetsCount is >= 1 and <= 99))
            {
                Console.WriteLine(Messages.CyclesSetError);
                ret = false;
            }

            if (!(settings.WorkingPhaseMinutes is >= 1 and <= 60))
            {
                Console.WriteLine(Messages.WorkingDurationSetError);
                ret = false;
            }

            if (!(settings.RestingPhaseMinutes is >= 1 and <= 15))
            {
                Console.WriteLine(Messages.RestingDurationSetError);
                ret = false;
            }

            return ret;
        }

        private static async Task<PomodoroSettings?> LoadConfidurationAsync(
            string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Configuration file not found. " +
                    "A default configuration will be used.");
                CreateDefaultConfig(path);
            }

            string json;
            /* reading json */
            try
            {
                json = await File.ReadAllTextAsync(path, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException
                                    || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Error reading configuration file: " +
                    $"{ex.Message}");
                return null;
            }

            PomodoroSettings settings;
            /* recording json to settings */
            try
            {
                settings = JsonSerializer.Deserialize<PomodoroSettings>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid JSON format: {ex.Message}");
                return null;
            }

            if (settings == null)
            {
                Console.WriteLine("Configuration file is empty or contains invalid data.");
                return null;
            }

            if (!ConfigurationValidating(settings))
                return null;

            return settings;
        }
    }
}