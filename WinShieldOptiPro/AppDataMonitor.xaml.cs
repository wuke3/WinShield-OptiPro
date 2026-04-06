using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using Microsoft.Win32;

// 使用别名避免Path命名空间冲突
using IO = System.IO;

namespace WinShieldOptiPro;

/// <summary>
/// Interaction logic for AppDataMonitor.xaml
/// </summary>
public partial class AppDataMonitor : UserControl
{
    // 监控相关变量
    private bool isMonitoring = false;
    private Thread monitorThread;
    private CancellationTokenSource cts;
    private List<MonitorLogItem> monitorLogs = new List<MonitorLogItem>();
    private Dictionary<string, SoftwareStats> softwareStats = new Dictionary<string, SoftwareStats>();
    private List<string> whitelist = new List<string>();
    
    // 开机自启相关
    private const string RUN_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string APP_NAME = "WinShieldOptiPro";
    
    // ReadDirectoryChangesW API相关
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadDirectoryChangesW(
        IntPtr hDirectory,
        IntPtr lpBuffer,
        uint nBufferLength,
        bool bWatchSubtree,
        uint dwNotifyFilter,
        out uint lpBytesReturned,
        IntPtr lpOverlapped,
        IntPtr lpCompletionRoutine
    );
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    
    // 常量定义
    private const uint FILE_LIST_DIRECTORY = 0x0001;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_NOTIFY_CHANGE_FILE_NAME = 0x00000001;
    private const uint FILE_NOTIFY_CHANGE_DIR_NAME = 0x00000002;
    private const uint FILE_NOTIFY_CHANGE_ATTRIBUTES = 0x00000004;
    private const uint FILE_NOTIFY_CHANGE_SIZE = 0x00000008;
    private const uint FILE_NOTIFY_CHANGE_LAST_WRITE = 0x00000010;
    private const uint FILE_NOTIFY_CHANGE_LAST_ACCESS = 0x00000020;
    private const uint FILE_NOTIFY_CHANGE_CREATION = 0x00000040;
    private const uint FILE_NOTIFY_CHANGE_SECURITY = 0x00000100;
    private const uint FILE_NOTIFY_CHANGE_ALL = FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_DIR_NAME | 
                                               FILE_NOTIFY_CHANGE_ATTRIBUTES | FILE_NOTIFY_CHANGE_SIZE | 
                                               FILE_NOTIFY_CHANGE_LAST_WRITE | FILE_NOTIFY_CHANGE_LAST_ACCESS | 
                                               FILE_NOTIFY_CHANGE_CREATION | FILE_NOTIFY_CHANGE_SECURITY;
    
    // 结构体定义
    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_NOTIFY_INFORMATION
    {
        public uint NextEntryOffset;
        public uint Action;
        public uint FileNameLength;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;
    }
    
    // 监控日志项
    public class MonitorLogItem
    {
        public string Time { get; set; }
        public string ProcessName { get; set; }
        public string Operation { get; set; }
        public string FilePath { get; set; }
        public string Size { get; set; }
    }
    
    // 软件统计信息
    public class SoftwareStats
    {
        public string SoftwareName { get; set; }
        public int WriteCount { get; set; }
        public string SpaceUsed { get; set; }
        public string LastWriteTime { get; set; }
        public long TotalSize { get; set; }
    }
    
    public AppDataMonitor()
    {
        InitializeComponent();
        Loaded += AppDataMonitor_Loaded;
        AlertThresholdSlider.ValueChanged += AlertThresholdSlider_ValueChanged;
    }
    
    private void AppDataMonitor_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载白名单
        LoadWhitelist();
        
        // 检查开机自启设置
        AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
        
        // 初始化日志数据
        InitializeLogs();
        
