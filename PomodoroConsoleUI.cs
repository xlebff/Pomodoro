using Pomodoro.Resources;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Pomodoro
{
    internal enum ConsolePrintingState { Panding, Typing, Completed }

    internal class PomodoroConsoleUI()
    {
        public ConsolePrintingState PrintingState => _state;


        private readonly string[] PomodoroPhaseNames = ["Working..", "Resting.."];

        private ConsolePrintingState _state = ConsolePrintingState.Panding;
        
        private CancellationTokenSource? _progressBarCts = new();

        private CancellationTokenSource? _typingCts, _completedCts;


        private async Task SerialPrint(string text, 
            CancellationToken typingToken,
            CancellationToken completedToken)
        {
            _state = ConsolePrintingState.Typing;

            await Task.Delay(50);

            await AnsiConsole.Live(Text.Empty)
                .StartAsync(async ctx =>
                {
                    for (int i = 0; i < text.Length; ++i)
                    {
                        if (typingToken.IsCancellationRequested)
                        {
                            ctx.UpdateTarget(new Text(text));
                            break;
                        }

                        ctx.UpdateTarget(new Text(text[..(i + 1)]));

                        switch (text[i])
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
                });

            _state = ConsolePrintingState.Completed;

            /* task.delay before returning */
            /* _state = Panding; */
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), completedToken);
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

        private async Task SerialPrintln(string text,
            CancellationToken typingToken,
            CancellationToken completedToken)
        {
            await SerialPrint(text, typingToken, completedToken);
            AnsiConsole.WriteLine();
        }


        public async Task Message(string text)
        {
            _typingCts?.Dispose();
            _typingCts = new();

            _completedCts?.Dispose();
            _completedCts = new();

            await SerialPrint(text,
                _typingCts!.Token, _completedCts!.Token);
        }

        public async Task WelcomeMessageAsync(object sender, EventArgs e) =>
            await Message(Messages.Welcome);

        public async Task EndMessageAsync() =>
            await Message(Messages.Final);

        public async Task IntMessageAsync(object sender, EventArgs e) =>
            await Message(Messages.Interruption);


        public void Skip()
        {
            if (_state == ConsolePrintingState.Completed)
                _completedCts?.Cancel();
            else _typingCts?.Cancel();
        }


        public async Task DrawProgressBar(PomodoroEngine e)
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

        public async void OnPhaseStart(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();

            await Task.Delay(100);

            AnsiConsole.Clear();

            _progressBarCts?.Dispose();
            _progressBarCts = new();
            _ = Task.Run(() => DrawProgressBar((PomodoroEngine)sender!));
        }

        public void OnPhaseEnd(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
            AnsiConsole.Clear();
        }

        public async Task OnPomodoroEnd(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
            await EndMessageAsync();
        }

        public void OnPomodoroInt(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
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