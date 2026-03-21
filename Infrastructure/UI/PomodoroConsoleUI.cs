using Pomodoro.Core.Engine;
using Pomodoro.Core.Interfaces;
using Pomodoro.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Pomodoro.Infrastructure.UI
{
    internal enum ConsolePrintingState { Panding, Typing, Completed }

    internal class PomodoroConsoleUI() : IUserInterface
    {
        private const float defaultDelay = 30;

        public ConsolePrintingState PrintingState => _state;


        private readonly string[] PomodoroPhaseNames = ["Working..", "Resting.."];

        private ConsolePrintingState _state = ConsolePrintingState.Panding;
        
        private CancellationTokenSource? _progressBarCts = new();

        private CancellationTokenSource? _typingCts, _completedCts;


        private async Task SerialPrint(string text, 
            CancellationToken typingToken)
        {
            _state = ConsolePrintingState.Typing;

            int startPosition = Console.CursorTop;

            foreach (char c in text)
            {
                if (typingToken.IsCancellationRequested)
                {
                    int currentPosition = Console.CursorTop;
                    string clear = new(' ', 
                        (currentPosition - startPosition) * Console.BufferWidth);
                    Console.SetCursorPosition(0, startPosition);
                    Console.Write(clear);
                    Console.SetCursorPosition(0, startPosition);
                    Console.WriteLine(text);
                    break;
                }

                Console.Write(c);

                switch (c)
                {
                    case ',':
                    case '.':
                    case '!':
                    case '?':
                    case '\n':
                        await Task.Delay(300);
                        break;
                    default:
                        await Task.Delay(50);
                        break;
                }
            }

            _state = ConsolePrintingState.Completed;
        }

        private async Task SerialPrintln(string text,
            CancellationToken typingToken)
        {
            await SerialPrint(text, typingToken);
            Console.WriteLine();
        }


        public async Task WriteMessageAsync(string message,
            float delay = 0)
        {
            _typingCts?.Dispose();
            _typingCts = new();

            _completedCts?.Dispose();
            _completedCts = new();

            await SerialPrintln(message,
                _typingCts!.Token);

            /* task.delay before returning */
            /* _state = Panding; */
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay),
                    _completedCts.Token);
            }
            catch (OperationCanceledException)
            {
                ;
            }
            finally
            {
                _state = ConsolePrintingState.Panding;
            }
        }


        public void Skip()
        {
            switch (_state)
            {
                case ConsolePrintingState.Panding:
                    return;
                case ConsolePrintingState.Typing:
                    _typingCts?.Cancel();
                    break;
                case ConsolePrintingState.Completed:
                    _completedCts?.Cancel();
                    break;

            }
        }


        public async Task DrawProgressBarAsync(PomodoroEngine e)
        {
            AnsiConsole.Clear();

            await AnsiConsole.Progress()
                .Columns(
                    new SpinnerColumn(),
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new CustomRemainingTimeColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(PomodoroPhaseNames[(int)e.Phase],
                        maxValue: e.CurrentDuration.TotalSeconds);

                    while (!ctx.IsFinished)
                    {
                        task.Value = e.Elapsed.TotalSeconds;

                        if (_progressBarCts!.IsCancellationRequested)
                        {
                            AnsiConsole.Clear();
                            break;
                        }

                        await Task.Delay(10);
                    }
                });

            AnsiConsole.Clear();
        }


        public async Task OnPhaseStartAsync(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();

            await Task.Delay(100);

            AnsiConsole.Clear();

            _progressBarCts?.Dispose();
            _progressBarCts = new();
            _ = Task.Run(() => DrawProgressBarAsync((PomodoroEngine)sender!));
        }

        public async Task OnPhaseEndAsync(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
            AnsiConsole.Clear();
        }

        public async Task OnPomodoroStartAsync(object? sender, EventArgs e)
        {
            await WriteMessageAsync(Messages.Start, defaultDelay);
        }

        public async Task OnPomodoroIntAsync(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
        }

        public async Task OnPomodoroEndAsync(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
            await WriteMessageAsync(Messages.End);
        }
    }

    public class CustomRemainingTimeColumn : ProgressColumn
    {
        public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
        {
            var remaining = TimeSpan.FromSeconds(task.MaxValue - task.Value);
            return new Markup($"[blue]{remaining:mm\\:ss}[/]");
        }
    }
}