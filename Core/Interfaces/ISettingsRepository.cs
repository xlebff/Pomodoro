using Pomodoro.Core.Models;

namespace Pomodoro.Core.Interfaces
{
    internal interface ISettingsRepository
    {
        PomodoroSettings? GetCurrentSettings();
        Task LoadAsync();
        Task SaveAsync();
    }
}