        // 初始化软件统计
        InitializeSoftwareStats();
    }
    
    private void StartMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        if (!isMonitoring)
        {
            StartMonitoring();
        }
    }
    
    private void StopMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        if (isMonitoring)
        {
            StopMonitoring();
        }
    }
    
    private void StartMonitoring()
    {
        try
        {
            cts = new CancellationTokenSource();
            monitorThread = new Thread(() => MonitorAppData(cts.Token));
            monitorThread.IsBackground = true;
            monitorThread.Start();
            
            isMonitoring = true;
            MonitorStatusText.Text = "监控中";
            MonitorStatusText.Foreground = new SolidColorBrush(Colors.Green);
            StartMonitorButton.IsEnabled = false;
            StopMonitorButton.IsEnabled = true;
            
            // 如果设置了后台常驻监控，最小化到托盘
            if (BackgroundMonitorCheckBox.IsChecked == true)
            {
                // 这里可以添加最小化到托盘的逻辑
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动监控失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void StopMonitoring()
    {
        try
        {
            cts.Cancel();
            monitorThread.Join(1000);
            
            isMonitoring = false;
            MonitorStatusText.Text = "未启动";
            MonitorStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 69, 0));
            StartMonitorButton.IsEnabled = true;
            StopMonitorButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止监控失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void MonitorAppData(CancellationToken token)
    {
        // 获取AppData目录路径
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localLowPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
        
        // 监控多个目录
        Task.Run(() => MonitorDirectory(appDataPath, token));
        Task.Run(() => MonitorDirectory(roamingPath, token));
        Task.Run(() => MonitorDirectory(localLowPath, token));
        
        // 保持线程运行
        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(1000);
        }
    }
    
    private void MonitorDirectory(string directoryPath, CancellationToken token)
    {
        if (!Directory.Exists(directoryPath))
            return;
        
        IntPtr directoryHandle = CreateFile(
            directoryPath,
            FILE_LIST_DIRECTORY,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero
        );
        
        if (directoryHandle == IntPtr.Zero)
        {
            return;
        }
        
        try
        {
            byte[] buffer = new byte[4096];
            IntPtr bufferPtr = Marshal.AllocHGlobal(buffer.Length);
            
            try
            {
                while (!token.IsCancellationRequested)
                {
                    uint bytesReturned;
                    bool success = ReadDirectoryChangesW(
                        directoryHandle,
                        bufferPtr,
                        (uint)buffer.Length,
                        true,
                        FILE_NOTIFY_CHANGE_ALL,
                        out bytesReturned,
                        IntPtr.Zero,
                        IntPtr.Zero
                    );
                    
                    if (success && bytesReturned > 0)
                    {
                        Marshal.Copy(bufferPtr, buffer, 0, (int)bytesReturned);
                        ProcessDirectoryChanges(buffer, (int)bytesReturned, directoryPath);
                    }
                    
                    Thread.Sleep(100); // 避免CPU占用过高
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
        finally
        {
            CloseHandle(directoryHandle);
        }
    }
    
    private void ProcessDirectoryChanges(byte[] buffer, int bytesReturned, string directoryPath)
    {
        int offset = 0;
        
        while (offset < bytesReturned)
        {
            FILE_NOTIFY_INFORMATION fni = Marshal.PtrToStructure<FILE_NOTIFY_INFORMATION>(Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset));
            
            string fileName = fni.FileName.Substring(0, (int)fni.FileNameLength / 2);
            string filePath = IO.Path.Combine(directoryPath, fileName);
            string operation = GetOperationString(fni.Action);
            
            // 获取进程信息
            string processName = GetProcessName();
            
            // 检查是否在白名单中
            if (!IsInWhitelist(processName))
            {
                // 创建日志项
                MonitorLogItem logItem = new MonitorLogItem
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProcessName = processName,
                    Operation = operation,
                    FilePath = filePath,
                    Size = GetFileSize(filePath)
                };
                
                // 更新UI
                Dispatcher.Invoke(() =>
                {
                    AddLogItem(logItem);
                    UpdateSoftwareStats(processName, filePath);
                    CheckAbnormalWrite(processName);
                });
            }
            
            if (fni.NextEntryOffset == 0)
                break;
            
            offset += (int)fni.NextEntryOffset;
        }
    }
    
    private string GetOperationString(uint action)
    {
        switch (action)
        {
            case 1: return "创建";
            case 2: return "删除";
            case 3: return "修改";
            case 4: return "重命名";
            default: return "未知";
        }
    }
    
    private string GetProcessName()
    {
        try
        {
            // 获取当前进程名称
            return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        }
        catch
        {
            return "未知进程";
        }
    }
    
    private string GetFileSize(string filePath)
    {
        try
        {
            if (IO.File.Exists(filePath))
            {
                IO.FileInfo fileInfo = new IO.FileInfo(filePath);
                long size = fileInfo.Length;
                
                if (size < 1024)
                    return $"{size} B";
                else if (size < 1024 * 1024)
                    return $"{Math.Round(size / 1024.0, 2)} KB";
                else if (size < 1024 * 1024 * 1024)
                    return $"{Math.Round(size / (1024.0 * 1024.0), 2)} MB";
                else
                    return $"{Math.Round(size / (1024.0 * 1024.0 * 1024.0), 2)} GB";
            }
        }
        catch { }
        
        return "0 B";
    }
    
    private void AddLogItem(MonitorLogItem logItem)
    {
        monitorLogs.Insert(0, logItem);
        
        // 只保留最近100条日志
        if (monitorLogs.Count > 100)
        {
            monitorLogs.RemoveAt(monitorLogs.Count - 1);
        }
        
        MonitorLogDataGrid.ItemsSource = null;
        MonitorLogDataGrid.ItemsSource = monitorLogs;
    }
    
    private void UpdateSoftwareStats(string processName, string filePath)
    {
        if (!softwareStats.ContainsKey(processName))
        {
            softwareStats[processName] = new SoftwareStats
            {
                SoftwareName = processName,
                WriteCount = 0,
                TotalSize = 0,
                LastWriteTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        
        SoftwareStats stats = softwareStats[processName];
        stats.WriteCount++;
        stats.LastWriteTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        // 更新占用空间
        try
        {
            if (IO.File.Exists(filePath))
            {
                IO.FileInfo fileInfo = new IO.FileInfo(filePath);
                stats.TotalSize += fileInfo.Length;
            }
        }
        catch { }
        
        // 更新空间显示
        stats.SpaceUsed = FormatSize(stats.TotalSize);
        
        // 更新UI
        SoftwareStatsDataGrid.ItemsSource = null;
        SoftwareStatsDataGrid.ItemsSource = softwareStats.Values;
    }
    
    private void CheckAbnormalWrite(string processName)
    {
        if (AlertCheckBox.IsChecked == true)
        {
            // 简单的异常写入检测逻辑
            // 实际项目中可以实现更复杂的检测算法
            if (softwareStats.TryGetValue(processName, out SoftwareStats stats))
            {
                double threshold = AlertThresholdSlider.Value;
                if (stats.TotalSize > threshold * 1024 * 1024) // MB
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"警告：进程 {processName} 在AppData目录写入了大量数据（{stats.SpaceUsed}），可能存在异常行为。", 
                            "异常写入提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }
        }
    }
    
    private string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{Math.Round(bytes / 1024.0, 2)} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0), 2)} MB";
        else
            return $"{Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
    }
    
    private void ExportLogButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|CSV文件 (*.csv)|*.csv",
            Title = "导出监控日志"
        };
        
        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                using (IO.StreamWriter writer = new IO.StreamWriter(saveFileDialog.FileName))
                {
                    // 写入表头
                    writer.WriteLine("时间,进程,操作,文件路径,大小");
                    
                    // 写入日志数据
                    foreach (var logItem in monitorLogs)
                    {
                        writer.WriteLine($"{logItem.Time},{logItem.ProcessName},{logItem.Operation},{logItem.FilePath},{logItem.Size}");
                    }
                }
                
                MessageBox.Show("日志导出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出日志失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("确定要清空监控日志吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            monitorLogs.Clear();
            MonitorLogDataGrid.ItemsSource = null;
        }
    }
    
    private void AddWhitelistButton_Click(object sender, RoutedEventArgs e)
    {
        Button button = sender as Button;
        if (button != null)
        {
            SoftwareStats stats = button.DataContext as SoftwareStats;
            if (stats != null)
            {
                if (!whitelist.Contains(stats.SoftwareName))
                {
                    whitelist.Add(stats.SoftwareName);
                    SaveWhitelist();
                    MessageBox.Show($"已将 {stats.SoftwareName} 添加到白名单", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"{stats.SoftwareName} 已经在白名单中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
    
    private void AlertThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AlertThresholdText.Text = $"{AlertThresholdSlider.Value} MB/分钟";
    }
    
    private bool IsAutoStartEnabled()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false))
            {
                return key.GetValue(APP_NAME) != null;
            }
        }
        catch
        {
            return false;
        }
    }
    
    private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                key.SetValue(APP_NAME, System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"设置开机自启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            AutoStartCheckBox.IsChecked = false;
        }
    }
    
    private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
            {
                key.DeleteValue(APP_NAME, false);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"取消开机自启失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            AutoStartCheckBox.IsChecked = true;
        }
    }
    
    private void LoadWhitelist()
    {
        try
        {
            string whitelistPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "whitelist.txt");
            if (IO.File.Exists(whitelistPath))
            {
                whitelist = new List<string>(IO.File.ReadAllLines(whitelistPath));
            }
        }
        catch { }
    }
    
    private void SaveWhitelist()
    {
        try
        {
            string whitelistPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro");
            IO.Directory.CreateDirectory(whitelistPath);
            IO.File.WriteAllLines(IO.Path.Combine(whitelistPath, "whitelist.txt"), whitelist);
        }
        catch { }
    }
    
    private bool IsInWhitelist(string processName)
    {
        return whitelist.Contains(processName);
    }
    
    private void InitializeLogs()
    {
        // 初始化日志数据
        monitorLogs = new List<MonitorLogItem>
        {
            new MonitorLogItem { Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ProcessName = "WinShieldOptiPro", Operation = "启动", FilePath = "AppData监控模块", Size = "0 B" }
        };
        MonitorLogDataGrid.ItemsSource = monitorLogs;
    }
    
    private void InitializeSoftwareStats()
    {
        // 初始化软件统计数据
        softwareStats = new Dictionary<string, SoftwareStats>
        {
            { "WinShieldOptiPro", new SoftwareStats { SoftwareName = "WinShieldOptiPro", WriteCount = 1, SpaceUsed = "0 B", LastWriteTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), TotalSize = 0 } }
        };
        SoftwareStatsDataGrid.ItemsSource = softwareStats.Values;
    }
}
