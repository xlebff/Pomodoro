using System.Text;
using System.Text.Json;

namespace Pomodoro
{
    class Program
    {
        private const string configPath = "config.json";


        private static PomodoroSettings? settings;

        private static PomodoroConsoleUI? gui;

        private static bool isActive = false;

        private static CancellationTokenSource? cts;


        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            if ((settings = await LoadConfidurationAsync(configPath)) == null)
                return 1;

            var gui = new PomodoroConsoleUI();
            cts = new();
            _ = Task.Run(() => PomodoroConsoleHandler.HandleTypingInput(
                gui, cts.Token));

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
                case "start":
                    await Start();
                    break;
                case "set":
                    SetHandler(args[1..]);
                    break;
            }

            return 0;
        }

        private static void SetHandler(string[]? args)
        {
            //if ()
            //settings.WorkingPhaseMinutes = 10;
            //JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
            //var options = jsonSerializerOptions;
            //string updatedJson = JsonSerializer.Serialize(settings, options);
            //File.WriteAllText(configPath, updatedJson);
        }


        private static async Task<int> Start()
        {
            isActive = true;

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
                    /* checking number of sets */
                    if (!(settings.SetsUntilLongResting is >= 1 and <= 10))
                    {
                        Console.WriteLine("Invalid sets value before a " +
                            "long break. Acceptable values are " +
                            "from 1 to 10.");
                        ret = false;
                    }

                    /* checking long rest phase duration */
                    if (!(settings.LongRestingPhaseMinutes is >= 5 and <= 30))
                    {
                        Console.WriteLine("The long break value is " +
                            "incorrect. Acceptable values are " +
                            "from 5 to 30 minutes.");
                        ret = false;
                    }
                } /* else nothing to check */
            }
            else /* if the values are different */
            {
                Console.WriteLine("The long break values are not fully set." +
                    " A long break will be ignored.");
                settings.LongRestingPhaseMinutes = null;
                settings.SetsUntilLongResting = null;
            }

            if (!(settings.SetsCount is >= 1 and <= 99))
            {
                Console.WriteLine("Incorrect number of sets. " +
                    "Acceptable values are from 1 to 99.");
                ret = false;
            }

            if (!(settings.WorkingPhaseMinutes is >= 1 and <= 60))
            {
                Console.WriteLine("Incorrect duration of the working " +
                    "phase. Acceptable values are from 1 to 60 minutes.");
                ret = false;
            }

            if (!(settings.RestingPhaseMinutes is >= 1 and <= 15))
            {
                Console.WriteLine("Incorrect duration of the rest " +
                    "phase. Acceptable values are from 1 to 15 minutes.");
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