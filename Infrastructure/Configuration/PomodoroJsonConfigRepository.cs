using System.Text;
using System.Text.Json;
using ConsolePomodoro.Core.Contracts;
using ConsolePomodoro.Domain.Models;
using ConsolePomodoro.Resources;

namespace ConsolePomodoro.Infrastructure.Configuration
{
    internal class PomodoroJsonConfigRepository
        : IJsonConfigRepository
    {
        private readonly JsonSerializerOptions _jso;
        private readonly string _configFilePath;
        private readonly PomodoroConfig _config;


        private PomodoroJsonConfigRepository(
            JsonSerializerOptions jso,
            string configFilePath,
            PomodoroConfig config)
        {
            _jso = jso;
            _configFilePath = configFilePath;
            _config = config;
        }


        public static async Task<(
            PomodoroJsonConfigRepository? res,
            bool success,
            string? message)>
            CreateAsync(JsonSerializerOptions jso, string configFilePath)
        {
            (PomodoroConfig? Config, bool Success, string? Message) =
                await JsonReader.ReadAsync<PomodoroConfig>(configFilePath);

            if (!Success ||
                Config is null)
            {
                return (null, false, Message);
            }

            return (new PomodoroJsonConfigRepository(
                        jso,
                        configFilePath,
                        Config),
                    true,
                    null);
        }

        public async Task<(bool Success, string? Message)> CreateDefaultConfig()
        {
            PomodoroConfig? defaultConfig = new();

            try
            {
                string json = JsonSerializer.Serialize(defaultConfig, _jso);

                await File.WriteAllTextAsync(
                    _configFilePath,
                    json,
                    Encoding.UTF8);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public IConfig GetConfig() => _config;

        public (bool Success, string? Message) Validate()
        {
            bool res = true;
            string message = string.Empty;

            // Cycles count test
            Math.Clamp(_config.CyclesCount, 1, 99);

            // Work phase duration test
            Math.Clamp(_config.WorkPhaseDuration, 1f, 60f);

            // Break phase duration test
            Math.Clamp(_config.BreakPhaseDuration, 1f, 15f);

            // Long break test
            if ((_config.LongBreakDuration is null) == 
                (_config.CyclesBeforeLongBreak is null))
            {
                if (_config.LongBreakDuration is not null)
                {
                    Math.Clamp((float)_config.LongBreakDuration!,
                        5f, 30f);

                    Math.Clamp((int)_config.CyclesBeforeLongBreak!,
                        2, 10);
                }
            } else
            {
                _config.LongBreakDuration = null;
                _config.CyclesBeforeLongBreak = null;

                message += Messages.Error_InvalidLongBreakSettings;
            }

            return (res, message);
        }

        public async Task<(bool Success, string? Message)> SaveAsync()
        {
            return await JsonReader.SaveAsync<PomodoroConfig>(_configFilePath, _config);
        }
    }
}
