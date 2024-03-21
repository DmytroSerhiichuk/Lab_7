using System.IO;
using System.Net;
using System.Windows.Media.Imaging;

namespace Lab_7_Client
{
    public class MeetingParticipant
    {
        public IPEndPoint IpEndPoint { get; private set; }
        public string Name { get; private set; }
        public bool IsUsingCamera { get; private set; } = false;
        public bool IsUsingAudio { get; private set; } = false;

        public List<(byte[], int)> Frame { get; private set; } = new List<(byte[], int)>();
        private byte[] _frameBuffer;

        public List<(byte[], int)> ShareFrame { get; private set; } = new List<(byte[], int)>();
        private byte[] _shareFrameBuffer;

        public MeetingParticipant(string name, IPEndPoint endPoint)
        {
            IpEndPoint = endPoint;
            Name = name;
        }

        public void UpdateCamera(bool value)
        {
            IsUsingCamera = value;
        }
        public void UpdateAudio(bool value)
        {
            IsUsingAudio = value;
        }

        public void CreateFrame(int length)
        {
            Frame = new List<(byte[], int)>();
            _frameBuffer = new byte[length];
        }
        public void AddFrameData(byte[] array, int index)
        {
            Frame.Add(new(array, index));
        }
        public BitmapImage GetFrame()
        {
            for (var i = 0; i < Frame.Count; i++)
            {
                Buffer.BlockCopy(Frame[i].Item1, 0, _frameBuffer, Frame[i].Item2 * 32768, Frame[i].Item1.Length);
            }

            using (MemoryStream memoryStream = new MemoryStream(_frameBuffer))
            {

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public void CreateShareFrame(int length)
        {
            ShareFrame = new List<(byte[], int)>();
            _shareFrameBuffer = new byte[length];
        }
        public void AddShareFrameData(byte[] array, int index)
        {
            ShareFrame.Add(new(array, index));
        }
        public BitmapImage GetShareFrame()
        {
            for (var i = 0; i < ShareFrame.Count; i++)
            {
                Buffer.BlockCopy(ShareFrame[i].Item1, 0, _shareFrameBuffer, ShareFrame[i].Item2 * 32768, ShareFrame[i].Item1.Length);
            }

            using (MemoryStream memoryStream = new MemoryStream(_shareFrameBuffer))
            {

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();

                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
    }
}
