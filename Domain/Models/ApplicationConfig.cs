namespace ConsolePomodoro.Domain.Models
{
    /// <summary>
    /// Application configuration loading from JSON.
    /// </summary>
    internal class ApplicationConfig : IConfig
    {
        /// <summary>
        /// Background music volume (0.00f - 1.00f)
        /// </summary>
        public float DefaultMusicVolume { get; set; } = 0.3f;
        /// <summary>
        /// Ticking sound of timer volume (0.00f - 1.00f)
        /// </summary>
        public float DefaultTickingVolume { get; set; } = 1f;
        /// <summary>
        /// Phase end signal volume (0.00f - 1.00f)
        /// </summary>
        public float DefaultPhaseEndBellVolume { get; set; } = 1f;
        /// <summary>
        /// The path to the directory with timer sounds (ringing, ticking).
        /// </summary>
        public string TimerSoundDir { get; set; } = "./Assets/Audio/TimerSound";
        /// <summary>
        /// The path to the directory with the background music. May be null.
        /// </summary>
        public string? MusicDir { get; set; } = null;
    }
}
