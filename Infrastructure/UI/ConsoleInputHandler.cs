using ConsolePomodoro.Core.Contracts;

namespace ConsolePomodoro.Infrastructure.UI
{
    internal class ConsoleInputHandler : IInputHandler
    {
        public event Func<ConsoleKey, Task>? KeyPressed;

        public async void StartListening(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (KeyPressed != null)
                        _ = KeyPressed.Invoke(key);
                }
                await Task.Delay(50, cancellationToken);
            }
        }
    }
}
