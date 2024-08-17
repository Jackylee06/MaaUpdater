using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;

namespace MaaUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!File.Exists(".\\MAA.exe"))
            {
                if (MessageBox.Show("请将MAA更新器移动到MAA根目录下后重新启动", "错误", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK) == MessageBoxResult.OK)
                {
                    Environment.Exit(0);
                }
            }
            if (!File.Exists(".\\MaaUpdater.json"))
            {
                LogInfo.Text += "请在上方文本框内填入或选择MAA更新器临时文件存储位置，留空则默认为MAA根目录下MaaUpdater文件夹。\n";
                LogInfo.ScrollToEnd();
                FileStream createJson = File.Create(".\\MaaUpdater.json");
                createJson.Close();
                ConfigJson configJson = new ConfigJson(MaaPath.Text, "");
                File.WriteAllText(".\\MaaUpdater.Json", JsonSerializer.Serialize(configJson, jsonOptions));
            }
            else
            {
                string jsonString = File.ReadAllText(".\\MaaUpdater.json");
                ConfigJson? readJson = JsonSerializer.Deserialize<ConfigJson>(jsonString);
                MaaPath.Text = readJson.filePath;
                currentCommit = readJson.currentVersion;
            }
        }
        JsonSerializerOptions jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        string? latestCommit = string.Empty;
        string currentCommit = string.Empty;
        async Task MainProcess()
        {
            try
            {
                MaaPath.Focusable = false;
                MaaPath.IsHitTestVisible = false;
                MaaPathChoose.Focusable = false;
                MaaPathChoose.IsHitTestVisible = false;
                if (String.IsNullOrEmpty(MaaPath.Text))
                {
                    Directory.CreateDirectory(".\\MaaUpdater");
                }
                else
                {
                    Directory.CreateDirectory(MaaPath.Text);
                }
                string filePath = (String.IsNullOrEmpty(MaaPath.Text) ? System.IO.Path.GetFullPath(".\\MaaUpdater\\") : MaaPath.Text) + "\\MaaResource-main.zip";
                using HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10D) };
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                httpClient.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
                await ProcessRepositoriesAsync(httpClient);
                if (!String.IsNullOrEmpty(latestCommit))
                {
                    if (String.IsNullOrEmpty(currentCommit))
                    {
                        if (await DownloadFileAsync("https://github.com/MaaAssistantArknights/MaaResource/archive/refs/heads/main.zip", filePath))
                        {
                        UpdateFile(filePath);
                        currentCommit = latestCommit;
                        }
                    }
                    else
                    {
                        if (currentCommit == latestCommit)
                        {
                            LogInfo.Text += "当前已是最新版本\n";
                            LogInfo.ScrollToEnd();
                        }
                        else
                        {
                            if (await DownloadFileAsync("https://github.com/MaaAssistantArknights/MaaResource/archive/refs/heads/main.zip", filePath))
                            {
                                UpdateFile(filePath);
                                currentCommit = latestCommit;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo.Text += ex.Message + "\n";
                LogInfo.ScrollToEnd();
            }
        }
        async Task ProcessRepositoriesAsync(HttpClient httpClient)
        {
            try
            {
                var resourceCommits = await httpClient.GetAsync("https://api.github.com/repos/MaaAssistantArknights/MaaResource/commits");
                if (!resourceCommits.IsSuccessStatusCode)
                {
                    LogInfo.Text += "当前无法连接到GitHub，请使用手机热点或在另一时间段重试。\n";
                    LogInfo.ScrollToEnd();
                    return;
                }
                CommitInfo[]? commitInfo = JsonSerializer.Deserialize<CommitInfo[]>(await resourceCommits.Content.ReadAsStringAsync());
                latestCommit = commitInfo[0].sha;
                LogInfo.Text += $"Latest Commit Hash: {latestCommit}\n";
                LogInfo.ScrollToEnd();
            }
            catch (Exception ex)
            {
                LogInfo.Text += ex.Message + "\n";
                LogInfo.ScrollToEnd();
            }
        }
        async Task<bool> DownloadFileAsync(string url, string localPath)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    LogInfo.Text += "开始下载。\n";
                    LogInfo.ScrollToEnd();
                    httpClient.Timeout = TimeSpan.FromSeconds(10D);
                    using (HttpResponseMessage responseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            LogInfo.Text += "当前无法连接到GitHub，请使用手机热点或在另一时间段重试。\n";
                            LogInfo.ScrollToEnd();
                        }
                        using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                        {
                            using (FileStream fileStream = new FileStream(localPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 8192, true))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                long totalBytesRead = 0;
                                long totalBytes = responseMessage.Content.Headers.ContentLength ?? -1;
                                string logChange = "";
                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;
                                    LogInfo.Text = LogInfo.Text.Substring(0, LogInfo.Text.Length - logChange.Length);
                                    LogInfo.ScrollToEnd();
                                    logChange = "下载进度：" + ((double)totalBytesRead / totalBytes * 100).ToString("f2") + "%";
                                    LogInfo.Text += logChange;
                                    LogInfo.ScrollToEnd();
                                }
                                LogInfo.Text += "\n下载完成。\n";
                                LogInfo.ScrollToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogInfo.Text += ex.Message + "\n";
                LogInfo.ScrollToEnd();
                return false;
            }
            return true;
        }
        public void UpdateFile(string localPath)
        {
            try
            {
                string cachePath = String.IsNullOrEmpty(MaaPath.Text) ? System.IO.Path.GetFullPath(".\\MaaUpdater") : MaaPath.Text;
                System.IO.Compression.ZipFile.ExtractToDirectory(localPath, cachePath, true);
                LogInfo.Text += "解压完成。\n";
                LogInfo.ScrollToEnd();
                Directory.Delete(".\\cache", true);
                Directory.Delete(".\\resource", true);
                Directory.Move(cachePath + "\\MaaResource-main\\cache", ".\\cache");
                Directory.Move(cachePath + "\\MaaResource-main\\resource", ".\\resource");
                LogInfo.Text += "覆盖完成。\n";
                LogInfo.ScrollToEnd();
                File.Delete(localPath);
                Directory.Delete(cachePath + "\\MaaResource-main", true);
                LogInfo.Text += "已删除临时文件。\n";
                LogInfo.ScrollToEnd();
                ConfigJson updateJson = new ConfigJson(MaaPath.Text, currentCommit);
                File.WriteAllText(".\\MaaUpdater.Json", JsonSerializer.Serialize(updateJson, jsonOptions));
                LogInfo.Text += "配置文件已更新。\n";
                LogInfo.ScrollToEnd();
                LogInfo.Text += "更新完成，正在启动MAA。\n";
                LogInfo.ScrollToEnd();
                System.Diagnostics.Process.Start(".\\MAA.exe");
                //Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogInfo.Text += ex.Message + "\n";
                LogInfo.ScrollToEnd();
            }
        }
        private void MaaPathChoose_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                InitialDirectory = Environment.CurrentDirectory,
                Multiselect = false
            };
            if (openFolderDialog.ShowDialog() == true)
            {
                MaaPath.Text = openFolderDialog.FolderName;
            }
        }
        private async void StartUpdate_Click(object sender, RoutedEventArgs e)
        {
            StartUpdate.Visibility = Visibility.Collapsed;
            await MainProcess();
        }
    }
    public class ConfigJson
    {
        public ConfigJson(string filePath, string currentVersion)
        {
            this.filePath = filePath;
            this.currentVersion = currentVersion;
        }
        public string filePath { get; set; }
        public string currentVersion { get; set; }
    }
    public class CommitInfo
    {
        public string? sha { get; set; }
    }
}