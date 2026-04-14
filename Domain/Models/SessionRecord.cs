namespace ConsolePomodoro.Domain.Models
{
    /// <summary>
    /// Model for session records.
    /// </summary>
    internal class SessionRecord
    {
        /// <summary>
        /// Recording time.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        /// <summary>
        /// Current pomodoro status. May be "Started", "Ended", "Interrupted"
        /// or etc.
        /// </summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>
        /// The amount of passed cycles.
        /// </summary>
        public int Cycles { get; set; } = 0;
    }
}
