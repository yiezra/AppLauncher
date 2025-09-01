using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AppLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private string versionFileName;
        private string gameZipName;
        private string gameExeName;
        private string downloadUrl;
        private string appFolder;

        private long gameZipTotalSize;
        private long gameZipReceivedTotal;

        private readonly DispatcherTimer timer =
        new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        PlayStatus.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "업데이트 실패 - 재시도";
                        PlayStatus.Content = "업데이트 실패 - 재시도";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "다운로드 중";
                        PlayStatus.Content = "다운로드 중";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "업데이트 다운로드 중";
                        PlayStatus.Content = "업데이트 다운로드 중";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            versionFileName = ConfigurationManager.AppSettings["versionFile"].ToString();
            gameZipName = ConfigurationManager.AppSettings["gameZip"].ToString();
            gameExeName = ConfigurationManager.AppSettings["gameExe"].ToString();
            downloadUrl = ConfigurationManager.AppSettings["downloadUrl"].ToString();
            appFolder = Path.GetFileNameWithoutExtension(gameZipName);
            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, versionFileName);
            gameZip = Path.Combine(rootPath, gameZipName);
            gameExe = Path.Combine(rootPath, appFolder, gameExeName);


            pbStatus.Visibility = Visibility.Hidden;
            PlayButton.Visibility = Visibility.Hidden;

            KillProcess();


            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void KillProcess()
        {
            string result = gameExeName.Replace(".exe", "");
            Process[] processes = Process.GetProcessesByName(result);

            if (processes.Length != 0)
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();  // 프로세스 종료

                        Thread.Sleep(2000);
                
                        //Console.WriteLine($"프로세스 {process.ProcessName} (PID: {process.Id})가 종료되었습니다.");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"프로세스를 종료할 권한이 없습니다. (PID: {process.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"예기치 않은 오류 발생: {ex.Message}");
                    }
                }
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            PlayStatus.Visibility = PlayStatus.Visibility == Visibility.Visible
                ? Visibility.Hidden : Visibility.Visible;
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    //HttpClient webClient = new HttpClient();
                    WebClient webClient = new WebClient();
                    string uri = downloadUrl + versionFileName;
                    Version onlineVersion = new Version(webClient.DownloadString(uri));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                        Play();
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                Read_GamezipFileSize();

                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString(downloadUrl+versionFileName));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadGameProgressChangedCallback);
                webClient.DownloadFileAsync(new Uri(downloadUrl+gameZipName), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void Read_GamezipFileSize()
        {
            System.Net.FtpWebRequest request = (FtpWebRequest)System.Net.FtpWebRequest.Create(new Uri(downloadUrl + gameZipName));
            request.Proxy = null;
            request.Method = WebRequestMethods.Ftp.GetFileSize;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            long size = response.ContentLength;

            gameZipTotalSize = size;

            pbStatus.Minimum = 0;
            pbStatus.Maximum = size;
        }

        private void DownloadGameProgressChangedCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            if (gameZipTotalSize > 0)
            {
                gameZipReceivedTotal += e.BytesReceived;
                pbStatus.Value = e.BytesReceived;
            }

            //Random r = new Random();
            //Brush brush = new SolidColorBrush(Color.FromRgb((byte)r.Next(1, 255),
            //                  (byte)r.Next(1, 255), (byte)r.Next(1, 233)));
            //PlayButton.BorderBrush = brush;
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                PlayButton.Content = "설치중";
                PlayStatus.Content = "설치중";
                ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateLayout();

                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
                Play();
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                return; // 자동실행
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, appFolder);
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }

        private void Play()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
            startInfo.WorkingDirectory = Path.Combine(rootPath, appFolder);
            Process.Start(startInfo);

            Close();
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
