namespace Pomodoro.Core.Interfaces
{
    internal interface IUserInterface
    {
        Task OnPhaseStartAsync(object? sender, EventArgs e);
        Task OnPhaseEndAsync(object? sender, EventArgs e);
        Task OnPomodoroStartAsync(object? sender, EventArgs e);
        Task OnPomodoroIntAsync(object? sender, EventArgs e);
        Task OnPomodoroEndAsync(object? sender, EventArgs e);

        Task WriteMessageAsync(string message,
            float delay = 0);
        void Skip();
    }
}
