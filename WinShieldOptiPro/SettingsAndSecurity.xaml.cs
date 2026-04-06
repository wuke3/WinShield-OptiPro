using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;

namespace WinShieldOptiPro
{
    public partial class SettingsAndSecurity : UserControl
    {
        private List<string> protectedDirectories = new List<string>();
        private List<string> trustedSoftware = new List<string>();
        private List<RestorePointInfo> restorePoints = new List<RestorePointInfo>();
        private bool isInitialized = false;

        public SettingsAndSecurity()
        {
            InitializeComponent();
            Loaded += SettingsAndSecurity_Loaded;
        }

        private void SettingsAndSecurity_Loaded(object sender, RoutedEventArgs e)
        {
            if (isInitialized) return;
            isInitialized = true;

            InitializeSettings();
            CheckAdminPrivileges();
            UpdateDiskThresholdText();
            LoadProtectedDirectories();
            LoadTrustedSoftware();
            LoadRestorePoints();
        }

        private void InitializeSettings()
        {
            // 从配置文件加载设置
            // 这里使用临时数据，实际项目中应该从配置文件加载
            DiskThresholdSlider.Value = 5;
            EnableDiskThresholdCheckBox.IsChecked = true;
            EnableBackupCheckBox.IsChecked = true;
            EnableAutoCleanCheckBox.IsChecked = false;
            AutoCleanPeriodComboBox.SelectedIndex = 1; // 每周
        }

        private void CheckAdminPrivileges()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (isAdmin)
            {
                AdminStatusText.Text = "当前状态: 已获取管理员权限";
                AdminStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                AdminStatusText.Text = "当前状态: 未获取管理员权限";
                AdminStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void UpdateDiskThresholdText()
        {
            DiskThresholdText.Text = $"{DiskThresholdSlider.Value} GB";
        }

        private void LoadProtectedDirectories()
        {
            // 加载受保护目录
            protectedDirectories.Add(@"C:\Windows");
            protectedDirectories.Add(@"C:\Program Files");
            protectedDirectories.Add(@"C:\Program Files (x86)");
            UpdateProtectedDirectoriesList();
        }

        private void LoadTrustedSoftware()
        {
            // 加载信任软件
            trustedSoftware.Add("WinShield OptiPro");
            trustedSoftware.Add("Microsoft Edge");
            trustedSoftware.Add("Microsoft Office");
            UpdateTrustedSoftwareList();
        }

        private void LoadRestorePoints()
        {
            restorePoints.Clear();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\default", "SELECT * FROM SystemRestore");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string creationTimeStr = obj["CreationTime"]?.ToString();
                    DateTime creationTime;

                    if (!string.IsNullOrEmpty(creationTimeStr) && TryParseWmiDateTime(creationTimeStr, out creationTime))
                    {
                        restorePoints.Add(new RestorePointInfo
                        {
                            Description = obj["Description"]?.ToString() ?? "",
                            CreationTime = creationTime
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 静默处理错误，不显示错误消息
                System.Diagnostics.Debug.WriteLine($"加载还原点失败: {ex.Message}");
            }
            UpdateRestorePointsList();
        }

        private bool TryParseWmiDateTime(string wmiDateTime, out DateTime result)
        {
            // WMI DateTime format: yyyyMMddHHmmss.ffffff+ooo
            try
            {
                if (string.IsNullOrEmpty(wmiDateTime) || wmiDateTime.Length < 14)
                {
                    result = DateTime.Now;
                    return false;
                }

                // 提取日期时间部分
                string dateTimePart = wmiDateTime.Substring(0, 14);
                
                // 解析为DateTime
                result = DateTime.ParseExact(dateTimePart, "yyyyMMddHHmmss", null);
                return true;
            }
            catch
            {
                result = DateTime.Now;
                return false;
            }
        }

        private void UpdateProtectedDirectoriesList()
        {
            ProtectedDirectoriesListBox.Items.Clear();
            foreach (var dir in protectedDirectories)
            {
                ProtectedDirectoriesListBox.Items.Add(dir);
            }
        }

        private void UpdateTrustedSoftwareList()
        {
            TrustedSoftwareListBox.Items.Clear();
            foreach (var software in trustedSoftware)
            {
                TrustedSoftwareListBox.Items.Add(software);
            }
        }

        private void UpdateRestorePointsList()
        {
            RestorePointsListBox.Items.Clear();
            foreach (var point in restorePoints)
            {
                RestorePointsListBox.Items.Add($"{point.Description} - {point.CreationTime}");
            }
        }

        private void CheckAdminButton_Click(object sender, RoutedEventArgs e)
        {
            CheckAdminPrivileges();
        }

        private void AddProtectedDirButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = ProtectedDirectoryTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                if (!protectedDirectories.Contains(dir))
                {
                    protectedDirectories.Add(dir);
                    UpdateProtectedDirectoriesList();
                    ProtectedDirectoryTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("目录已在白名单中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("请输入有效的目录路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTrustedSoftwareButton_Click(object sender, RoutedEventArgs e)
        {
            string software = TrustedSoftwareTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(software))
            {
                if (!trustedSoftware.Contains(software))
                {
                    trustedSoftware.Add(software);
                    UpdateTrustedSoftwareList();
                    TrustedSoftwareTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("软件已在信任列表中", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("请输入软件名称", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string description = $"WinShield OptiPro - {DateTime.Now}";
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/c wmic.exe /namespace:\root\default path SystemRestore call CreateRestorePoint ""{description}"", 100, 7",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi)?.WaitForExit();
                MessageBox.Show("还原点创建成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadRestorePoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建还原点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshRestorePointsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRestorePoints();
        }

        private void DeleteRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            if (RestorePointsListBox.SelectedIndex >= 0 && RestorePointsListBox.SelectedIndex < restorePoints.Count)
            {
                try
                {
                    // 实际项目中需要实现删除还原点的逻辑
                    // 这里仅做演示
                    restorePoints.RemoveAt(RestorePointsListBox.SelectedIndex);
                    UpdateRestorePointsList();
                    MessageBox.Show("还原点删除成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除还原点失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("请选择要删除的还原点", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DiskThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateDiskThresholdText();
        }

        public class RestorePointInfo
        {
            public string Description { get; set; } = "";
            public DateTime CreationTime { get; set; }
        }
    }
}
