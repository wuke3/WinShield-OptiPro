using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace WinShieldOptiPro
{
    public partial class FreezeRestore : System.Windows.Controls.UserControl
    {
        private List<string> whitelist = new List<string>();
        private List<Snapshot> snapshots = new List<Snapshot>();
        private DispatcherTimer? protectionTimer;
        private DateTime protectionExpiryTime;
        private bool isAdmin;
        private bool isInitialized = false;

        public FreezeRestore()
        {
            InitializeComponent();
            Loaded += FreezeRestore_Loaded;
        }

        private void FreezeRestore_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized) return;
            isInitialized = true;

            // 检查管理员权限
            isAdmin = IsRunningAsAdmin();

            InitializeEvents();
            LoadWhitelist();

            // 如果不是管理员，显示提示并禁用相关功能
            if (!isAdmin)
            {
                DisableAdminFeatures();
            }
            else
            {
                LoadSnapshots();
            }
        }

        private void DisableAdminFeatures()
        {
            // 禁用创建快照按钮
            if (CreateSnapshotButton != null)
                CreateSnapshotButton.IsEnabled = false;

            // 显示权限提示
            if (SnapshotsListBox != null)
            {
                SnapshotsListBox.Items.Clear();
                SnapshotsListBox.Items.Add("需要管理员权限才能查看和创建还原点");
            }
        }

        private void InitializeEvents()
        {
            if (ScheduledRestoreRadio != null)
                ScheduledRestoreRadio.Checked += (s, e) => { if (ScheduledRestorePanel != null) ScheduledRestorePanel.Visibility = Visibility.Visible; };

            if (RestartRestoreRadio != null)
                RestartRestoreRadio.Checked += (s, e) => { if (ScheduledRestorePanel != null) ScheduledRestorePanel.Visibility = Visibility.Collapsed; };

            if (ManualRestoreRadio != null)
                ManualRestoreRadio.Checked += (s, e) => { if (ScheduledRestorePanel != null) ScheduledRestorePanel.Visibility = Visibility.Collapsed; };

            if (TemporaryDisableRadio != null)
                TemporaryDisableRadio.Checked += (s, e) => { if (TemporaryDisablePanel != null) TemporaryDisablePanel.Visibility = Visibility.Visible; };

            if (AlwaysProtectRadio != null)
                AlwaysProtectRadio.Checked += (s, e) => { if (TemporaryDisablePanel != null) TemporaryDisablePanel.Visibility = Visibility.Collapsed; };

            if (BrowseButton != null)
                BrowseButton.Click += BrowseButton_Click;

            if (AddToWhitelistButton != null)
                AddToWhitelistButton.Click += AddToWhitelistButton_Click;

            if (CreateSnapshotButton != null)
                CreateSnapshotButton.Click += CreateSnapshotButton_Click;

            if (EnableProtectionButton != null)
                EnableProtectionButton.Click += EnableProtectionButton_Click;

            if (DisableProtectionButton != null)
                DisableProtectionButton.Click += DisableProtectionButton_Click;

            if (ApplySettingsButton != null)
                ApplySettingsButton.Click += ApplySettingsButton_Click;

            protectionTimer = new DispatcherTimer();
            protectionTimer.Interval = TimeSpan.FromMinutes(1);
            protectionTimer.Tick += ProtectionTimer_Tick;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == Forms.DialogResult.OK)
            {
                if (WhitelistPathTextBox != null)
                    WhitelistPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void AddToWhitelistButton_Click(object sender, RoutedEventArgs e)
        {
            if (WhitelistPathTextBox == null) return;

            string path = WhitelistPathTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path) && !whitelist.Contains(path))
            {
                whitelist.Add(path);
                if (WhitelistListBox != null)
                    WhitelistListBox.Items.Add(path);
                SaveWhitelist();
                WhitelistPathTextBox.Clear();
            }
        }

        private void CreateSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isAdmin)
            {
                MessageBox.Show("需要管理员权限才能创建还原点。\n请右键点击程序图标，选择'以管理员身份运行'。", "权限不足", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SnapshotNameTextBox == null) return;

            string name = SnapshotNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("请输入快照名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                string snapshotId = CreateVssSnapshot();
                if (!string.IsNullOrEmpty(snapshotId))
                {
                    var snapshot = new Snapshot
                    {
                        Id = snapshotId,
                        Name = name,
                        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    snapshots.Add(snapshot);
                    if (SnapshotsListBox != null)
                        SnapshotsListBox.Items.Add(snapshot);
                    SaveSnapshots();
                    SnapshotNameTextBox.Clear();
                    MessageBox.Show("快照创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建快照失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableProtectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (TemporaryDisableRadio != null && TemporaryDisableRadio.IsChecked == true)
            {
                if (DisableDurationComboBox != null)
                {
                    string? duration = (DisableDurationComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    protectionExpiryTime = GetExpiryTime(duration);
                    protectionTimer?.Start();
                    MessageBox.Show($"保护已开启，将在 {duration} 后自动重启", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("保护已开启", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DisableProtectionButton_Click(object sender, RoutedEventArgs e)
        {
            protectionTimer?.Stop();
            MessageBox.Show("保护已关闭", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已应用", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ProtectionTimer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.Now >= protectionExpiryTime)
            {
                protectionTimer?.Stop();
                MessageBox.Show("保护已自动重启", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string CreateVssSnapshot()
        {
            try
            {
                // 检查是否以管理员权限运行
                if (!IsRunningAsAdmin())
                {
                    throw new Exception("需要管理员权限才能创建还原点");
                }

                ManagementClass vssClass = new ManagementClass("Win32_ShadowCopy");
                ManagementBaseObject inParams = vssClass.GetMethodParameters("Create");
                inParams["Volume"] = "C:\\\\";
                ManagementBaseObject outParams = vssClass.InvokeMethod("Create", inParams, null);
                return outParams["ShadowID"]?.ToString() ?? "";
            }
            catch (ManagementException mex)
            {
                // 处理特定的管理异常
                if (mex.ErrorCode == ManagementStatus.AccessDenied)
                {
                    throw new Exception("访问被拒绝：需要管理员权限。请右键点击程序图标，选择'以管理员身份运行'。");
                }
                throw new Exception("创建卷影副本失败: " + mex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                throw new Exception("访问被拒绝：需要管理员权限。请右键点击程序图标，选择'以管理员身份运行'。");
            }
            catch (Exception ex)
            {
                throw new Exception("创建卷影副本失败: " + ex.Message);
            }
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private DateTime GetExpiryTime(string? duration)
        {
            switch (duration)
            {
                case "1小时":
                    return DateTime.Now.AddHours(1);
                case "4小时":
                    return DateTime.Now.AddHours(4);
                case "半天":
                    return DateTime.Now.AddHours(12);
                case "全天":
                    return DateTime.Now.AddDays(1);
                default:
                    return DateTime.Now.AddHours(1);
            }
        }

        private void SaveWhitelist()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whitelist.txt");
                File.WriteAllLines(path, whitelist);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存白名单失败: {ex.Message}");
            }
        }

        private void LoadWhitelist()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whitelist.txt");
                if (File.Exists(path))
                {
                    whitelist = new List<string>(File.ReadAllLines(path));
                    if (WhitelistListBox != null)
                    {
                        WhitelistListBox.Items.Clear();
                        foreach (string item in whitelist)
                        {
                            WhitelistListBox.Items.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载白名单失败: {ex.Message}");
            }
        }

        private void SaveSnapshots()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots.json");
                string json = System.Text.Json.JsonSerializer.Serialize(snapshots, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存快照失败: {ex.Message}");
            }
        }

        private void LoadSnapshots()
        {
            if (!isAdmin)
            {
                // 如果不是管理员，不尝试加载快照
                return;
            }

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "snapshots.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loadedSnapshots = System.Text.Json.JsonSerializer.Deserialize<List<Snapshot>>(json);
                    if (loadedSnapshots != null)
                    {
                        snapshots = loadedSnapshots;
                        if (SnapshotsListBox != null)
                        {
                            SnapshotsListBox.Items.Clear();
                            foreach (var snapshot in snapshots)
                            {
                                SnapshotsListBox.Items.Add(snapshot);
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // JSON解析错误，可能是格式不兼容，静默处理并清空列表
                System.Diagnostics.Debug.WriteLine("快照文件格式不兼容，已清空列表");
                snapshots = new List<Snapshot>();
                if (SnapshotsListBox != null)
                {
                    SnapshotsListBox.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载快照失败: {ex.Message}");
                // 不显示错误消息，静默处理
            }
        }

        private void SaveSettings()
        {
            try
            {
                // 保存设置到配置文件
                var settings = new Dictionary<string, string>
                {
                    { "ProtectC", CDriveCheckBox?.IsChecked.ToString() ?? "False" },
                    { "ProtectD", DDriveCheckBox?.IsChecked.ToString() ?? "False" },
                    { "ProtectE", EDriveCheckBox?.IsChecked.ToString() ?? "False" },
                    { "ProtectF", FDriveCheckBox?.IsChecked.ToString() ?? "False" },
                    { "RestoreMode", GetRestoreMode() },
                    { "AlwaysProtect", AlwaysProtectRadio?.IsChecked.ToString() ?? "True" }
                };

                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "freeze_settings.json");
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            }
        }

        private string GetRestoreMode()
        {
            if (RestartRestoreRadio?.IsChecked == true)
                return "Restart";
            else if (ScheduledRestoreRadio?.IsChecked == true)
                return "Scheduled";
            else
                return "Manual";
        }
    }

    public class Snapshot
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Time { get; set; } = "";

        public override string ToString()
        {
            return $"{Name} - {Time}";
        }
    }
}
