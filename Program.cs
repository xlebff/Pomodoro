using System.Text;
using System.Text.Json;

namespace Pomodoro
{
    class Program
    {
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

            string json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
        }

        public static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            const string configPath = "config.json";

            if (!File.Exists(configPath))
            {
                Console.WriteLine("Configuration file not found. A default configuration will be used.");
                CreateDefaultConfig(configPath);
            }

            string json;
            try
            {
                json = await File.ReadAllTextAsync(configPath, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"Error reading configuration file: {ex.Message}");
                return;
            }

            PomodoroSettings settings;
            try
            {
                settings = JsonSerializer.Deserialize<PomodoroSettings>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Invalid JSON format: {ex.Message}");
                return;
            }

            if (settings == null)
            {
                Console.WriteLine("Configuration file is empty or contains invalid data.");
                return;
            }

            var engine = new PomodoroEngine(
                TimeSpan.FromMinutes(settings.WorkingPhaseMinutes),
                TimeSpan.FromMinutes(settings.RestingPhaseMinutes),
                settings.SetsCount,
                settings.LongRestingPhaseMinutes is not null ? 
                    TimeSpan.FromMinutes((double)settings.LongRestingPhaseMinutes) :
                    null,
                settings.SetsUntilLongResting);

            if (!engine.ConfCheck())
            {
                Console.WriteLine("Settings contain invalid values. Please check the configuration.");
                return;
            }

            var gui = new PomodoroConsoleUI(engine);

            PomodoroConsoleHandler handler = new();

            _ = Task.Run(() => handler.HandleInput(engine, gui));

            await gui.WelcomeMessageAsync();

            await engine.StartAsync();

            await (engine.IsCompleted ?
                gui.EndMessageAsync() :
                gui.IntMessageAsync());
        }
    }
}