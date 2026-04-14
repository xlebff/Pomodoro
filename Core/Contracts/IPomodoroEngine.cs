namespace ConsolePomodoro.Core.Contracts
{
    internal enum PomodoroPhase { Working, Resting }

    internal interface IPomodoroEngine
    {
        PomodoroPhase Phase { get; }
        TimeSpan CurrentDuration { get; }
        bool IsCompleted { get; }


        TimeSpan GetElapsed();

        Task StartAsync();

        void Pause();

        void Skip();

        Task Quit();
    }
}