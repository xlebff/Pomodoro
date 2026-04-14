namespace ConsolePomodoro.Domain.Models
{
    /// <summary>
    /// Pomodoro timer settings loading from JSON.
    /// </summary>
    internal class PomodoroConfig : IConfig
    {
        /// <summary>
        /// The total number of Pomodoro cycles (1 - 99).
        /// </summary>
        public int CyclesCount { get; set; }
        /// <summary>
        /// The duration of the work phase in minutes (1.00f - 60.00f).
        /// </summary>
        public float WorkPhaseDuration { get; set; }
        /// <summary>
        /// The duration of the break phase in minutes (1.00f - 15.00f).
        /// </summary>
        public float BreakPhaseDuration { get; set; }
        /// <summary>
        /// Number of cycles before long break (2 - 10).
        /// If you ignore long breaks, leave null.
        /// </summary>
        public int? CyclesBeforeLongBreak { get; set; }
        /// <summary>
        /// The duration of the long break phase in minutes (5.00f - 30.00f).
        /// May be null.
        /// </summary>
        public float? LongBreakDuration { get; set; }
    }
}
