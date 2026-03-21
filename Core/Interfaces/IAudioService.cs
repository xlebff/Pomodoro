namespace Pomodoro.Core.Interfaces
{
    internal interface IAudioService
    {
        Task PlayAlarmAsync(object? sender, EventArgs e);
        Task PlayTickAsync(object? sender, EventArgs e);
    }
}
