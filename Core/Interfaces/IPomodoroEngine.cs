namespace Pomodoro.Core.Interfaces
{
    internal interface IPomodoroEngine
    {
        Task StartAsync();
        Task Pause();
        Task Skip();
        Task Quit();
    }
}
