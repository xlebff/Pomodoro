using System.Text.Json;
using ConsolePomodoro.Core;
using ConsolePomodoro.Core.Contracts;
using ConsolePomodoro.Domain.Models;
using ConsolePomodoro.Engine;
using ConsolePomodoro.Infrastructure.Audio;
using ConsolePomodoro.Infrastructure.Configuration;
using ConsolePomodoro.Infrastructure.UI;
using ConsolePomodoro.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace ConsolePomodoro;

internal static class Program
{
    private const string SettingsDir = "./Settings";
    private const string ApplicationConfigPath = "./Settings/applicationConfig.json";
    private const string PomodoroConfigPath = "./Settings/pomodoroConfig.json";

    private static async Task<int> Main(string[] args)
    {
        using CancellationTokenSource appCts = new();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            appCts.Cancel();
        };

        try
        {
            using ServiceProvider serviceProvider = await BuildServiceProviderAsync();
            Application application = serviceProvider.GetRequiredService<Application>();

            await application.RunAsync(args, appCts.Token);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application startup failed: {ex.Message}");
            return 1;
        }
    }

    private static async Task<ServiceProvider> BuildServiceProviderAsync()
    {
        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        ApplicationJsonConfigRepository applicationConfigRepository =
            await LoadApplicationConfigRepositoryAsync(jsonOptions);
        ApplicationConfig applicationConfig =
            (ApplicationConfig)applicationConfigRepository.GetConfig();

        // 2) Then load pomodoro config, because timer engine depends on it.
        PomodoroJsonConfigRepository pomodoroConfigRepository =
            await LoadPomodoroConfigRepositoryAsync(jsonOptions);
        PomodoroConfig pomodoroConfig =
            (PomodoroConfig)pomodoroConfigRepository.GetConfig();

        string tickingSoundPath = ResolveAudioFile(
            applicationConfig.TimerSoundDir,
            ["ticking"],
            fallbackIndex: 0);

        string endBellSoundPath = ResolveAudioFile(
            applicationConfig.TimerSoundDir,
            ["end_bell"],
            fallbackIndex: 1);

        string musicDirectoryPath = applicationConfig.MusicDir ?? applicationConfig.TimerSoundDir;

        ServiceCollection services = new();

        services.AddSingleton(jsonOptions);

        services.AddScoped<IJsonConfigRepository, ApplicationJsonConfigRepository>(sp => applicationConfigRepository);
        services.AddScoped<IJsonConfigRepository, PomodoroJsonConfigRepository>(sp => pomodoroConfigRepository);
        //services.AddSingleton(applicationConfig);
        //services.AddSingleton(pomodoroConfig);

        services.AddSingleton<ConsoleUI>();
        services.AddSingleton<ConsoleInputHandler>();
        services.AddSingleton<Core.Contracts.IUserInterface>(sp => sp.GetRequiredService<ConsoleUI>());
        services.AddSingleton<Core.Contracts.IInputHandler>(sp => sp.GetRequiredService<ConsoleInputHandler>());

        services.AddSingleton<Core.Contracts.IAudioService>(_ =>
            new NAudioService(
                tickingSoundPath,
                endBellSoundPath,
                musicDirectoryPath,
                applicationConfig.DefaultPhaseEndBellVolume,
                applicationConfig.DefaultTickingVolume,
                applicationConfig.DefaultMusicVolume));

        services.AddTransient<PomodoroEngine>(_ =>
            new PomodoroEngine(
                TimeSpan.FromMinutes(pomodoroConfig.WorkPhaseDuration),
                TimeSpan.FromMinutes(pomodoroConfig.BreakPhaseDuration),
                pomodoroConfig.CyclesCount,
                pomodoroConfig.LongBreakDuration.HasValue
                    ? TimeSpan.FromMinutes(pomodoroConfig.LongBreakDuration.Value)
                    : null,
                pomodoroConfig.CyclesBeforeLongBreak));

        services.AddSingleton<Func<PomodoroEngine>>(sp => () => sp.GetRequiredService<PomodoroEngine>());
        services.AddSingleton<Application>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<ApplicationJsonConfigRepository> LoadApplicationConfigRepositoryAsync(
        JsonSerializerOptions jsonOptions)
    {
        await EnsureConfigExistsAsync(
            ApplicationConfigPath, 
            new ApplicationConfig(), 
            jsonOptions);

        var (repository, success, message) =
            await ApplicationJsonConfigRepository.CreateAsync(jsonOptions, ApplicationConfigPath);

        if (!success || repository is null)
        {
            throw new InvalidOperationException(String.Format(
                Messages.Error_ApplicationConfigLoadFailed,
                ApplicationConfigPath) + message);
        }

        var (Success, Message) = repository.Validate();
        if (!Success)
        {
            throw new InvalidOperationException(
                Messages.Error_InvalidApplicationConfig +
                '\n' +
                Message);
        }

        return repository;
    }

    private static async Task<PomodoroJsonConfigRepository> LoadPomodoroConfigRepositoryAsync(
        JsonSerializerOptions jsonOptions)
    {
        await EnsureConfigExistsAsync(PomodoroConfigPath, new PomodoroConfig(), jsonOptions);

        var (repository, success, message) =
            await PomodoroJsonConfigRepository.CreateAsync(jsonOptions, PomodoroConfigPath);

        if (!success || repository is null)
        {
            throw new InvalidOperationException(
                $"Failed to load pomodoro config '{PomodoroConfigPath}'. {message}");
        }

        var validation = repository.Validate();
        if (!validation.Success)
        {
            throw new InvalidOperationException(
                $"Pomodoro config validation failed. {validation.Message}");
        }

        return repository;
    }

    private static async Task EnsureConfigExistsAsync<TConfig>(
        string configPath,
        TConfig defaultConfig,
        JsonSerializerOptions jsonOptions)
    {
        Directory.CreateDirectory(SettingsDir);

        if (File.Exists(configPath) && new FileInfo(configPath).Length > 0)
        {
            return;
        }

        string json = JsonSerializer.Serialize(defaultConfig, jsonOptions);
        await File.WriteAllTextAsync(configPath, json);
    }

    private static string ResolveAudioFile(
        string directoryPath,
        string[] preferredNameFragments,
        int fallbackIndex)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Sound directory was not found: {directoryPath}");
        }

        string[] supportedFiles = Directory
            .EnumerateFiles(directoryPath)
            .Where(path =>
            {
                string extension = Path.GetExtension(path);
                return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (supportedFiles.Length == 0)
        {
            throw new FileNotFoundException(
                $"No .mp3 or .wav files were found in '{directoryPath}'.");
        }

        foreach (string fragment in preferredNameFragments)
        {
            string? matchedFile = supportedFiles.FirstOrDefault(path =>
                Path.GetFileName(path).Contains(fragment, StringComparison.OrdinalIgnoreCase));

            if (matchedFile is not null)
            {
                return matchedFile;
            }
        }

        if (fallbackIndex < supportedFiles.Length)
        {
            return supportedFiles[fallbackIndex];
        }

        return supportedFiles[0];
    }
}
