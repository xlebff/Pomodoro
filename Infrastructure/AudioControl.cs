using NAudio.Wave;

namespace SimplePomodoro.Infrastructure;

internal class AudioControl
{
    private readonly AudioFileReader _endBellSound;
    private readonly AudioFileReader _tickingSound;

    private readonly WaveOutEvent _endBellPlayer;
    private readonly WaveOutEvent _tickPlayer;

    private readonly string[] Extensions = [".mp3", ".wav", ".ogg"];

    private readonly string _endBellSoundPath;
    private readonly string _tickingSoundPath;
    private readonly string _musicDirPath;

    private readonly List<string> _musicFileNames = [];

    private float _musicVolume;

    private WaveOutEvent? _currentPlayer;
    private AudioFileReader? _currentReader;

    private int _nextIndex = 0;

    private bool _isPaused = false;

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

        _tickingSound = new(_tickingSoundPath);
        _tickPlayer = new();
        _tickPlayer.Init(_tickingSound);
        _tickPlayer.Volume = tickingVolume;

        _endBellSound = new(_endBellSoundPath);
        _endBellPlayer = new();
        _endBellPlayer.Init(_endBellSound);
        _endBellPlayer.Volume = endBellVolume;
    }

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

    public void PlayEndBell()
    {
        _tickPlayer.Stop();
        _endBellSound.Position = 0;
        _endBellPlayer.Play();
    }

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

        _currentReader = new(
            _musicFileNames[_nextIndex]);
        _currentPlayer = new()
        {
            Volume = _musicVolume
        };

        _currentPlayer.PlaybackStopped += OnPlaybackStopped;

        _currentPlayer.Init(_currentReader);
        await Task.Delay(10, CancellationToken.None);
        _currentPlayer.Play();

        ++_nextIndex;
    }

    private void OnPlaybackStopped(object? sender, EventArgs e)
    {
        _currentPlayer?.PlaybackStopped -= OnPlaybackStopped;

        _currentReader?.Dispose();
        _currentReader = null;

        _currentPlayer?.Dispose();
        _currentPlayer = null;

        PlayMusic();
    }

    public void PlayTick()
    {
        _tickingSound.Position = 0;
        _tickPlayer.Play();
    }

    public async Task SmoothMute(float duration)
    {
        if (_currentPlayer is null)
            return;

        int magicValue = 100;

        float timeStep = duration / magicValue;
        float volumeStep = _currentPlayer.Volume / magicValue;

        for (int i = 0; i < magicValue; ++i)
        {
            _currentPlayer.Volume -= volumeStep;
            await Task.Delay((int)timeStep, CancellationToken.None);
        }

        _currentPlayer.Stop();
        _currentPlayer.Dispose();
        _currentReader?.Dispose();
    }

    private void VolumeDecrease(float step = 0.01F)
    {
        if (_currentPlayer is null)
            return;

        if ((_currentPlayer.Volume - step) > 0.0f)
            _currentPlayer.Volume -= step;
        else _currentPlayer.Volume = 0f;

        _musicVolume = _currentPlayer.Volume;
    }

    private void VolumeIncrease(float step = 0.01F)
    {
        if (_currentPlayer is null)
            return;

        if ((_currentPlayer.Volume + step) < 1.0f)
            _currentPlayer.Volume += step;
        else _currentPlayer.Volume = 1f;

        _musicVolume = _currentPlayer.Volume;
    }

    public void OnVolumeIncrease(object? sender, EventArgs e)
    {
        VolumeIncrease();
    }

    public void OnVolumeDecrease(object? sender, EventArgs e)
    {
        VolumeDecrease();
    }

    public void OnPhaseEnd(object? sender, EventArgs e)
    {
        PlayEndBell();
    }

    public void OnPhaseCountdown(object? sender, EventArgs e)
    {
        _ = Task.Run(() => SmoothMute(400));
        PlayTick();
    }

    public void OnPhaseStart(object? sender, EventArgs e)
    {
        PlayMusic();
    }

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

    public void NextTrack(object? sender, EventArgs e)
    {
        if (_musicFileNames.Count == 0) return;

        _ = PlayMusic();
    }

    public void PreviousTrack(object? sender, EventArgs e)
    {
        if (_musicFileNames.Count == 0) return;

        if (_currentPlayer != null)
        {
            --_nextIndex;
            _ = PlayMusic();
        }
    }
}
