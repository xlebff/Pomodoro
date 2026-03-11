using Spectre.Console;
using Spectre.Console.Rendering;

namespace Pomodoro
{
    internal class PomodoroConsoleUI
    {
        public bool IsPrinting => _isPrinting;


        private readonly string[] PomodoroPhaseNames = ["Working..", "Resting.."];

        private readonly PomodoroEngine _engine;

        private bool _isPrinting = false;
        
        private CancellationTokenSource? _progressBarCts = new();

        private CancellationTokenSource? _printingCts;


        public PomodoroConsoleUI(PomodoroEngine engine)
        {
            _engine = engine;
            _engine.OnPhaseStart += OnPhaseStart;
            _engine.OnPhaseEnd += OnPhaseEnd;
            _engine.OnPomodoroEnd += OnPomodoroEnd;
            _engine.OnPomodoroInt += OnPomodoroInt;
        }


        private async Task SerialPrint(string text, CancellationToken token)
        {
            _isPrinting = true;

            foreach (char c in text)
            {
                if (token.IsCancellationRequested)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.Write(text);
                    _isPrinting = false;
                    return;
                }

                AnsiConsole.Write(c);
                await Task.Delay(100);
            }
            await Task.Delay(100);

            _isPrinting = false;
        }

        private async Task SerialPrintln(string text, CancellationToken token)
        {
            _isPrinting = true;

            await Task.Delay(50);

            foreach (char c in text)
            {
                if (token.IsCancellationRequested)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.WriteLine(text);
                    _isPrinting = false;
                    return;
                }

                AnsiConsole.Write(c);
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
            await Task.Delay(500);
            AnsiConsole.WriteLine();

            _isPrinting = false;
        }


        public void Skip()
        {
            _printingCts?.Cancel();
        }

        public async Task WelcomeMessageAsync()
        {
            _printingCts?.Dispose();
            _printingCts = new();

            await SerialPrintln("Hey there! Welcome to your cozy " +
                "Pomodoro timer!\nI'm here to help you stay focused " +
                "and take sweet breaks.\nReady to dive in? Let's make " +
                "today productive and fun! (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧",
                _printingCts!.Token);
        }

        public async Task EndMessageAsync()
        {
            await Task.Delay(100);

            _printingCts?.Dispose();
            _printingCts = new();

            await SerialPrintln("Great job!\n" +
                "You've completed your Pomodoro session! " +
                "I'm so proud of you for staying focused.\n" +
                "Keep up the amazing work. " +
                "See you next session! (◕‿◕) ❤️",
                _printingCts!.Token);
        }

        public async Task IntMessageAsync()
        {
            _printingCts?.Dispose();
            _printingCts = new();

            await SerialPrintln("Hope to see you soon!",
                _printingCts!.Token);
        }

        public async Task DrawProgressBar()
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
                    var task = ctx.AddTask(PomodoroPhaseNames[(int)_engine.Phase],
                        maxValue: _engine.CurrentDuration.TotalSeconds);

                    while (!ctx.IsFinished)
                    {
                        task.Value = _engine.Elapsed.TotalSeconds;

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
            _ = Task.Run(DrawProgressBar);
        }

        public void OnPhaseEnd(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
            AnsiConsole.Clear();
        }

        public void OnPomodoroEnd(object? sender, EventArgs e)
        {
            _progressBarCts?.Cancel();
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
            return new Markup(remaining.ToString(@"mm\:ss"));
        }
    }
}