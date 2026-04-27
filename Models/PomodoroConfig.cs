namespace SimplePomodoro.Models;

/// <summary>
/// Pomodoro timer settings loading from JSON.
/// </summary>
internal class PomodoroConfig
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
    /// If 0 long break will be ignored.
    /// </summary>
    public int CyclesBeforeLongBreak { get; set; }
    /// <summary>
    /// The duration of the long break phase in minutes (5.00f - 30.00f).
    /// If 0 long break will be ignored.
    /// </summary>
    public float LongBreakDuration { get; set; }
}