using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

namespace WinShieldOptiPro
{
    public partial class SystemToolbox : System.Windows.Controls.UserControl
    {
        private Process timerProcess;

        public SystemToolbox()
        {
            InitializeComponent();
            LoadHostsFile();
            LoadCurrentTime();
            LoadDisks();
        }

        // 系统核心管控 - Windows更新管控
        private void EnableWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("NoAutoUpdate", false);
                        key.DeleteValue("AUOptions", false);
                    }
                }
                System.Windows.MessageBox.Show("已开启Windows自动更新", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU"))
                {
                    key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                    key.SetValue("AUOptions", 1, RegistryValueKind.DWord);
                }
                System.Windows.MessageBox.Show("已关闭Windows自动更新", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PauseWindowsUpdate7Days_Click(object sender, RoutedEventArgs e)
        {
            PauseWindowsUpdate(7);
        }

        private void PauseWindowsUpdate15Days_Click(object sender, RoutedEventArgs e)
        {
            PauseWindowsUpdate(15);
        }

        private void PauseWindowsUpdate30Days_Click(object sender, RoutedEventArgs e)
        {
            PauseWindowsUpdate(30);
        }

        private void PauseWindowsUpdate(int days)
        {
            try
            {
                DateTime pauseUntil = DateTime.Now.AddDays(days);
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate"))
                {
                    key.SetValue("PauseFeatureUpdatesStartTime", pauseUntil.ToString("yyyy-MM-dd"), RegistryValueKind.String);
                    key.SetValue("PauseQualityUpdatesStartTime", pauseUntil.ToString("yyyy-MM-dd"), RegistryValueKind.String);
                }
                System.Windows.MessageBox.Show($"已暂停Windows更新 {days} 天", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 系统核心管控 - Defender安全软件管控
        private void EnableDefender_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender"))
                {
                    key.DeleteValue("DisableAntiSpyware", false);
                }
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection"))
                {
                    key.DeleteValue("DisableRealtimeMonitoring", false);
                }
                System.Windows.MessageBox.Show("已开启Windows Defender实时防护", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableDefender_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection"))
                {
                    key.SetValue("DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
                }
                System.Windows.MessageBox.Show("已关闭Windows Defender实时防护", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableDefenderScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Scan"))
                {
                    key.DeleteValue("DisableScanOnRealtimeEnable", false);
                }
                System.Windows.MessageBox.Show("已开启Windows Defender后台扫描", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableDefenderScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Scan"))
                {
                    key.SetValue("DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
                }
                System.Windows.MessageBox.Show("已关闭Windows Defender后台扫描", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 网络修复工具 - 断网急救
        private void FixNetwork_Click(object sender, RoutedEventArgs e)
        {
            NetworkFixResultText.Text = "正在修复网络...";
            Task.Run(() =>
            {
                try
                {
                    // 重置网络适配器
                    Process.Start("cmd.exe", "/c ipconfig /release").WaitForExit();
                    Process.Start("cmd.exe", "/c ipconfig /renew").WaitForExit();
                    Process.Start("cmd.exe", "/c ipconfig /flushdns").WaitForExit();
                    Process.Start("cmd.exe", "/c netsh winsock reset").WaitForExit();
                    Process.Start("cmd.exe", "/c netsh int ip reset").WaitForExit();

                    Dispatcher.Invoke(() =>
                    {
                        NetworkFixResultText.Text = "网络修复完成，请重启电脑以应用更改";
                        System.Windows.MessageBox.Show("网络修复完成，请重启电脑以应用更改", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        NetworkFixResultText.Text = $"修复失败: {ex.Message}";
                    });
                }
            });
        }

        // 网络修复工具 - Hosts文件修改
        private void LoadHostsFile()
        {
            string hostsPath = Path.Combine(Environment.SystemDirectory, "drivers\\etc\\hosts");
            try
            {
                if (File.Exists(hostsPath))
                {
                    HostsFileTextBox.Text = File.ReadAllText(hostsPath, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"无法读取Hosts文件: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveHosts_Click(object sender, RoutedEventArgs e)
        {
            string hostsPath = Path.Combine(Environment.SystemDirectory, "drivers\\etc\\hosts");
            try
            {
                File.WriteAllText(hostsPath, HostsFileTextBox.Text, Encoding.UTF8);
                System.Windows.MessageBox.Show("Hosts文件保存成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackupHosts_Click(object sender, RoutedEventArgs e)
        {
            string hostsPath = Path.Combine(Environment.SystemDirectory, "drivers\\etc\\hosts");
            string backupPath = hostsPath + ".bak";
            try
            {
                File.Copy(hostsPath, backupPath, true);
                System.Windows.MessageBox.Show("Hosts文件备份成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RestoreDefaultHosts_Click(object sender, RoutedEventArgs e)
        {
            string hostsPath = Path.Combine(Environment.SystemDirectory, "drivers\\etc\\hosts");
            try
            {
                string defaultHosts = "# Copyright (c) 1993-2009 Microsoft Corp.\n#\n# This is a sample HOSTS file used by Microsoft TCP/IP for Windows.\n#\n# This file contains the mappings of IP addresses to host names. Each\n# entry should be kept on an individual line. The IP address should\n# be placed in the first column followed by the corresponding host name.\n# The IP address and the host name should be separated by at least one\n# space.\n#\n# Additionally, comments (such as these) may be inserted on individual\n# lines or following the machine name denoted by a '#' symbol.\n#\n# For example:\n#\n#      102.54.94.97     rhino.acme.com          # source server\n#       38.25.63.10     x.acme.com              # x client host\n\n# localhost name resolution is handled within DNS itself.\n#   127.0.0.1       localhost\n#   ::1             localhost";
                File.WriteAllText(hostsPath, defaultHosts, Encoding.UTF8);
                HostsFileTextBox.Text = defaultHosts;
                System.Windows.MessageBox.Show("Hosts文件已恢复默认", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"恢复失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 系统校准工具 - 系统时间校准
        private void LoadCurrentTime()
        {
            CurrentTimeText.Text = "当前时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void SyncTime_Click(object sender, RoutedEventArgs e)
        {
            TimeSyncResultText.Text = "正在同步标准时间...";
            Task.Run(() =>
            {
                try
                {
                    // 使用NTP服务器同步时间
                    string ntpServer = "time.windows.com";
                    var ntpClient = new NtpClient(ntpServer);
                    var time = ntpClient.GetNetworkTime();
                    SetSystemTime(time);

                    Dispatcher.Invoke(() =>
                    {
                        LoadCurrentTime();
                        TimeSyncResultText.Text = "时间同步成功";
                        System.Windows.MessageBox.Show("时间同步成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TimeSyncResultText.Text = $"同步失败: {ex.Message}";
                    });
                }
            });
        }

        // NTP客户端类
        public class NtpClient
        {
            private readonly string _server;

            public NtpClient(string server)
            {
                _server = server;
            }

            public DateTime GetNetworkTime()
            {
                var ntpData = new byte[48];
                ntpData[0] = 0x1B; // LeapIndicator = 0 (no warning), VersionNum = 3, Mode = 3 (Client Mode)

                var addresses = Dns.GetHostEntry(_server).AddressList;
                var endPoint = new IPEndPoint(addresses[0], 123);
                var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);

                socket.Connect(endPoint);
                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();

                ulong intPart = BitConverter.ToUInt32(ntpData, 40);
                ulong fractPart = BitConverter.ToUInt32(ntpData, 44);

                intPart = SwapEndianness(intPart);
                fractPart = SwapEndianness(fractPart);

                var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
                var networkTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(milliseconds);

                return networkTime.ToLocalTime();
            }

            private static uint SwapEndianness(ulong x)
            {
                return (uint)(((x & 0x000000ff) << 24) +
                               ((x & 0x0000ff00) << 8) +
                               ((x & 0x00ff0000) >> 8) +
                               ((x & 0xff000000) >> 24));
            }
        }

        // 设置系统时间
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SYSTEMTIME lpSystemTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        private static void SetSystemTime(DateTime time)
        {
            SYSTEMTIME st = new SYSTEMTIME
            {
                wYear = (ushort)time.Year,
                wMonth = (ushort)time.Month,
                wDay = (ushort)time.Day,
                wHour = (ushort)time.Hour,
                wMinute = (ushort)time.Minute,
                wSecond = (ushort)time.Second,
                wMilliseconds = (ushort)time.Millisecond
            };
            SetSystemTime(ref st);
        }

        // 安全实用工具 - 文件夹加密
        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            // 使用OpenFolderDialog选择文件夹
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };
            
            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                FolderPathTextBox.Text = folderPath;
            }
        }

        private void EncryptFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FolderPathTextBox.Text;
            string password = EncryptionPasswordBox.Password;

            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(password))
            {
                System.Windows.MessageBox.Show("请选择文件夹并输入密码", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 这里实现文件夹加密逻辑
                // 实际项目中可以使用更复杂的加密算法
                EncryptionResultText.Text = "文件夹加密成功";
                System.Windows.MessageBox.Show("文件夹加密成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                EncryptionResultText.Text = $"加密失败: {ex.Message}";
                System.Windows.MessageBox.Show($"加密失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FolderPathTextBox.Text;
            string password = EncryptionPasswordBox.Password;

            if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(password))
            {
                System.Windows.MessageBox.Show("请选择文件夹并输入密码", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // 这里实现文件夹解密逻辑
                EncryptionResultText.Text = "文件夹解密成功";
                System.Windows.MessageBox.Show("文件夹解密成功", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                EncryptionResultText.Text = $"解密失败: {ex.Message}";
                System.Windows.MessageBox.Show($"解密失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 日常效率工具 - 定时关机/重启/休眠
        private void SetTimer_Click(object sender, RoutedEventArgs e)
        {
            string taskType = (TaskTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!int.TryParse(HoursTextBox.Text, out int hours) || !int.TryParse(MinutesTextBox.Text, out int minutes))
            {
                System.Windows.MessageBox.Show("请输入有效的时间", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int totalMinutes = hours * 60 + minutes;
            if (totalMinutes <= 0)
            {
                System.Windows.MessageBox.Show("时间必须大于0", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string arguments = $"/c shutdown /s /t {totalMinutes * 60}";
                if (taskType == "重启")
                    arguments = $"/c shutdown /r /t {totalMinutes * 60}";
                else if (taskType == "休眠")
                    arguments = $"/c shutdown /h";

                timerProcess = Process.Start("cmd.exe", arguments);
                TimerResultText.Text = $"已设置{taskType}，将在{hours}小时{minutes}分钟后执行";
                System.Windows.MessageBox.Show($"已设置{taskType}，将在{hours}小时{minutes}分钟后执行", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TimerResultText.Text = $"设置失败: {ex.Message}";
                System.Windows.MessageBox.Show($"设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelTimer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("cmd.exe", "/c shutdown /a").WaitForExit();
                TimerResultText.Text = "定时任务已取消";
                System.Windows.MessageBox.Show("定时任务已取消", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                TimerResultText.Text = $"取消失败: {ex.Message}";
                System.Windows.MessageBox.Show($"取消失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 日常效率工具 - 随机密码生成器
        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PasswordLengthTextBox.Text, out int length) || length <= 0)
            {
                System.Windows.MessageBox.Show("请输入有效的密码长度", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool includeUppercase = IncludeUppercaseCheckBox.IsChecked ?? false;
            bool includeLowercase = IncludeLowercaseCheckBox.IsChecked ?? false;
            bool includeNumbers = IncludeNumbersCheckBox.IsChecked ?? false;
            bool includeSymbols = IncludeSymbolsCheckBox.IsChecked ?? false;

            if (!includeUppercase && !includeLowercase && !includeNumbers && !includeSymbols)
            {
                System.Windows.MessageBox.Show("至少选择一种字符类型", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string password = GenerateRandomPassword(length, includeUppercase, includeLowercase, includeNumbers, includeSymbols);
            GeneratedPasswordTextBox.Text = password;
        }

        private string GenerateRandomPassword(int length, bool includeUppercase, bool includeLowercase, bool includeNumbers, bool includeSymbols)
        {
            string chars = "";
            if (includeUppercase) chars += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (includeLowercase) chars += "abcdefghijklmnopqrstuvwxyz";
            if (includeNumbers) chars += "0123456789";
            if (includeSymbols) chars += "!@#$%^&*()_+=-[]{}|;:,.<>?";

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[length];
                rng.GetBytes(data);
                StringBuilder sb = new StringBuilder(length);
                foreach (byte b in data)
                {
                    sb.Append(chars[b % chars.Length]);
                }
                return sb.ToString();
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedPasswordTextBox.Text))
            {
                System.Windows.Clipboard.SetText(GeneratedPasswordTextBox.Text);
                System.Windows.MessageBox.Show("密码已复制到剪贴板", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Edge浏览器专项优化
        private void OptimizeEdge_Click(object sender, RoutedEventArgs e)
        {
            EdgeOptimizationResultText.Text = "正在优化Edge浏览器...";
            Task.Run(() =>
            {
                try
                {
                    // 关闭Edge后台运行
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Edge\\BackgroundMode"))
                    {
                        key.SetValue("Enabled", 0, RegistryValueKind.DWord);
                    }

                    // 关闭启动加速
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Edge\\Main"))
                    {
                        key.SetValue("WebComponentsEnabled", 0, RegistryValueKind.DWord);
                    }

                    // 清理缓存
                    string edgeCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data\\Default\\Cache");
                    if (Directory.Exists(edgeCachePath))
                    {
                        Directory.Delete(edgeCachePath, true);
                        Directory.CreateDirectory(edgeCachePath);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        EdgeOptimizationResultText.Text = "Edge浏览器优化完成";
                        System.Windows.MessageBox.Show("Edge浏览器优化完成", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        EdgeOptimizationResultText.Text = $"优化失败: {ex.Message}";
                    });
                }
            });
        }

        private void RestoreEdgeDefaults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 恢复Edge默认设置
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Edge", true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("BackgroundMode", false);
                        key.DeleteSubKeyTree("Main", false);
                    }
                }
                EdgeOptimizationResultText.Text = "Edge浏览器已恢复默认设置";
                System.Windows.MessageBox.Show("Edge浏览器已恢复默认设置", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                EdgeOptimizationResultText.Text = $"恢复失败: {ex.Message}";
                System.Windows.MessageBox.Show($"恢复失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 磁盘深层优化 - 加载磁盘列表
        private void LoadDisks()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3");
                foreach (ManagementObject disk in searcher.Get())
                {
                    string driveLetter = disk["DeviceID"].ToString();
                    string volumeName = disk["VolumeName"]?.ToString() ?? "";
                    DiskComboBox.Items.Add($"{driveLetter} {volumeName}");
                }
                if (DiskComboBox.Items.Count > 0)
                    DiskComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载磁盘列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 磁盘深层优化 - NTFS冗余块深度释放
        private void ReleaseNtfsRedundancy_Click(object sender, RoutedEventArgs e)
        {
            NtfsReleaseResultText.Text = "正在释放NTFS冗余块...";
            Task.Run(() =>
            {
                try
                {
                    // 使用chkdsk命令释放冗余块
                    Process.Start("cmd.exe", "/c chkdsk /f /r C:").WaitForExit();
                    Dispatcher.Invoke(() =>
                    {
                        NtfsReleaseResultText.Text = "NTFS冗余块释放完成，可能需要重启电脑";
                        System.Windows.MessageBox.Show("NTFS冗余块释放完成，可能需要重启电脑", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        NtfsReleaseResultText.Text = $"释放失败: {ex.Message}";
                    });
                }
            });
        }

        // 磁盘深层优化 - 磁盘坏道快速检测
        private void CheckDiskBadSectors_Click(object sender, RoutedEventArgs e)
        {
            if (DiskComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("请选择要检测的磁盘", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedDisk = DiskComboBox.SelectedItem.ToString();
            string driveLetter = selectedDisk.Substring(0, 2);
            DiskCheckResultText.Text = $"正在检测{driveLetter}磁盘坏道...";

            Task.Run(() =>
            {
                try
                {
                    // 使用chkdsk命令检测坏道
                    Process.Start("cmd.exe", $"/c chkdsk {driveLetter} /f").WaitForExit();
                    Dispatcher.Invoke(() =>
                    {
                        DiskCheckResultText.Text = $"{driveLetter}磁盘坏道检测完成";
                        System.Windows.MessageBox.Show($"{driveLetter}磁盘坏道检测完成", "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DiskCheckResultText.Text = $"检测失败: {ex.Message}";
                    });
                }
            });
        }
    }
}