namespace ConsolePomodoro.Core.Contracts
{
    /// <summary>
    /// Provides audio playback functionality for the Pomodoro timer,
    /// including end bell, ticking, music, and volume control.
    /// </summary>
    internal interface IAudioService
    {
        /// <summary>
        /// Plays the bell sound indicating the end of a work or break phase.
        /// </summary>
        void PlayEndBell();

        /// <summary>
        /// Plays the ticking sound during the active phase
        /// (e.g., while counting down).
        /// </summary>
        void PlayTick();

        /// <summary>
        /// Starts or resumes background music playback.
        /// </summary>
        void PlayMusic();

        /// <summary>
        /// Prepares the list of available music file names
        /// from the configured directory.
        /// </summary>
        /// <remarks>
        /// This method should be called once during initialization
        /// to scan the music directory and shuffle the file list.
        /// </remarks>
        void FileNamesPrepare();

        /// <summary>
        /// Decreases the current audio volume by a specified step.
        /// </summary>
        /// <param name="step">
        /// The amount to decrease the volume by.
        /// Typically a value between 0.0 and 1.0, but can be any positive float.
        /// Default is 0.01 (1%).
        /// </param>
        /// <remarks>
        /// The volume will not go below 0.0 (silence).
        /// If the current volume minus <paramref name="step"/> is less than 0.0,
        /// the volume is set to 0.0.
        /// </remarks>
        void VolumeDecrease(float step = 0.01f);

        /// <summary>
        /// Increases the current audio volume by a specified step.
        /// </summary>
        /// <param name="step">
        /// The amount to increase the volume by.
        /// Typically a value between 0.0 and 1.0, but can be any positive float.
        /// Default is 0.01 (1%).
        /// </param>
        /// <remarks>
        /// The volume will not exceed 1.0 (maximum).
        /// If the current volume plus <paramref name="step"/> exceeds 1.0,
        /// the volume is set to 1.0.
        /// </remarks>
        void VolumeIncrease(float step = 0.01f);

        /// <summary>
        /// Smoothly mutes the audio by gradually decreasing the volume to zero
        /// over the specified duration.
        /// </summary>
        /// <param name="sender">
        /// The event source that triggered the mute operation.
        /// </param>
        /// <param name="e">
        /// Arguments containing the duration (in seconds) over which to fade out.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous fade-out operation.
        /// </returns>
        /// <remarks>
        /// This method is typically used to gracefully stop audio
        /// before a timer ends or when pausing. The implementation should
        /// perform a linear (or logarithmic) fade-out and then stop or pause playback.
        /// After the mute completes, the volume is restored to its original level
        /// if playback resumes (or remains zero depending on design).
        /// </remarks>
        Task SmoothMute(float duration);

        void OnPhaseStart(object? sender, EventArgs e);
        void OnPhaseEnd(object? sender, EventArgs e);
        void OnPhaseCountdown(object? sender, EventArgs e);
        Task OnPomodoroStart(object? sender, EventArgs e);

        public class SmoothMuteArgs(float duration) : EventArgs
        {
            public float Duration { get; } = duration;
        }
    }
}