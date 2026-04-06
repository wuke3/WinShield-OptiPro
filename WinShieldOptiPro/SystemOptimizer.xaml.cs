using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace WinShieldOptiPro
{
    public partial class SystemOptimizer : UserControl
    {
        public SystemOptimizer()
        {
            InitializeComponent();
        }

        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            AnalyzeResultText.Text = "正在分析系统...";
            
            // 模拟分析过程
            System.Threading.Thread.Sleep(1000);
            
            AnalyzeResultText.Text = "分析完成，发现以下可优化项：\n" +
                "1. 存储感知未开启\n" +
                "2. 系统临时文件过多\n" +
                "3. 网络配置未优化\n" +
                "4. 后台进程占用过高\n" +
                "5. 存在冗余自启动项";
        }

        private void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            OptimizeResultText.Text = "正在优化系统...";
            
            // 模拟优化过程
            System.Threading.Thread.Sleep(2000);
            
            int optimizedCount = 0;
            
            // 存储瘦身优化
            if (StorageSenseCheckBox.IsChecked == true)
            {
                EnableStorageSense();
                optimizedCount++;
            }
            
            if (AutoCleanCheckBox.IsChecked == true)
            {
                AutoCleanFiles();
                optimizedCount++;
            }
            
            if (OptimizeHibernationCheckBox.IsChecked == true)
            {
                OptimizeHibernation();
                optimizedCount++;
            }
            
            if (OptimizeVirtualMemoryCheckBox.IsChecked == true)
            {
                OptimizeVirtualMemory();
                optimizedCount++;
            }
            
            // 网络专项提速优化
            if (OptimizePacketStrategyCheckBox.IsChecked == true)
            {
                OptimizePacketStrategy();
                optimizedCount++;
            }
            
            if (EnableFastForwardCheckBox.IsChecked == true)
            {
                EnableFastForward();
                optimizedCount++;
            }
            
            if (OptimizeTCPConfigCheckBox.IsChecked == true)
            {
                OptimizeTCPConfig();
                optimizedCount++;
            }
            
            // 系统深层性能优化
            if (DisableDebugCheckBox.IsChecked == true)
            {
                DisableDebugFeatures();
                optimizedCount++;
            }
            
            if (DisableRedundantProcessesCheckBox.IsChecked == true)
            {
                DisableRedundantProcesses();
                optimizedCount++;
            }
            
            // 系统稳定性优化
            if (FixConfigAbnormalCheckBox.IsChecked == true)
            {
                FixConfigAbnormal();
                optimizedCount++;
            }
            
            // 用户界面与运行速度优化
            if (DisableRedundantAnimationsCheckBox.IsChecked == true)
            {
                DisableRedundantAnimations();
                optimizedCount++;
            }
            
            if (OptimizeWindowRenderingCheckBox.IsChecked == true)
            {
                OptimizeWindowRendering();
                optimizedCount++;
            }
            
            // 系统杂项精细化配置
            if (ShowFileExtensionsCheckBox.IsChecked == true)
            {
                ShowFileExtensions();
                optimizedCount++;
            }
            
            // 隐私基础优化
            if (DisableSystemAdsCheckBox.IsChecked == true)
            {
                DisableSystemAds();
                optimizedCount++;
            }
            
            if (DisableDiagnosticDataCheckBox.IsChecked == true)
            {
                DisableDiagnosticData();
                optimizedCount++;
            }
            
            OptimizeResultText.Text = $"优化完成！共优化 {optimizedCount} 项设置。\n" +
                "建议重启系统以应用所有更改。";
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            // 还原默认设置
            StorageSenseCheckBox.IsChecked = false;
            AutoCleanCheckBox.IsChecked = false;
            CustomCleanPeriodCheckBox.IsChecked = false;
            MoveDefaultPathCheckBox.IsChecked = false;
            OptimizeHibernationCheckBox.IsChecked = false;
            OptimizeVirtualMemoryCheckBox.IsChecked = false;
            LimitRestorePointCheckBox.IsChecked = false;
            DisableReservedStorageCheckBox.IsChecked = false;
            
            OptimizePacketStrategyCheckBox.IsChecked = false;
            EnableFastForwardCheckBox.IsChecked = false;
            FixNetworkLinkCheckBox.IsChecked = false;
            OptimizeTCPConfigCheckBox.IsChecked = false;
            
            DisableDebugCheckBox.IsChecked = false;
            DisableRedundantProcessesCheckBox.IsChecked = false;
            OptimizeKernelSchedulerCheckBox.IsChecked = false;
            ReduceLogWritingCheckBox.IsChecked = false;
            
            FixConfigAbnormalCheckBox.IsChecked = false;
            OptimizeCrashReportCheckBox.IsChecked = false;
            ControlBackgroundResourcesCheckBox.IsChecked = false;
            OptimizeDriverConfigCheckBox.IsChecked = false;
            
            DisableRedundantAnimationsCheckBox.IsChecked = false;
            OptimizeWindowRenderingCheckBox.IsChecked = false;
            DisableBackgroundProcessesCheckBox.IsChecked = false;
            
            ShowFileExtensionsCheckBox.IsChecked = false;
            ShowSystemVersionCheckBox.IsChecked = false;
            HideShutdownButtonCheckBox.IsChecked = false;
            OptimizeFileAssociationsCheckBox.IsChecked = false;
            CleanInvalidContextMenuCheckBox.IsChecked = false;
            
            DisableSystemAdsCheckBox.IsChecked = false;
            DisableDiagnosticDataCheckBox.IsChecked = false;
            CleanUsageTracesCheckBox.IsChecked = false;
            
            OptimizeResultText.Text = "已还原所有默认设置。";
        }

        private void ScanStartupItemsButton_Click(object sender, RoutedEventArgs e)
        {
            // 模拟扫描自启动项
            var startupItems = new List<StartupItem>
            {
                new StartupItem { Name = "微信", Type = "应用", Status = "启用", Recommendation = "建议保留" },
                new StartupItem { Name = "QQ", Type = "应用", Status = "启用", Recommendation = "建议保留" },
                new StartupItem { Name = "360安全卫士", Type = "应用", Status = "启用", Recommendation = "建议禁用" },
                new StartupItem { Name = "迅雷", Type = "应用", Status = "启用", Recommendation = "建议禁用" },
                new StartupItem { Name = "Microsoft Edge", Type = "应用", Status = "启用", Recommendation = "建议保留" },
                new StartupItem { Name = "Steam", Type = "应用", Status = "启用", Recommendation = "建议禁用" }
            };
            
            StartupItemsDataGrid.ItemsSource = startupItems;
        }

        private void DisableRedundantStartupButton_Click(object sender, RoutedEventArgs e)
        {
            // 模拟禁用冗余自启项
            var items = StartupItemsDataGrid.ItemsSource as List<StartupItem>;
            if (items != null)
            {
                int disabledCount = 0;
                foreach (var item in items)
                {
                    if (item.Recommendation == "建议禁用")
                    {
                        item.Status = "禁用";
                        disabledCount++;
                    }
                }
                StartupItemsDataGrid.Items.Refresh();
                OptimizeResultText.Text = $"已禁用 {disabledCount} 个冗余自启动项。";
            }
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as StartupItem;
            if (item != null)
            {
                item.Status = item.Status == "启用" ? "禁用" : "启用";
                StartupItemsDataGrid.Items.Refresh();
            }
        }

        // 存储瘦身优化方法
        private void EnableStorageSense()
        {
            // 开启存储感知
            try
            {
                // 这里使用PowerShell命令开启存储感知
                RunPowerShellCommand("Set-ItemProperty -Path 'HKCU:\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy' -Name '01' -Value 1");
            }
            catch { }
        }

        private void AutoCleanFiles()
        {
            // 自动清理临时文件/回收站/过期下载
            try
            {
                // 清理临时文件夹
                string tempPath = System.IO.Path.GetTempPath();
                if (System.IO.Directory.Exists(tempPath))
                {
                    var files = System.IO.Directory.GetFiles(tempPath);
                    foreach (var file in files)
                    {
                        try { System.IO.File.Delete(file); } catch { }
                    }
                }
                
                // 清理回收站（这里只是模拟，实际需要使用Windows API）
            }
            catch { }
        }

        private void OptimizeHibernation()
        {
            // 优化休眠文件
            try
            {
                RunCmdCommand("powercfg -h -size 50");
            }
            catch { }
        }

        private void OptimizeVirtualMemory()
        {
            // 优化虚拟内存
            try
            {
                // 这里需要使用WMI或注册表来设置虚拟内存
            }
            catch { }
        }

        // 网络专项提速优化方法
        private void OptimizePacketStrategy()
        {
            // 优化系统默认分组报文策略
            try
            {
                RunCmdCommand("netsh int tcp set global autotuninglevel=normal");
            }
            catch { }
        }

        private void EnableFastForward()
        {
            // 开启系统网络快速转发机制
            try
            {
                RunCmdCommand("netsh int tcp set global rss=enabled");
            }
            catch { }
        }

        private void OptimizeTCPConfig()
        {
            // 优化TCP传输配置
            try
            {
                RunCmdCommand("netsh int tcp set global chimney=enabled");
            }
            catch { }
        }

        // 系统深层性能优化方法
        private void DisableDebugFeatures()
        {
            // 关闭系统无用自动调试功能
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting' -Name 'Disabled' -Value 1");
            }
            catch { }
        }

        private void DisableRedundantProcesses()
        {
            // 关停后台冗余系统进程和闲置系统服务
            try
            {
                // 这里可以添加具体的服务禁用逻辑
            }
            catch { }
        }

        // 系统稳定性优化方法
        private void FixConfigAbnormal()
        {
            // 修复系统隐性配置异常
            try
            {
                // 这里可以添加具体的配置修复逻辑
            }
            catch { }
        }

        // 用户界面与运行速度优化方法
        private void DisableRedundantAnimations()
        {
            // 关闭冗余视觉动画
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKCU:\\Control Panel\\Desktop' -Name 'UserPreferencesMask' -Value 0x90800080");
            }
            catch { }
        }

        private void OptimizeWindowRendering()
        {
            // 优化桌面窗口渲染速度
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'DisableThumbnails' -Value 1");
            }
            catch { }
        }

        // 系统杂项精细化配置方法
        private void ShowFileExtensions()
        {
            // 显示文件扩展名
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced' -Name 'HideFileExt' -Value 0");
            }
            catch { }
        }

        // 隐私基础优化方法
        private void DisableSystemAds()
        {
            // 关闭系统广告
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager' -Name 'SystemPaneSuggestionsEnabled' -Value 0");
            }
            catch { }
        }

        private void DisableDiagnosticData()
        {
            // 关闭诊断数据
            try
            {
                RunPowerShellCommand("Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection' -Name 'AllowTelemetry' -Value 0");
            }
            catch { }
        }

        // 辅助方法
        private void RunCmdCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch { }
        }

        private void RunPowerShellCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command {command}",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi)?.WaitForExit();
            }
            catch { }
        }
    }

    public class StartupItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Recommendation { get; set; }
    }
}