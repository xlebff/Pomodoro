using Spectre.Console;

namespace Pomodoro
{
    internal class PomodoroConsoleUI
    {
        private readonly string[] PomodoroPhaseNames = ["Working..", "Resting.."];

        private readonly PomodoroEngine _engine;
        
        private CancellationTokenSource _cts = new();

        public PomodoroConsoleUI(PomodoroEngine engine)
        {
            _engine = engine;
            _engine.OnPhaseStart += OnPhaseStart;
            _engine.OnPhaseOver += OnPhaseOver;
            _engine.OnPomodoroEnd += OnPomodoroEnd;
        }

        private async Task SerialPrint(string text)
        {
            foreach (char c in text)
            {
                AnsiConsole.Write(c);
                await Task.Delay(100);
            }
            await Task.Delay(100);
        }

        private async Task SerialPrintln(string text)
        {
            foreach (char c in text)
            {
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
        }

        public async Task WelcomeMessageAsync()
        {
            await SerialPrintln("Hey there! Welcome to your cozy " +
                "Pomodoro timer! \nI'm here to help you stay focused " +
                "and take sweet breaks.\nReady to dive in? Let's make " +
                "today productive and fun! (ﾉ◕ヮ◕)ﾉ*:･ﾟ✧");
        }

        public async Task EndMessageAsync()
        {
            await SerialPrintln("🌟 Great job! 🌟\n" +
                "You've completed your Pomodoro session! " +
                "I'm so proud of you for staying focused. " +
                "Keep up the amazing work, one tomato at a time.\n" +
                "See you next session!(◕‿◕) ❤️");
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
                    new RemainingTimeColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask(PomodoroPhaseNames[(int)_engine.Phase],
                        maxValue: _engine.CurrentDuration.TotalSeconds);

                    while (!ctx.IsFinished)
                    {
                        task.Value = _engine.Elapsed.TotalSeconds;

                        if (_cts.IsCancellationRequested)
                        {
                            AnsiConsole.Clear();
                            return;
                        }

                        await Task.Delay(10);
                    }
                });
        }

        public void OnPhaseStart(object? sender, EventArgs e)
        {
            _cts.Cancel();
            AnsiConsole.Clear();
            _cts = new();
            _ = Task.Run(DrawProgressBar);
        }

        public void OnPhaseOver(object? sender, EventArgs e)
        {
            _cts.Cancel();
            AnsiConsole.Clear();
            _cts = new();
            _ = Task.Run(DrawProgressBar);
        }

        public void OnPomodoroEnd(object? sender, EventArgs e)
        {
            _cts.Cancel();
        }
    }
}