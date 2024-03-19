using Lab_7_Client.Pages;

namespace Lab_7_Client.Utils
{
    internal class ProgramManager
    {
        public event Action<PageType> Navigated;
        public event Action<MeetingPage> ShareStarted;
        public event Action ShareFinished;

        public static ProgramManager Instance { get; private set; }

        static ProgramManager()
        {
            Instance = new ProgramManager();
        }

        public void Navigate(PageType pageType)
        {
            Navigated?.Invoke(pageType);
        }
        public void StartShare(MeetingPage page)
        {
            ShareStarted?.Invoke(page);
        }
        public void StopShare()
        {
            ShareFinished?.Invoke();
        }
    }

    enum PageType
    {
        MainPage = 0,
        MeetingPage = 1,
        ShareScreenPage = 2,
    }
}
