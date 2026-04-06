using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinShieldOptiPro
{
    public class PopupBlockerService
    {
        private static PopupBlockerService instance;
        private bool isRunning = false;
        private Thread serviceThread;
        private CancellationTokenSource cts;
        private List<string> blacklist = new List<string>();
        private List<string> whitelist = new List<string>();
        private string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "popup_logs.txt");
        private string blacklistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "blacklist.txt");
        private string whitelistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "whitelist.txt");

        // Windows API 相关
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool CloseWindow(IntPtr hWnd);

        public static PopupBlockerService Instance
        {
            get
            {
                if (instance == null)
                    instance = new PopupBlockerService();
                return instance;
            }
        }

        private PopupBlockerService()
        {
            InitializeDirectories();
            LoadLists();
        }

        private void InitializeDirectories()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro");
            if (!Directory.Exists(appDataPath))
                Directory.CreateDirectory(appDataPath);
        }

        private void LoadLists()
        {
            if (File.Exists(blacklistFilePath))
                blacklist = File.ReadAllLines(blacklistFilePath).ToList();
            if (File.Exists(whitelistFilePath))
                whitelist = File.ReadAllLines(whitelistFilePath).ToList();
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            cts = new CancellationTokenSource();

            serviceThread = new Thread(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    EnumWindows(new EnumWindowsProc(CheckWindow), IntPtr.Zero);
                    Thread.Sleep(1000);
                }
            });
            serviceThread.IsBackground = true;
            serviceThread.Start();
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            cts?.Cancel();
            serviceThread?.Join();
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

        private void AddLog(PopupLog log)
        {
            File.AppendAllText(logFilePath, $"{log.Time}|{log.Type}|{log.ProcessName}|{log.WindowTitle}|{log.Action}\n");
        }

        public void SetStartup(bool enable)
        {
            string startupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "WinShieldOptiPro.lnk");
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            if (enable)
            {
                if (!File.Exists(startupPath))
                {
                    CreateShortcut(startupPath, exePath, "WinShield OptiPro 弹窗拦截服务");
                }
            }
            else
            {
                if (File.Exists(startupPath))
                {
                    File.Delete(startupPath);
                }
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            var shortcut = shellType.GetMethod("CreateShortcut").Invoke(shell, new object[] { shortcutPath });
            shortcut.GetType().GetProperty("TargetPath").SetValue(shortcut, targetPath);
            shortcut.GetType().GetProperty("Description").SetValue(shortcut, description);
            shortcut.GetType().GetProperty("WindowStyle").SetValue(shortcut, 7); // 最小化运行
            shortcut.GetType().GetMethod("Save").Invoke(shortcut, null);
        }
    }
}
