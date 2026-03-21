using Pomodoro.Core.Interfaces;

namespace Pomodoro.Infrastructure.UI
{
    internal class PomodoroConsoleHandler : IInputHandler
    {
        public event Func<ConsoleKey, Task>? KeyPressed;

        public async void StartListening(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (KeyPressed != null)
                        _ = KeyPressed.Invoke(key);
                }
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
