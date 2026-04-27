using NAudio.Wave;

namespace SimplePomodoro.Infrastructure;

/// <summary>
///     Controls all audio playback: ticking sound, end‑bell alarm, and background music.
///     Manages volume, play/pause, next/previous track, and smooth fading.
/// </summary>
internal class AudioControl
{
    // --- Static fields ---
    /// <summary>Supported audio file extensions for background music.</summary>
    private static readonly string[] Extensions = [".mp3", ".wav", ".ogg"];

    // --- Readonly fields (injected or constant paths) ---
    /// <summary>Path to the end‑bell sound file.</summary>
    private readonly string _endBellSoundPath;
    /// <summary>Path to the ticking sound file.</summary>
    private readonly string _tickingSoundPath;
    /// <summary>Directory path containing background music files.</summary>
    private readonly string _musicDirPath;

    /// <summary>NAudio reader for the end‑bell sound.</summary>
    private readonly AudioFileReader _endBellSound;
    /// <summary>NAudio reader for the ticking sound.</summary>
    private readonly AudioFileReader _tickingSound;

    /// <summary>NAudio player for the end‑bell sound.</summary>
    private readonly WaveOutEvent _endBellPlayer;
    /// <summary>NAudio player for the ticking sound.</summary>
    private readonly WaveOutEvent _tickPlayer;

    // --- Mutable state fields ---
    /// <summary>List of full paths to discovered music files.</summary>
    private readonly List<string> _musicFileNames = [];
    /// <summary>Current volume level for background music (0..1).</summary>
    private float _musicVolume;

    /// <summary>Currently active music player (changes when track advances).</summary>
    private WaveOutEvent? _currentPlayer;
    /// <summary>Audio reader for the currently playing track.</summary>
    private AudioFileReader? _currentReader;

    /// <summary>Index of the next track to play.</summary>
    private int _nextIndex = 0;
    /// <summary>Indicates whether playback is globally paused.</summary>
    private bool _isPaused = false;

    // --- Constructor ---
    /// <summary>
    ///     Initializes a new instance of the <see cref="AudioControl"/> class.
    /// </summary>
    /// <param name="tickingSoundPath">Path to the ticking sound file.</param>
    /// <param name="alarmSoundPath">Path to the end‑bell sound file.</param>
    /// <param name="musicDirPath">Directory containing background music.</param>
    /// <param name="endBellVolume">Volume for the end‑bell (0..1).</param>
    /// <param name="tickingVolume">Volume for the ticking sound (0..1).</param>
    /// <param name="musicVolume">Initial volume for background music (0..1).</param>
    public AudioControl(
        string tickingSoundPath,
        string alarmSoundPath,
        string musicDirPath,
        float endBellVolume,
        float tickingVolume,
        float musicVolume)
    {
        _musicVolume = musicVolume;

        _endBellSoundPath = alarmSoundPath;
        _tickingSoundPath = tickingSoundPath;
        _musicDirPath = musicDirPath;

        _tickingSound = new AudioFileReader(_tickingSoundPath);
        _tickPlayer = new WaveOutEvent();
        _tickPlayer.Init(_tickingSound);
        _tickPlayer.Volume = tickingVolume;

        _endBellSound = new AudioFileReader(_endBellSoundPath);
        _endBellPlayer = new WaveOutEvent();
        _endBellPlayer.Init(_endBellSound);
        _endBellPlayer.Volume = endBellVolume;
    }

    // --- Initialization ---
    /// <summary>
    ///     Scans the music directory for supported audio files and shuffles the playlist.
    ///     Must be called before any music playback.
    /// </summary>
    public void Init()
    {
        DirectoryInfo directoryInfo = new(_musicDirPath);

        foreach (FileInfo file in directoryInfo.EnumerateFiles())
        {
            if (Extensions.Contains(Path.GetExtension(file.FullName)))
            {
                _musicFileNames.Add(file.FullName);
            }
        }

        if (_musicFileNames.Count > 0)
        {
            _ = _musicFileNames.Shuffle();
        }
    }

    // --- Public control methods ---
    /// <summary>Plays the end‑bell sound once, stopping any ongoing ticking sound.</summary>
    public void PlayEndBell()
    {
        _tickPlayer.Stop();
        _endBellSound.Position = 0;
        _endBellPlayer.Play();
    }

    /// <summary>Plays the next track in the shuffled playlist. If the end is reached, loops from the start.</summary>
    public async Task PlayMusic()
    {
        if (_currentPlayer != null)
        {
            _currentPlayer.Stop();
            _currentPlayer.Dispose();
            _currentReader?.Dispose();
        }

        if (_nextIndex >= _musicFileNames.Count)
            _nextIndex = 0;

        if (_nextIndex < 0)
            _nextIndex = _musicFileNames.Count - 1;

        _currentReader = new AudioFileReader(_musicFileNames[_nextIndex]);
        _currentPlayer = new WaveOutEvent
        {
            Volume = _musicVolume
        };

        _currentPlayer.PlaybackStopped += OnPlaybackStopped;
        _currentPlayer.Init(_currentReader);
        await Task.Delay(10, CancellationToken.None);
        _currentPlayer.Play();

        ++_nextIndex;
    }

