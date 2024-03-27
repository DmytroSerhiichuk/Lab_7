using NAudio.Wave;
using System.IO;

namespace Lab_7_Client.Utils
{
    internal class AudioPlayer
    {
        private readonly WaveOutEvent _waveOut;
        private readonly MemoryStream _stream;

        public AudioPlayer()
        {
            _waveOut = new WaveOutEvent();
            _stream = new MemoryStream();
            _waveOut.Init(new RawSourceWaveStream(_stream, new WaveFormat(44100, 16, 1)));
        }

        public void Play(byte[] buffer)
        {
            _waveOut.Play();
            _stream.SetLength(0);
            _stream.Write(buffer, 0, buffer.Length);
            _stream.Position = 0;
        }

        public void Stop()
        {
            _waveOut.Stop();
        }

        public static AudioPlayer Instanse { get; set; }

        static AudioPlayer()
        {
            Instanse = new AudioPlayer();
        }
    }
}
