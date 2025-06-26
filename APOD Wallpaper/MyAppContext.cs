using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APOD_Wallpaper;

internal class MyAppContext : ApplicationContext
{
    const string APOD_URL = "https://apod.nasa.gov/apod/";

    private readonly NotifyIcon _trayIcon = new()
    {
        Icon = Properties.Resources.star,
        Visible = true
    };

    private static readonly string[] _validImageExtensions = new string[] { ".jpg", ".png", ".bmp" };

    private readonly System.Threading.Timer _timer;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly HttpClient _httpClient = new();
    
    public MyAppContext()
    {
        _cancellationToken = _cancellationTokenSource.Token;

        //Run on login
        using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key.SetValue(Application.ProductName, Application.ExecutablePath);


        //Menu
        var cms = _trayIcon.ContextMenuStrip = new ContextMenuStrip();

        cms.Items.Add("APOD").Click += APOD_Menu_Clicked;
        cms.Items.Add("-");
        cms.Items.Add("Exit").Click += Exit_Menu_Click;

        _timer = new System.Threading.Timer(new TimerCallback(Timer_Ticked), null, 0, Timeout.Infinite);
    }

    private void APOD_Menu_Clicked(object sender, EventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(APOD_URL) { UseShellExecute = true });   
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
        using var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\DustyPig.tv\\APOD Wallpaper", true);

        string lastUrl = null;
        try { lastUrl = key.GetValue("LastUrl", string.Empty) as string; }
        catch { }

        string apiUrl = "https://api.nasa.gov/planetary/apod?api_key=dT4APYOJBWpbHdA5GkvmglXwhvMgaSGXdnJK1cyg";

        string currentUrl = null;
        var apodResponse = await _httpClient.GetFromJsonAsync<APODResponse>(apiUrl, _cancellationToken).ConfigureAwait(false);
        currentUrl = apodResponse.Hdurl;
        currentUrl ??= apodResponse.Url;
        if (string.IsNullOrWhiteSpace(currentUrl))
            return;
        if (currentUrl == lastUrl)
            return;


        string ext = (Path.GetExtension(currentUrl) + string.Empty).ToLower();
        if (!_validImageExtensions.Contains(ext))
            return;
        string imageFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DustyPig.tv", "APOD Wallpaper");
        Directory.CreateDirectory(imageFile);
        imageFile = Path.Combine(imageFile, "apod_image" + ext);
        

        using var httpResponse = await _httpClient.GetAsync(currentUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
        using var content = await httpResponse.Content.ReadAsStreamAsync(_cancellationToken).ConfigureAwait(false);

        using var fileStream = new FileStream(imageFile, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        await content.CopyToAsync(fileStream).ConfigureAwait(false);

        key.SetValue("LastUrl", currentUrl);
        var wallpaper = (IDesktopWallpaper)(new DesktopWallpaperClass());
        wallpaper.SetWallpaper(null, imageFile);
        wallpaper.Enable();
    }
}
