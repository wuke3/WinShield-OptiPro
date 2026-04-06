using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;

namespace WinShieldOptiPro
{
    public partial class PopupBlocker : UserControl
    {
        private bool isBlocking = false;
        private Thread blockingThread;
        private CancellationTokenSource cts;
        private List<string> blacklist = new List<string>();
        private List<string> whitelist = new List<string>();
        private List<PopupLog> logs = new List<PopupLog>();
        private string logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "popup_logs.txt");
        private string blacklistFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "blacklist.txt");
        private string whitelistFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "whitelist.txt");

        // Windows API 相关
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool CloseWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public PopupBlocker()
        {
            InitializeComponent();
            InitializeDirectories();
            LoadLists();
            LoadLogs();
            UpdateStatistics();
            // 默认关闭状态
            StatusText.Text = "(已停止)";
            StatusText.Foreground = Brushes.Red;
        }

        private void StartBlockingInternal()
        {
            if (isBlocking) return;

            isBlocking = true;
            cts = new CancellationTokenSource();
            StatusText.Text = "(拦截中)";
            StatusText.Foreground = Brushes.Green;

            blockingThread = new Thread(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    EnumWindows(new EnumWindowsProc(CheckWindow), IntPtr.Zero);
                    Thread.Sleep(1000);
                }
            });
            blockingThread.IsBackground = true;
            blockingThread.Start();
        }

        private void InitializeDirectories()
        {
            string appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro");
            if (!System.IO.Directory.Exists(appDataPath))
                System.IO.Directory.CreateDirectory(appDataPath);
        }

        private void LoadLists()
        {
            if (System.IO.File.Exists(blacklistFilePath))
                blacklist = System.IO.File.ReadAllLines(blacklistFilePath).ToList();
            if (System.IO.File.Exists(whitelistFilePath))
                whitelist = System.IO.File.ReadAllLines(whitelistFilePath).ToList();
        }

        private void SaveLists()
        {
            System.IO.File.WriteAllLines(blacklistFilePath, blacklist);
            System.IO.File.WriteAllLines(whitelistFilePath, whitelist);
        }

        private void LoadLogs()
        {
            if (System.IO.File.Exists(logFilePath))
            {
                var logLines = System.IO.File.ReadAllLines(logFilePath);
                foreach (var line in logLines)
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        logs.Add(new PopupLog
                        {
                            Time = parts[0],
                            Type = parts[1],
                            ProcessName = parts[2],
                            WindowTitle = parts[3],
                            Action = parts[4]
                        });
                    }
                }
                LogDataGrid.ItemsSource = logs.OrderByDescending(l => l.Time).ToList();
            }
        }

        private void AddLog(PopupLog log)
        {
            logs.Add(log);
            LogDataGrid.ItemsSource = logs.OrderByDescending(l => l.Time).ToList();
            System.IO.File.AppendAllText(logFilePath, $"{log.Time}|{log.Type}|{log.ProcessName}|{log.WindowTitle}|{log.Action}\n");
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            var today = DateTime.Now.Date;
            var weekAgo = today.AddDays(-7);
            TodayCount.Text = logs.Count(l => DateTime.Parse(l.Time).Date == today).ToString();
            WeekCount.Text = logs.Count(l => DateTime.Parse(l.Time) >= weekAgo).ToString();
            BlacklistCount.Text = blacklist.Count.ToString();
            WhitelistCount.Text = whitelist.Count.ToString();
        }

        private void StartBlocking(object sender, RoutedEventArgs e)
        {
            StartBlockingInternal();
        }

        private bool CheckWindow(IntPtr hWnd, IntPtr lParam)
        {
            if (!IsWindowVisible(hWnd)) return true;

            StringBuilder title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            string windowTitle = title.ToString();

            if (string.IsNullOrWhiteSpace(windowTitle)) return true;

            GetWindowThreadProcessId(hWnd, out uint processId);
            try
            {
                Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName;

                // 检查白名单
                if (whitelist.Contains(processName)) return true;

                // 检查黑名单
                if (blacklist.Contains(processName))
                {
                    CloseWindow(hWnd);
                    AddLog(new PopupLog
                    {
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = "黑名单拦截",
                        ProcessName = processName,
                        WindowTitle = windowTitle,
                        Action = "已关闭"
                    });
                    return true;
                }

                // 弹窗特征检测
                if (IsPopupWindow(processName, windowTitle))
                {
                    CloseWindow(hWnd);
                    AddLog(new PopupLog
                    {
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = "智能拦截",
                        ProcessName = processName,
                        WindowTitle = windowTitle,
                        Action = "已关闭"
                    });
                }
            }
            catch { }

            return true;
        }

        private bool IsPopupWindow(string processName, string windowTitle)
        {
            // 常见弹窗特征
            string[] popupKeywords = { "广告", "推广", "营销", "弹窗", "ad", "AD", "Advertisement", "Promotion" };
            string[] popupProcesses = { "360se", "360chrome", "qqpcmgr", "baiduprotect", "sogouexplorer", "maxthon", "ucbrowser" };

            // 检查进程名
            if (popupProcesses.Contains(processName))
                return true;

            // 检查窗口标题
            foreach (var keyword in popupKeywords)
            {
                if (windowTitle.Contains(keyword))
                    return true;
            }

            return false;
        }

        private void AddToBlacklist(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(dialog.SelectedPath);
                if (!blacklist.Contains(processName))
                {
                    blacklist.Add(processName);
                    SaveLists();
                    BlacklistCount.Text = blacklist.Count.ToString();
                    MessageBox.Show($"已添加 {processName} 到黑名单");
                }
            }
        }

        private void AddToWhitelist(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(dialog.SelectedPath);
                if (!whitelist.Contains(processName))
                {
                    whitelist.Add(processName);
                    SaveLists();
                    WhitelistCount.Text = whitelist.Count.ToString();
                    MessageBox.Show($"已添加 {processName} 到白名单");
                }
            }
        }

        private void ViewLogs(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(logFilePath))
                Process.Start("notepad.exe", logFilePath);
            else
                MessageBox.Show("日志文件不存在");
        }

        private void ManageRules(object sender, RoutedEventArgs e)
        {
            RuleManager ruleManager = new RuleManager();
            ruleManager.ShowDialog();
            // 重新加载规则
            LoadLists();
        }

        private void LocateSoftware(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string processName = button.Tag as string;
            
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                string path = processes[0].MainModule.FileName;
                string directory = System.IO.Path.GetDirectoryName(path);
                
                var result = MessageBox.Show($"找到软件：{processName}\n路径：{path}\n\n是否打开所在目录？\n\n提示：如果该软件频繁弹出广告，建议卸载。", "软件定位", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);
                
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("explorer.exe", directory);
                }
                else if (result == MessageBoxResult.No)
                {
                    // 打开控制面板卸载界面
                    Process.Start("appwiz.cpl");
                }
            }
            else
            {
                MessageBox.Show("未找到该进程");
            }
        }

        private void StopBlockingInternal()
        {
            if (!isBlocking) return;

            isBlocking = false;
            cts?.Cancel();
            blockingThread?.Join();
            StatusText.Text = "(已停止)";
            StatusText.Foreground = Brushes.Red;
        }

        private void StopBlocking(object sender, RoutedEventArgs e)
        {
            StopBlockingInternal();
        }
    }

    public class PopupLog
    {
        public string Time { get; set; }
        public string Type { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public string Action { get; set; }
    }
}
