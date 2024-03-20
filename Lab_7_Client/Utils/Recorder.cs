using NAudio.SoundFont;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab_7_Client.Utils
{
    internal class Recorder
    {
        private const string _ffmpegPath = @"D:\Program Files\ffmpeg\bin\ffmpeg.exe";
        private const string args = "-f dshow -i audio=\"Stereo Mix (Realtek High Definition Audio)\" -f gdigrab -framerate 30 -draw_mouse 0 -i desktop -c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p -c:a aac";

        private readonly static string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        private readonly static string _tempName;

        private static Process _ffmpegProcess;

        static Recorder()
        {
            _tempName = $"{DateTime.Now.Ticks}.mp4";

            _ffmpegProcess = new Process();
            _ffmpegProcess.StartInfo.FileName = _ffmpegPath;
            _ffmpegProcess.StartInfo.Arguments = $"{args} {_outputPath}\\{_tempName}";
            _ffmpegProcess.StartInfo.UseShellExecute = false;
            _ffmpegProcess.StartInfo.RedirectStandardInput = true;
            _ffmpegProcess.StartInfo.CreateNoWindow = true;
        }

        public static void StartRecording()
        {
            _ffmpegProcess.Start();
        }

        public static void StopRecording()
        {
            using (var writer = _ffmpegProcess.StandardInput)
                if (writer.BaseStream.CanWrite)
                    writer.WriteLine("q");

            _ffmpegProcess.WaitForExit();

            var source = $"{_outputPath}\\{_tempName}";

            if (File.Exists(source))
            {
                var fi = new FileInfo(source);

                var newName = $"Record_{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.mp4";
                var dest = $"{_outputPath}\\{newName}";

                fi.MoveTo(dest);
            }
        }
    }
}
