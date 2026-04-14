using System.Text;
using System.Text.Json;
using ConsolePomodoro.Core.Contracts;
using ConsolePomodoro.Domain.Models;
using ConsolePomodoro.Resources;

namespace ConsolePomodoro.Infrastructure.Configuration
{
    internal class ApplicationJsonConfigRepository
        : IJsonConfigRepository
    {
        private readonly JsonSerializerOptions _jso;
        private readonly string _configFilePath;
        private readonly ApplicationConfig _config;


        private ApplicationJsonConfigRepository(
            JsonSerializerOptions jso,
            string configFilePath,
            ApplicationConfig config)
        {
            _jso = jso;
            _configFilePath = configFilePath;
            _config = config;
        }


        public static async Task<(
            ApplicationJsonConfigRepository? res,
            bool success,
            string? message)> 
            CreateAsync(JsonSerializerOptions jso, string configFilePath)
        {
            (ApplicationConfig? Config, bool Success, string? Message) = 
                await JsonReader.ReadAsync<ApplicationConfig>(configFilePath);

            if (!Success ||
                Config is null)
            {
                return (null, false, Message);
            }

            return (new ApplicationJsonConfigRepository(
                        jso,
                        configFilePath,
                        Config),
                    true,
                    null);
        }


        public async Task<(bool Success, string? Message)> CreateDefaultConfig()
        {
            ApplicationConfig? defaultConfig = new();

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

            // Default music volume test
            Math.Clamp(_config.DefaultMusicVolume, 0f, 1f);

            // Default ticking volume test
            Math.Clamp(_config.DefaultTickingVolume, 0f, 1f);

            // Default phase end bell volume test
            Math.Clamp(_config.DefaultPhaseEndBellVolume, 0f, 1f);

            // Timer sound dir test
            try
            {
                Directory.GetFiles(_config.TimerSoundDir);
            }
            catch (UnauthorizedAccessException)
            {
                res = false;
                message += Messages.Error_NoAccessToMusicDir;
            }
            catch (DirectoryNotFoundException)
            {
                res = false;
                message += Messages.Error_WrongMusicDir;
            }

            // Music dir test
            if (_config.MusicDir is not null)
            {
                try
                {
                    Directory.GetFiles(_config.MusicDir);
                }
                catch (UnauthorizedAccessException)
                {
                    res = false;
                    message += Messages.Error_NoAccessToMusicDir;
                } catch (DirectoryNotFoundException)
                {
                    res = false;
                    message += Messages.Error_WrongMusicDir;
                }
            }

            return (res, message);
        }

        public async Task<(bool Success, string? Message)> SaveAsync()
        {
            return await JsonReader.SaveAsync<ApplicationConfig>(_configFilePath, _config);
        }
    }
}
