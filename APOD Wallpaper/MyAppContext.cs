using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebUtils;

namespace APOD_Wallpaper
{
    internal class MyAppContext : ApplicationContext
    {
        const string APOD_URL = "https://apod.nasa.gov/apod/";

        [DllImport("User32", CharSet = CharSet.Auto)]
        public static extern void SystemParametersInfo(int uiAction, int uiParam, string pvParam, uint fWinIni);
        
        private readonly NotifyIcon _trayIcon = new()
        {
            Icon = Properties.Resources.star,
            Visible = true
        };

        private readonly System.Threading.Timer _timer;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        
        public MyAppContext()
        {
            //Run on login
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.SetValue(Application.ProductName, Application.ExecutablePath);
           
            //Menu
            var cms = _trayIcon.ContextMenuStrip = new ContextMenuStrip();

            cms.Items.Add("APOD").Click += APOD_Menu_Clicked;
            cms.Items.Add("-");
            cms.Items.Add("Exit").Click += Exit_Menu_Click;

            _cancellationToken = _cancellationTokenSource.Token;
            _timer = new System.Threading.Timer(new TimerCallback(Timer_Ticked), null, 0, Timeout.Infinite);
        }

        private void APOD_Menu_Clicked(object sender, EventArgs e)
        {
            
        }

        private void Exit_Menu_Click(object sender, EventArgs e)
        {
            _timer.Dispose();
            _trayIcon.Dispose();
            _cancellationTokenSource.Cancel();
            Application.Exit();
        }

        private async void Timer_Ticked(object state)
        {
            try { await DoWork(); }
            catch { }

            //Try block just incase the timer is disposed
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _timer.Change(1000 * 60 * 60, Timeout.Infinite);
            }
            catch { }
        }


        private async Task DoWork()
        {
            string trackingFile = Path.Combine(Path.GetTempPath(),  "current_apod_image.txt");

            string lastUrl = null;
            try { lastUrl = File.ReadAllText(trackingFile); }
            catch { }

            try
            {
                string html = await SimpleDownloader.DownloadStringAsync(APOD_URL, _cancellationToken).ConfigureAwait(false);
                string url = html.Substring(html.IndexOf("<a href=\"image/") + 9);
                url = url.Substring(0, url.IndexOf("\""));
                url = APOD_URL + url;

                if (url != lastUrl)
                {
                    string ext = (Path.GetExtension(url) + string.Empty).ToLower();
                    if (new string[] { ".jpg", ".png", ".bmp" }.Contains(ext))
                    {
                        string imgeFile = Path.Combine(Path.GetTempPath(), "apod_image" + ext);
                        if (File.Exists(imgeFile))
                            File.Delete(imgeFile);

                        await SimpleDownloader.DownloadFileAsync(url, imgeFile, _cancellationToken).ConfigureAwait(false);

                        SystemParametersInfo(0x0014, 0, imgeFile, 0x0001);
                    }

                    File.WriteAllText(trackingFile, url);
                }
            }
            catch { }

        }
    }
}
