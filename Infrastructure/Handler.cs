namespace SimplePomodoro.Infrastructure;

internal class Handler
{
    public static event EventHandler? OnPause;
    public static event EventHandler? OnSkip;
    public static event EventHandler? OnNext;
    public static event EventHandler? OnPrevious;
    public static event EventHandler? OnUp;
    public static event EventHandler? OnDown;


    public static async Task Init()
    {
        while (true)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKey key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.P:
                        OnPause?.Invoke(null, EventArgs.Empty);
                        break;

                    case ConsoleKey.S:
                        OnSkip?.Invoke(null, EventArgs.Empty);
                        break;

                    case ConsoleKey.RightArrow:
                        OnNext?.Invoke(null, EventArgs.Empty);
                        break;

                    case ConsoleKey.LeftArrow:
                        OnPrevious?.Invoke(null, EventArgs.Empty);
                        break;

                    case ConsoleKey.UpArrow:
                        OnUp?.Invoke(null, EventArgs.Empty);
                        break;

                    case ConsoleKey.DownArrow:
                        OnDown?.Invoke(null, EventArgs.Empty);
                        break;

                    default: break;
                }
            }
            await Task.Delay(50, CancellationToken.None);
        }
    }
}
