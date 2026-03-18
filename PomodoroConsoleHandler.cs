namespace Pomodoro
{
    internal class PomodoroConsoleHandler
    {
        public async Task HandleInput(PomodoroEngine engine, PomodoroConsoleUI gui)
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.Q:
                            engine.Quit();
                            return;
                        case ConsoleKey.P:
                            engine.Pause();
                            break;
                        case ConsoleKey.S:
                            if (gui.PrintingState != ConsolePrintingState.Panding)
                                gui.Skip();
                            else
                                engine.Skip();
                            break;
                        //case ConsoleKey.UpArrow:
                        //    AnsiConsole.MarkupLine("[yellow]Стрелка вверх[/]");
                        //    break;
                        //case ConsoleKey.DownArrow:
                        //    AnsiConsole.MarkupLine("[yellow]Стрелка вниз[/]");
                        //    break;
                    }
                }

                await Task.Delay(50);
            }
        }

        public static async Task HandleTypingInput(PomodoroConsoleUI gui,
            CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    return;

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    if (key == ConsoleKey.S)
                    {
                        if (gui.PrintingState != ConsolePrintingState.Panding)
                            gui.Skip();
                    }
                }

                await Task.Delay(50);
            }
        }
    }
}