    /// <summary>Plays the ticking sound from the beginning (looped by NAudio).</summary>
    public void PlayTick()
    {
        _tickingSound.Position = 0;
        _tickPlayer.Play();
    }

    /// <summary>Smoothly fades out the current music over the specified duration.</summary>
    /// <param name="duration">Fade‑out duration in milliseconds.</param>
    public async Task SmoothMute(float duration)
    {
        if (_currentPlayer is null)
            return;

        const int steps = 100;
        float timeStep = duration / steps;
        float volumeStep = _currentPlayer.Volume / steps;

        for (int i = 0; i < steps; ++i)
        {
            _currentPlayer.Volume -= volumeStep;
            await Task.Delay((int)timeStep, CancellationToken.None);
        }

        _currentPlayer.Stop();
        _currentPlayer.Dispose();
        _currentReader?.Dispose();
    }

    // --- Event handlers (public) ---
    /// <summary>Increases background music volume by a small step.</summary>
    public void OnVolumeIncrease(object? sender, EventArgs e) => VolumeIncrease();
    /// <summary>Decreases background music volume by a small step.</summary>
    public void OnVolumeDecrease(object? sender, EventArgs e) => VolumeDecrease();

    /// <summary>Called when a Pomodoro phase ends – plays the end‑bell.</summary>
    public void OnPhaseEnd(object? sender, EventArgs e) => PlayEndBell();

    /// <summary>Called during the last seconds of a phase – starts ticking and fades out music.</summary>
    public void OnPhaseCountdown(object? sender, EventArgs e)
    {
        _ = Task.Run(() => SmoothMute(400));
        PlayTick();
    }

    /// <summary>Called when a new Pomodoro phase starts – resumes background music.</summary>
    public void OnPhaseStart(object? sender, EventArgs e) => _ = PlayMusic();

    /// <summary>Toggles pause/resume for all audio players.</summary>
    public void OnPause(object? sender, EventArgs e)
    {
        if (!_isPaused)
        {
            _isPaused = true;
            if (_tickPlayer.PlaybackState == PlaybackState.Playing) _tickPlayer.Pause();
            if (_endBellPlayer.PlaybackState == PlaybackState.Playing) _endBellPlayer.Pause();
            _currentPlayer?.Pause();
        }
        else
        {
            _isPaused = false;
            if (_tickPlayer.PlaybackState == PlaybackState.Paused) _tickPlayer.Play();
            if (_endBellPlayer.PlaybackState == PlaybackState.Paused) _endBellPlayer.Play();
            _currentPlayer?.Play();
        }
    }

    /// <summary>Skips to the next track in the playlist.</summary>
    public void NextTrack(object? sender, EventArgs e)
    {
        if (_musicFileNames.Count == 0) return;
        _ = PlayMusic();
    }

    /// <summary>Goes back to the previous track in the playlist.</summary>
    public void PreviousTrack(object? sender, EventArgs e)
    {
        if (_musicFileNames.Count == 0) return;
        if (_currentPlayer != null)
        {
            --_nextIndex;
            _ = PlayMusic();
        }
    }

    // --- Private helper methods ---
    /// <summary>Handles the natural end of a music track and automatically starts the next one.</summary>
    private void OnPlaybackStopped(object? sender, EventArgs e)
    {
        _currentPlayer?.PlaybackStopped -= OnPlaybackStopped;

        _currentReader?.Dispose();
        _currentReader = null;

        _currentPlayer?.Dispose();
        _currentPlayer = null;

        _ = PlayMusic();
    }

    /// <summary>Decreases music volume by an optional step (default 0.01).</summary>
    private void VolumeDecrease(float step = 0.01f)
    {
        if (_currentPlayer is null)
            return;

        _currentPlayer.Volume = (_currentPlayer.Volume - step) > 0.0f
            ? _currentPlayer.Volume - step
            : 0f;

        _musicVolume = _currentPlayer.Volume;
    }

    /// <summary>Increases music volume by an optional step (default 0.01), capping at 1.0.</summary>
    private void VolumeIncrease(float step = 0.01f)
    {
        if (_currentPlayer is null)
            return;

        _currentPlayer.Volume = (_currentPlayer.Volume + step) < 1.0f
            ? _currentPlayer.Volume + step
            : 1f;

        _musicVolume = _currentPlayer.Volume;
    }
}