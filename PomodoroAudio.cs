using NAudio.Wave;

namespace Pomodoro
{
    internal class PomodoroAudio
    {
        private const string TickingSoundPath =
            "./TimerSound/tick_sound.mp3";
        private const string AlarmSoundPath =
            "./TimerSound/alarm_sound.mp3";

        private readonly AudioFileReader _tickingSound;
        private readonly AudioFileReader _alarmSound;

        private readonly WaveOutEvent _tickPlayer;
        private readonly WaveOutEvent _alarmPlayer;


        public PomodoroAudio()
        {
            _tickingSound = new(TickingSoundPath);
            _tickPlayer = new();
            _tickPlayer.Init(_tickingSound);

            _alarmSound = new(AlarmSoundPath);
            _alarmPlayer = new();
            _alarmPlayer.Init(_alarmSound);
        }


        public void CountdownPlay(object? sender, EventArgs e)
        {
            _tickingSound.Position = 0;
            _tickPlayer.Play();
        }
        
        public void AlarmPlay(object? sender, EventArgs e)
        {
            _tickPlayer.Stop();
            _alarmSound.Position = 0;
            _alarmPlayer.Play();
        }
    }
}
