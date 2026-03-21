using Microsoft.Extensions.DependencyInjection;
using Pomodoro.Core.Interfaces;
using Pomodoro.Infrastructure.Audio;
using Pomodoro.Infrastructure.Configuration;
using Pomodoro.Infrastructure.UI;

namespace Pomodoro
{
    class Program
    {
        private static CancellationTokenSource cts = new();

        public static async Task Main(string[] args)
        {
            //var host = Host.CreateDefaultBuilder(args)
            //    .ConfigureServices((context, services) =>
            //    {
            //        services.AddSingleton<ISettingsRepository>(sp =>
            //            new JsonSettingsRepository("config.json"));

            //        services.AddSingleton<IUserInterface, PomodoroConsoleUI>();
            //        services.AddSingleton<IInputHandler, PomodoroConsoleHandler>();
            //        services.AddSingleton<IAudioService>(sp =>
            //            new PomodoroAudio());

            //        services.AddSingleton<Application>();
            //    })
            //    .Build();

            //var app = host.Services.GetRequiredService<Application>();
            //await app.RunAsync(args, cts.Token);

            var services = new ServiceCollection();

            services.AddSingleton<ISettingsRepository, JsonSettingsRepository>(sp =>
                        new JsonSettingsRepository("config.json"));
            services.AddSingleton<IUserInterface, PomodoroConsoleUI>();
            services.AddSingleton<IInputHandler, PomodoroConsoleHandler>();
            services.AddSingleton<IAudioService, PomodoroAudio>();
            services.AddScoped<Application>();

            var serviceProvider = services.BuildServiceProvider();

            var app = serviceProvider.GetRequiredService<Application>();
            await app.RunAsync(args, cts.Token);
        }
    }
}