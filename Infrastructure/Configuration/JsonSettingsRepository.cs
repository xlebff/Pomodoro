using Pomodoro.Core.Interfaces;
using Pomodoro.Core.Models;
using Pomodoro.Resources;
using System.Text;
using System.Text.Json;

namespace Pomodoro.Infrastructure.Configuration
{
    internal class JsonSettingsRepository(string filePath)
        : ISettingsRepository
    {
        private readonly string _filePath = filePath;

        private readonly JsonSerializerOptions jso =
                            new() { WriteIndented = true };

        private PomodoroSettings? _settings = null;


        public PomodoroSettings? GetCurrentSettings() => _settings;


        public async Task LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine(Messages.ConfigNotFoundError);
                await CreateDefaultConfig(_filePath);
            }

            string json;
            /* reading json */
            try
            {
                json = await File.ReadAllTextAsync(_filePath, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException
                                    || ex is UnauthorizedAccessException)
            {
                Console.WriteLine(Messages.ConfigReadingError +
                    ex.Message);
                return;
            }

            PomodoroSettings? settings;
            /* recording json to settings */
            try
            {
                settings = JsonSerializer.Deserialize<PomodoroSettings>(json);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(Messages.ConfigInvalidJsonError +
                    ex.Message);
                return;
            }

            if (settings == null)
            {
                Console.WriteLine(Messages.ConfigInvalidDataError);
                return;
            }

            if (!ConfigurationValidating(settings))
                return;

            _settings = settings;
        }

        public async Task SaveAsync()
        {
            try
            {
                await File.WriteAllTextAsync(_filePath,
                    JsonSerializer.Serialize(_settings, jso));
            }
            catch (IOException ex)
            {
                Console.WriteLine(Messages.ConfigSaveError + ex.Message);
            }
        }


        private async Task CreateDefaultConfig(string filePath)
        {
            var defaultSettings = new PomodoroSettings
            {
                SetsCount = 4,
                WorkingPhaseMinutes = 25,
                RestingPhaseMinutes = 5,
                LongRestingPhaseMinutes = null,
                SetsUntilLongResting = null
            };

            string json = JsonSerializer.Serialize(defaultSettings, jso);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
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
    }
}
