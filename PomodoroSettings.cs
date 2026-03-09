namespace Pomodoro
{
    internal class PomodoroSettings
    {
        public int SetsCount { get; set; }
        public float WorkingPhaseMinutes { get; set; }      // вместо WorkingPhaseDuration
        public float RestingPhaseMinutes { get; set; }      // вместо RestingPhaseDuration
        public float? LongRestingPhaseMinutes { get; set; }  // вместо LongRestingPhaseDuration
        public int? SetsUntilLongResting { get; set; }
    }
}
