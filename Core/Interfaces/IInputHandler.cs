namespace Pomodoro.Core.Interfaces
{
    internal interface IInputHandler
    {
        event Func<ConsoleKey, Task> KeyPressed;
        void StartListening(CancellationToken cancellationToken);
    }
}
