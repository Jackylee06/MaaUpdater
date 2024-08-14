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
                    this.Close();
                }
            }
            if (!File.Exists(".\\MaaUpdater.json"))
            {
                LogInfo.Text += "请在上方文本框内填入或选择MAA更新器临时文件存储位置，默认为MAA根目录下MaaUpdater文件夹。\n";
                FileStream fileStream = File.Create(".\\MaaUpdater.json");
                fileStream.Close();
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
    }
    public class ConfigJson
    {
        public ConfigJson(string filePath, string currentVersion, string latestVersion)
        {
            this.filePath = filePath;
            this.currentVersion = currentVersion;
            this.latestVersion = latestVersion;
        }
        public string filePath;
        public string currentVersion;
        public string latestVersion;
    }
}