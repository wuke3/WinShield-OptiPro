using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WinShieldOptiPro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private System.Timers.Timer updateTimer;
        private Forms.NotifyIcon trayIcon;
        private GlassTooltip glassTooltip;
        private bool isInitialized = false;

        // 清理历史记录类
        public class CleaningHistoryItem
        {
            public string Time { get; set; }
            public string Type { get; set; }
            public string FreedSpace { get; set; }
            public string Status { get; set; }
        }

        // 使用ObservableCollection实现数据绑定
        private System.Collections.ObjectModel.ObservableCollection<CleaningHistoryItem> cleaningHistoryItems = new System.Collections.ObjectModel.ObservableCollection<CleaningHistoryItem>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }
        

        
        // 缓存提示文本，避免重复创建GlassTooltip
        private string lastTooltipText = string.Empty;
        
        private void AddTooltipEvents()
        {
            // 为所有导航按钮添加鼠标悬停事件
            HomeButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "返回首页概览");
            HardwareOverviewButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "查看硬件配置信息");
            DiskSpaceAnalyzerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "分析C盘空间使用情况");
            JunkCleanButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "清理系统垃圾文件");
            SystemOptimizeButtonNav.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "优化系统性能");
            MemoryOptimizerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "优化内存使用");
            AppDataMonitorButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "监控AppData目录");
            SoftwareUninstallerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "卸载软件并清理残留");
            SuperSlimmerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "深度清理系统空间");
            SystemToolboxButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "系统实用工具");
            PopupBlockerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "拦截弹窗广告");
            FreezeRestoreButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "系统冰点还原");
            SystemScannerButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "系统静默查杀");
            FileShredderButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "文件粉碎");
            SettingsAndSecurityButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "软件设置与安全中心");
            
            // 为快捷按钮添加鼠标悬停事件
            QuickCleanButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "一键快速清理系统垃圾");
            SystemOptimizeButton.MouseEnter += (s, e) => ShowGlassTooltip((System.Windows.Controls.Button)s, "一键系统最优优化");
        }
        
        private void ShowGlassTooltip(System.Windows.Controls.Button button, string text)
        {
            // 只有当提示文本变化时才创建和显示GlassTooltip
            if (text == lastTooltipText && glassTooltip != null && glassTooltip.Visibility == Visibility.Visible)
                return;

            try
            {
                // 延迟初始化GlassTooltip
                if (glassTooltip == null)
                {
                    glassTooltip = new GlassTooltip();
                    glassTooltip.Show();
                    glassTooltip.Hide();
                }

                // 获取按钮在屏幕上的位置
                System.Windows.Point point = button.PointToScreen(new System.Windows.Point(button.ActualWidth / 2, button.ActualHeight));

                // 显示毛玻璃悬浮提示
                glassTooltip.ShowTooltip(text, point.X - 100, point.Y + 10);
                lastTooltipText = text;
            }
            catch
            {
                // 静默处理错误
            }
        }

    // 缓存数据，用于检测变化
        private double lastUsagePercentage = -1;
        private int lastHealthScore = -1;
        private string lastTotalSpace = string.Empty;
        private string lastUsedSpace = string.Empty;
        private string lastFreeSpace = string.Empty;
        private int lastCpuUsage = -1;
        private int lastMemoryUsage = -1;
        
        // 性能计数器
        private System.Diagnostics.PerformanceCounter cpuCounter;
        private System.Diagnostics.PerformanceCounter memoryCounter;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化数据
            UpdateDiskSpaceInfo();
            UpdateSystemHealthScore();
            LoadCleaningHistory();
            
            // 初始化性能计数器
            InitializePerformanceCounters();
            UpdateStatusBar();

            // 设置定时更新 - 降低频率到10秒
            updateTimer = new System.Timers.Timer(10000); // 每10秒更新一次
            updateTimer.Elapsed += (s, args) => Dispatcher.Invoke(() => 
            {
                UpdateDiskSpaceInfo();
                UpdateStatusBar();
            });
            updateTimer.Start();

            // 初始化托盘图标
            InitializeTrayIcon();

            // 延迟初始化毛玻璃悬浮提示
            DispatcherTimer tooltipInitTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            tooltipInitTimer.Tick += (s, args) =>
            {
                tooltipInitTimer.Stop();
                try
                {
                    glassTooltip = new GlassTooltip();
                    glassTooltip.Show();
                    glassTooltip.Hide();
                    // 添加悬停提示事件
                    AddTooltipEvents();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GlassTooltip初始化失败: {ex.Message}");
                }
                finally
                {
                    // 标记窗口初始化完成
                    isInitialized = true;
                }
            };
            tooltipInitTimer.Start();
        }
    
    private void InitializeTrayIcon()
        {
            trayIcon = new Forms.NotifyIcon();
            try
            {
                // 尝试使用项目中的logo.png作为托盘图标
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    trayIcon.Icon = new System.Drawing.Icon(logoPath);
                }
                else
                {
                    // 如果logo.png不存在，使用默认图标
                    trayIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                // 如果出现错误，使用默认图标
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            trayIcon.Text = "WinShield OptiPro";
            trayIcon.Visible = true;
        
        // 添加托盘图标点击事件
        trayIcon.MouseClick += TrayIcon_MouseClick;
        
        // 添加上下文菜单
        Forms.ContextMenuStrip contextMenu = new Forms.ContextMenuStrip();
        Forms.ToolStripMenuItem openItem = new Forms.ToolStripMenuItem("打开主界面");
        openItem.Click += (s, args) =>
        {
            Show();
            WindowState = WindowState.Normal;
        };
        Forms.ToolStripMenuItem exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (s, args) => Application.Current.Shutdown();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(exitItem);
        trayIcon.ContextMenuStrip = contextMenu;
    }
    
    // 页面切换方法，添加动画效果
    private void SwitchPage(Grid targetPage, System.Windows.Controls.Button senderButton)
    {
        // 先隐藏所有可见页面
        Grid[] allPages = { HomePage, DiskSpaceAnalyzerPage, JunkCleanPage, SystemOptimizerPage, 
                          MemoryOptimizerPage, AppDataMonitorPage, SoftwareUninstallerPage, 
                          SuperSlimmerPage, HardwareOverviewPage, SystemToolboxPage, 
                          SystemScannerPage, FileShredderPage, PopupBlockerPage, 
                          FreezeRestorePage, SettingsAndSecurityPage };
        
        foreach (var page in allPages)
        {
            if (page.Visibility == Visibility.Visible && page != targetPage)
            {
                page.Visibility = Visibility.Collapsed;
            }
        }
        
        // 显示目标页面
        targetPage.Visibility = Visibility.Visible;
        targetPage.Opacity = 0;
        
        // 应用进入动画
        Storyboard fadeInStoryboard = (Storyboard)FindResource("PageFadeIn");
        if (fadeInStoryboard != null)
        {
            fadeInStoryboard.Begin(targetPage);
        }
        
        // 更新按钮状态
        StackPanel buttonPanel = (StackPanel)((Grid)senderButton.Parent).Children;
        foreach (System.Windows.Controls.Button button in buttonPanel.Children)
        {
            if (button != senderButton)
            {
                button.Background = new SolidColorBrush(MediaColor.FromRgb(42, 42, 42));
            }
        }
        senderButton.Background = new SolidColorBrush(MediaColor.FromRgb(0, 191, 255));
    }
    
    // 显示加载指示器
    private void ShowLoading()
    {
        LoadingIndicator.Visibility = Visibility.Visible;
    }
    
    // 隐藏加载指示器
    private void HideLoading()
    {
        LoadingIndicator.Visibility = Visibility.Collapsed;
    }
    
    private void TrayIcon_MouseClick(object sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            // 显示托盘悬浮页
            TrayPopup popup = new TrayPopup(this);
            // 设置悬浮页位置
            System.Drawing.Point mousePos = Forms.Cursor.Position;
            popup.Left = mousePos.X - 150;
            popup.Top = mousePos.Y - 450;
            popup.Show();
        }
    }
    
    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 确保窗口已经初始化完成
        if (!isInitialized)
        {
            isInitialized = true;
            return;
        }
        
        // 最小化到托盘，而不是关闭
        if (WindowState != WindowState.Minimized)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            Hide();
        }
    }
    
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void UpdateDiskSpaceInfo()
    {
        try
        {
            DriveInfo cDrive = new DriveInfo("C:");
            if (cDrive.IsReady)
            {
                long totalBytes = cDrive.TotalSize;
                long freeBytes = cDrive.AvailableFreeSpace;
                long usedBytes = totalBytes - freeBytes;
                double usagePercentage = (double)usedBytes / totalBytes * 100;

                // 格式化数据
                string totalSpace = $"{Math.Round(totalBytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
                string usedSpace = $"{Math.Round(usedBytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
                string freeSpace = $"{Math.Round(freeBytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
                string usagePercentageStr = $"{Math.Round(usagePercentage, 1)}%";

                // 只在数据变化时更新UI
                if (totalSpace != lastTotalSpace)
                {
                    TotalSpaceText.Text = totalSpace;
                    lastTotalSpace = totalSpace;
                }

                if (usedSpace != lastUsedSpace)
                {
                    UsedSpaceText.Text = usedSpace;
                    lastUsedSpace = usedSpace;
                }

                if (freeSpace != lastFreeSpace)
                {
                    FreeSpaceText.Text = freeSpace;
                    lastFreeSpace = freeSpace;
                }

                if (Math.Abs(usagePercentage - lastUsagePercentage) > 0.1) // 只有变化超过0.1%时才更新
                {
                    UsagePercentageText.Text = usagePercentageStr;
                    SpaceProgressBar.Value = usagePercentage;
                    lastUsagePercentage = usagePercentage;

                    // 设置进度条颜色
                    if (usagePercentage > 90)
                    {
                        SpaceProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else if (usagePercentage > 70)
                    {
                        SpaceProgressBar.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        SpaceProgressBar.Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 191, 255));
                    }

                    // 同时更新系统健康度
                    UpdateSystemHealthScore();
                }
            }
        }
        catch (Exception ex)
        {
            // 处理异常
        }
    }

    private void UpdateSystemHealthScore()
    {
        try
        {
            DriveInfo cDrive = new DriveInfo("C:");
            if (cDrive.IsReady)
            {
                long totalBytes = cDrive.TotalSize;
                long freeBytes = cDrive.AvailableFreeSpace;
                double freePercentage = (double)freeBytes / totalBytes * 100;

                // 计算健康度评分
                int healthScore = 100;
                string warningText = string.Empty;
                Brush warningBrush = null;
                Brush scoreBrush = null;

                if (freePercentage < 10)
                {
                    healthScore = 20;
                    warningText = "C盘空间严重不足，建议立即清理";
                    warningBrush = new SolidColorBrush(Colors.Red);
                    scoreBrush = new SolidColorBrush(Colors.Red);
                }
                else if (freePercentage < 20)
                {
                    healthScore = 40;
                    warningText = "C盘空间不足，建议清理";
                    warningBrush = new SolidColorBrush(Colors.Orange);
                    scoreBrush = new SolidColorBrush(Colors.Orange);
                }
                else if (freePercentage < 30)
                {
                    healthScore = 60;
                    warningText = "C盘空间紧张，建议定期清理";
                    warningBrush = new SolidColorBrush(Colors.Yellow);
                    scoreBrush = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    healthScore = 100;
                    warningText = "系统状态良好，无风险";
                    warningBrush = new SolidColorBrush(Colors.Green);
                    scoreBrush = new SolidColorBrush(Colors.Green);
                }

                // 只在健康度评分变化时更新UI
                if (healthScore != lastHealthScore)
                {
                    HealthScoreText.Text = healthScore.ToString();
                    HealthScoreText.Foreground = scoreBrush;
                    RiskWarningText.Text = warningText;
                    RiskWarningText.Foreground = warningBrush;
                    lastHealthScore = healthScore;
                }
            }
        }
        catch (Exception ex)
        {
            // 处理异常
        }
    }

    // 初始化性能计数器
    private void InitializePerformanceCounters()
    {
        try
        {
            // 初始化CPU计数器
            cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // 第一次调用返回0，需要调用一次
            
            // 初始化内存计数器
            memoryCounter = new System.Diagnostics.PerformanceCounter("Memory", "% Committed Bytes In Use");
            memoryCounter.NextValue(); // 第一次调用返回0，需要调用一次
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"性能计数器初始化失败: {ex.Message}");
        }
    }

    // 更新底部状态栏
    private void UpdateStatusBar()
    {
        try
        {
            // 更新CPU使用率
            if (cpuCounter != null)
            {
                int cpuUsage = (int)Math.Round(cpuCounter.NextValue());
                if (cpuUsage != lastCpuUsage)
                {
                    CpuUsageText.Text = $"CPU: {cpuUsage}%";
                    lastCpuUsage = cpuUsage;
                }
            }
            
            // 更新内存使用率
            if (memoryCounter != null)
            {
                int memoryUsage = (int)Math.Round(memoryCounter.NextValue());
                if (memoryUsage != lastMemoryUsage)
                {
                    MemoryUsageText.Text = $"内存: {memoryUsage}%";
                    lastMemoryUsage = memoryUsage;
                }
            }
            
            // 更新C盘使用率
            DriveInfo cDrive = new DriveInfo("C:");
            if (cDrive.IsReady)
            {
                long totalBytes = cDrive.TotalSize;
                long freeBytes = cDrive.AvailableFreeSpace;
                long usedBytes = totalBytes - freeBytes;
                int diskUsage = (int)Math.Round((double)usedBytes / totalBytes * 100);
                DiskUsageText.Text = $"C盘: {diskUsage}%";
            }
        }
        catch (Exception ex)
        {
            // 处理异常
        }
    }

    private void LoadCleaningHistory()
    {
        // 清空现有数据
        cleaningHistoryItems.Clear();
        
        // 模拟清理历史数据
        cleaningHistoryItems.Add(new CleaningHistoryItem { Time = "2026-03-29 10:30", Type = "系统垃圾", FreedSpace = "2.5 GB", Status = "成功" });
        cleaningHistoryItems.Add(new CleaningHistoryItem { Time = "2026-03-28 15:45", Type = "浏览器缓存", FreedSpace = "1.2 GB", Status = "成功" });
        cleaningHistoryItems.Add(new CleaningHistoryItem { Time = "2026-03-27 09:15", Type = "注册表清理", FreedSpace = "0.5 GB", Status = "成功" });
        cleaningHistoryItems.Add(new CleaningHistoryItem { Time = "2026-03-26 14:20", Type = "系统优化", FreedSpace = "0.8 GB", Status = "成功" });

        // 设置ItemsSource为ObservableCollection
        CleaningHistoryDataGrid.ItemsSource = cleaningHistoryItems;
    }

    public void QuickCleanButton_Click(object sender, RoutedEventArgs e)
    {
        // 模拟一键快速清理功能
        System.Windows.MessageBox.Show("一键快速清理功能已触发", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        
        // 更新清理历史
        AddCleaningHistory("系统垃圾", "3.2 GB");
    }

    public void SystemOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // 模拟一键系统优化功能
        System.Windows.MessageBox.Show("一键系统优化功能已触发", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        
        // 更新清理历史
        AddCleaningHistory("系统优化", "1.5 GB");
    }

    private void AddCleaningHistory(string type, string freedSpace)
    {
        // 获取当前时间
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        
        // 创建新的清理记录
        CleaningHistoryItem newItem = new CleaningHistoryItem
        {
            Time = currentTime,
            Type = type,
            FreedSpace = freedSpace,
            Status = "成功"
        };
        
        // 使用ObservableCollection添加新记录
        cleaningHistoryItems.Insert(0, newItem);
        
        // 只保留最近10条记录
        if (cleaningHistoryItems.Count > 10)
        {
            cleaningHistoryItems.RemoveAt(cleaningHistoryItems.Count - 1);
        }
    }



    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 停止定时器
        if (updateTimer != null)
        {
            updateTimer.Stop();
            updateTimer.Dispose();
        }
        
        // 清理托盘图标
        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }
        
        Application.Current.Shutdown();
    }

    private void DiskSpaceAnalyzerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到空间分析页面
        SwitchPage(DiskSpaceAnalyzerPage, sender as System.Windows.Controls.Button);
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到首页
        SwitchPage(HomePage, sender as System.Windows.Controls.Button);
    }

    private void JunkCleanButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到垃圾清理页面
        SwitchPage(JunkCleanPage, sender as System.Windows.Controls.Button);
    }

    private void SystemOptimizeButtonNav_Click(object sender, RoutedEventArgs e)
    {
        // 切换到系统优化页面
        SwitchPage(SystemOptimizerPage, sender as System.Windows.Controls.Button);
    }
    
    private void AppDataMonitorButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到AppData监控页面
        SwitchPage(AppDataMonitorPage, sender as System.Windows.Controls.Button);
    }
    
    private void SoftwareUninstallerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到软件卸载页面
        SwitchPage(SoftwareUninstallerPage, sender as System.Windows.Controls.Button);
    }
    
    private void SuperSlimmerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到超级瘦身页面
        SwitchPage(SuperSlimmerPage, sender as System.Windows.Controls.Button);
    }
    
    private void HardwareOverviewButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到硬件概览页面
        SwitchPage(HardwareOverviewPage, sender as System.Windows.Controls.Button);
    }
    
    public void MemoryOptimizerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到内存优化页面
        SwitchPage(MemoryOptimizerPage, sender as System.Windows.Controls.Button);
    }
    
    private void SystemToolboxButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到系统工具箱页面
        SwitchPage(SystemToolboxPage, sender as System.Windows.Controls.Button);
    }
    
    public void SystemScannerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到系统静默查杀页面
        SwitchPage(SystemScannerPage, sender as System.Windows.Controls.Button);
    }

    private void FileShredderButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到文件粉碎页面
        SwitchPage(FileShredderPage, sender as System.Windows.Controls.Button);
    }

    private void SettingsAndSecurityButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到基础设置&安全中心页面
        SwitchPage(SettingsAndSecurityPage, sender as System.Windows.Controls.Button);
    }
    
    private void PopupBlockerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到弹窗拦截页面
        SwitchPage(PopupBlockerPage, sender as System.Windows.Controls.Button);
    }

    private void FreezeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到冰点还原页面
        SwitchPage(FreezeRestorePage, sender as System.Windows.Controls.Button);
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        // 模拟扫描过程
        ScanResultText.Text = "正在扫描垃圾文件...";
        ShowLoading();
        
        // 启动异步扫描
        System.Threading.Tasks.Task.Run(() =>
        {
            // 模拟扫描时间
            System.Threading.Thread.Sleep(2000);
            
            // 计算可清理空间
            long totalJunkSize = 0;
            
            // 检查回收站
            if (RecycleBinCheckBox.IsChecked == true)
            {
                totalJunkSize += 500 * 1024 * 1024; // 500MB
            }
            
            // 检查系统临时文件
            if (TempFilesCheckBox.IsChecked == true)
            {
                totalJunkSize += 1000 * 1024 * 1024; // 1GB
            }
            
            // 检查日志文件
            if (LogFilesCheckBox.IsChecked == true)
            {
                totalJunkSize += 300 * 1024 * 1024; // 300MB
            }
            
            // 检查预读文件
            if (PrefetchCheckBox.IsChecked == true)
            {
                totalJunkSize += 200 * 1024 * 1024; // 200MB
            }
            
            // 检查缩略图缓存
            if (ThumbnailCacheCheckBox.IsChecked == true)
            {
                totalJunkSize += 400 * 1024 * 1024; // 400MB
            }
            
            // 检查浏览器缓存
            if (BrowserCacheCheckBox.IsChecked == true)
            {
                totalJunkSize += 800 * 1024 * 1024; // 800MB
            }
            
            // 检查Office缓存
            if (OfficeCacheCheckBox.IsChecked == true)
            {
                totalJunkSize += 300 * 1024 * 1024; // 300MB
            }
            
            // 检查系统更新残留包
            if (UpdateCacheCheckBox.IsChecked == true)
            {
                totalJunkSize += 1500 * 1024 * 1024; // 1.5GB
            }
            
            // 检查AppData
            if (AppDataCheckBox.IsChecked == true)
            {
                totalJunkSize += 2000 * 1024 * 1024; // 2GB
            }
            
            // 检查ProgramData
            if (ProgramDataCheckBox.IsChecked == true)
            {
                totalJunkSize += 1000 * 1024 * 1024; // 1GB
            }
            
            // 检查Windows更新下载缓存
            if (WindowsUpdateCacheCheckBox.IsChecked == true)
            {
                totalJunkSize += 3000L * 1024 * 1024; // 3GB
            }
            
            // 检查Windows.old
            if (WindowsOldCheckBox.IsChecked == true)
            {
                totalJunkSize += 10000L * 1024 * 1024; // 10GB
            }
            
            // 检查系统错误报告
            if (ErrorReportsCheckBox.IsChecked == true)
            {
                totalJunkSize += 500 * 1024 * 1024; // 500MB
            }
            
            // 检查注册表清理
            if (InvalidRegistryCheckBox.IsChecked == true ||
                UninstallResidueCheckBox.IsChecked == true ||
                InvalidShortcutsCheckBox.IsChecked == true ||
                RedundantContextMenuCheckBox.IsChecked == true)
            {
                totalJunkSize += 100 * 1024 * 1024; // 100MB
            }
            
            // 格式化结果
            string junkSizeStr = FormatSize(totalJunkSize);
            
            // 更新UI
            Dispatcher.Invoke(() =>
            {
                ScanResultText.Text = $"扫描完成！可清理垃圾：{junkSizeStr}";
                HideLoading();
            });
        });
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        // 预览清理内容
        string previewContent = "清理预览：\n";
        
        if (RecycleBinCheckBox.IsChecked == true)
        {
            previewContent += "- 回收站\n";
        }
        if (TempFilesCheckBox.IsChecked == true)
        {
            previewContent += "- 系统临时文件\n";
        }
        if (LogFilesCheckBox.IsChecked == true)
        {
            previewContent += "- 日志文件\n";
        }
        if (PrefetchCheckBox.IsChecked == true)
        {
            previewContent += "- 预读文件\n";
        }
        if (ThumbnailCacheCheckBox.IsChecked == true)
        {
            previewContent += "- 缩略图缓存\n";
        }
        if (BrowserCacheCheckBox.IsChecked == true)
        {
            previewContent += "- 浏览器缓存/历史记录\n";
        }
        if (OfficeCacheCheckBox.IsChecked == true)
        {
            previewContent += "- Office缓存\n";
        }
        if (UpdateCacheCheckBox.IsChecked == true)
        {
            previewContent += "- 系统更新残留包\n";
        }
        if (AppDataCheckBox.IsChecked == true)
        {
            previewContent += "- AppData全目录\n";
        }
        if (ProgramDataCheckBox.IsChecked == true)
        {
            previewContent += "- ProgramData公共缓存\n";
        }
        if (WindowsUpdateCacheCheckBox.IsChecked == true)
        {
            previewContent += "- Windows更新下载缓存\n";
        }
        if (WindowsOldCheckBox.IsChecked == true)
        {
            previewContent += "- Windows.old\n";
        }
        if (ErrorReportsCheckBox.IsChecked == true)
        {
            previewContent += "- 系统错误报告\n";
        }
        if (InvalidRegistryCheckBox.IsChecked == true)
        {
            previewContent += "- 无效注册表项\n";
        }
        if (UninstallResidueCheckBox.IsChecked == true)
        {
            previewContent += "- 软件卸载残留\n";
        }
        if (InvalidShortcutsCheckBox.IsChecked == true)
        {
            previewContent += "- 无效快捷方式关联\n";
        }
        if (RedundantContextMenuCheckBox.IsChecked == true)
        {
            previewContent += "- 冗余右键菜单\n";
        }
        
        // 二次确认
        System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(previewContent + "\n确定要清理这些内容吗？", "清理预览", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.No)
        {
            return;
        }
        
        // 开始清理
        CleanButton_Click(sender, e);
    }

    private void CleanButton_Click(object sender, RoutedEventArgs e)
    {
        // 模拟清理过程
        CleanResultText.Text = "正在清理...";
        ShowLoading();
        
        // 启动异步清理
        System.Threading.Tasks.Task.Run(() =>
        {
            // 模拟清理时间
            System.Threading.Thread.Sleep(3000);
            
            // 计算释放空间
            long freedSpace = 0;
            
            // 清理回收站
            if (RecycleBinCheckBox.IsChecked == true)
            {
                freedSpace += 500 * 1024 * 1024; // 500MB
                EmptyRecycleBin();
            }
            
            // 清理系统临时文件
            if (TempFilesCheckBox.IsChecked == true)
            {
                freedSpace += 1000 * 1024 * 1024; // 1GB
                CleanTempFiles();
            }
            
            // 清理日志文件
            if (LogFilesCheckBox.IsChecked == true)
            {
                freedSpace += 300 * 1024 * 1024; // 300MB
                CleanLogFiles();
            }
            
            // 清理预读文件
            if (PrefetchCheckBox.IsChecked == true)
            {
                freedSpace += 200 * 1024 * 1024; // 200MB
                CleanPrefetchFiles();
            }
            
            // 清理缩略图缓存
            if (ThumbnailCacheCheckBox.IsChecked == true)
            {
                freedSpace += 400 * 1024 * 1024; // 400MB
                CleanThumbnailCache();
            }
            
            // 清理浏览器缓存
            if (BrowserCacheCheckBox.IsChecked == true)
            {
                freedSpace += 800 * 1024 * 1024; // 800MB
                CleanBrowserCache();
            }
            
            // 清理Office缓存
            if (OfficeCacheCheckBox.IsChecked == true)
            {
                freedSpace += 300 * 1024 * 1024; // 300MB
                CleanOfficeCache();
            }
            
            // 清理系统更新残留包
            if (UpdateCacheCheckBox.IsChecked == true)
            {
                freedSpace += 1500 * 1024 * 1024; // 1.5GB
                CleanUpdateCache();
            }
            
            // 清理AppData
            if (AppDataCheckBox.IsChecked == true)
            {
                freedSpace += 2000 * 1024 * 1024; // 2GB
                CleanAppData();
            }
            
            // 清理ProgramData
            if (ProgramDataCheckBox.IsChecked == true)
            {
                freedSpace += 1000 * 1024 * 1024; // 1GB
                CleanProgramData();
            }
            
            // 清理Windows更新下载缓存
            if (WindowsUpdateCacheCheckBox.IsChecked == true)
            {
                freedSpace += 3000L * 1024 * 1024; // 3GB
                CleanWindowsUpdateCache();
            }
            
            // 清理Windows.old
            if (WindowsOldCheckBox.IsChecked == true)
            {
                freedSpace += 10000L * 1024 * 1024; // 10GB
                CleanWindowsOld();
            }
            
            // 清理系统错误报告
            if (ErrorReportsCheckBox.IsChecked == true)
            {
                freedSpace += 500 * 1024 * 1024; // 500MB
                CleanErrorReports();
            }
            
            // 清理注册表
            if (InvalidRegistryCheckBox.IsChecked == true ||
                UninstallResidueCheckBox.IsChecked == true ||
                InvalidShortcutsCheckBox.IsChecked == true ||
                RedundantContextMenuCheckBox.IsChecked == true)
            {
                freedSpace += 100 * 1024 * 1024; // 100MB
                CleanRegistry();
            }
            
            // 格式化结果
            string freedSpaceStr = FormatSize(freedSpace);
            
            // 更新UI
            Dispatcher.Invoke(() =>
            {
                CleanResultText.Text = $"清理完成！释放空间：{freedSpaceStr}";
                
                // 更新清理历史
                AddCleaningHistory("全量垃圾清理", freedSpaceStr);
                
                // 刷新磁盘空间信息
                UpdateDiskSpaceInfo();
                UpdateSystemHealthScore();
                
                HideLoading();
            });
        });
    }

    // 格式化文件大小
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

    // 清空回收站
    private void EmptyRecycleBin()
    {
        try
        {
            // 模拟回收站清理
            // 实际项目中可以使用更可靠的方法
        }
        catch { }
    }

    // 清理系统临时文件
    private void CleanTempFiles()
    {
        try
        {
            string tempPath = System.IO.Path.GetTempPath();
            if (System.IO.Directory.Exists(tempPath))
            {
                CleanDirectory(tempPath);
            }
        }
        catch { }
    }

    // 清理日志文件
    private void CleanLogFiles()
    {
        try
        {
            string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Logs");
            if (System.IO.Directory.Exists(logPath))
            {
                CleanDirectory(logPath);
            }
        }
        catch { }
    }

    // 清理预读文件
    private void CleanPrefetchFiles()
    {
        try
        {
            string prefetchPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
            if (System.IO.Directory.Exists(prefetchPath))
            {
                CleanDirectory(prefetchPath);
            }
        }
        catch { }
    }

    // 清理缩略图缓存
    private void CleanThumbnailCache()
    {
        try
        {
            string thumbnailPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Windows", "Explorer"
            );
            if (System.IO.Directory.Exists(thumbnailPath))
            {
                foreach (string file in System.IO.Directory.GetFiles(thumbnailPath, "thumbcache_*.db"))
                {
                    try { System.IO.File.Delete(file); } catch { }
                }
            }
        }
        catch { }
    }

    // 清理浏览器缓存
    private void CleanBrowserCache()
    {
        try
        {
            // 清理Edge/Chrome缓存
            string browserCachePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Edge", "User Data", "Default", "Cache"
            );
            if (System.IO.Directory.Exists(browserCachePath))
            {
                CleanDirectory(browserCachePath);
            }
        }
        catch { }
    }

    // 清理Office缓存
    private void CleanOfficeCache()
    {
        try
        {
            string officeCachePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "Office", "16.0", "OfficeFileCache"
            );
            if (System.IO.Directory.Exists(officeCachePath))
            {
                CleanDirectory(officeCachePath);
            }
        }
        catch { }
    }

    // 清理系统更新残留包
    private void CleanUpdateCache()
    {
        try
        {
            string updateCachePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "SoftwareDistribution", "Download"
            );
            if (System.IO.Directory.Exists(updateCachePath))
            {
                CleanDirectory(updateCachePath);
            }
        }
        catch { }
    }

    // 清理AppData
    private void CleanAppData()
    {
        try
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localLowPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
            
            // 清理Local目录
            if (System.IO.Directory.Exists(appDataPath))
            {
                CleanAppDataDirectory(appDataPath);
            }
            
            // 清理Roaming目录
            if (System.IO.Directory.Exists(roamingPath))
            {
                CleanAppDataDirectory(roamingPath);
            }
            
            // 清理LocalLow目录
            if (System.IO.Directory.Exists(localLowPath))
            {
                CleanAppDataDirectory(localLowPath);
            }
        }
        catch { }
    }

    // 清理AppData目录
    private void CleanAppDataDirectory(string path)
    {
        try
        {
            // 定义需要跳过的重要目录
            string[] importantDirs = { "Microsoft", "Google", "Mozilla", "Adobe", "Oracle", "Apple", "Dropbox", "OneDrive" };
            
            foreach (string dir in System.IO.Directory.GetDirectories(path))
            {
                string dirName = System.IO.Path.GetFileName(dir);
                // 跳过重要目录
                if (Array.Exists(importantDirs, d => d.Equals(dirName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                
                try
                {
                    System.IO.Directory.Delete(dir, true);
                }
                catch { }
            }
        }
        catch { }
    }

    // 清理ProgramData
    private void CleanProgramData()
    {
        try
        {
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (System.IO.Directory.Exists(programDataPath))
            {
                // 定义需要跳过的重要目录
                string[] importantDirs = { "Microsoft", "Windows", "Adobe", "Oracle", "Apple", "Dropbox", "OneDrive", "Google", "Mozilla" };
                
                foreach (string dir in System.IO.Directory.GetDirectories(programDataPath))
                {
                    string dirName = System.IO.Path.GetFileName(dir);
                    // 跳过重要目录
                    if (Array.Exists(importantDirs, d => d.Equals(dirName, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    
                    try
                    {
                        System.IO.Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    // 清理Windows更新下载缓存
    private void CleanWindowsUpdateCache()
    {
        try
        {
            string updateCachePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "SoftwareDistribution", "Download"
            );
            if (System.IO.Directory.Exists(updateCachePath))
            {
                CleanDirectory(updateCachePath);
            }
        }
        catch { }
    }

    // 清理Windows.old
    private void CleanWindowsOld()
    {
        try
        {
            string windowsOldPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old");
            if (System.IO.Directory.Exists(windowsOldPath))
            {
                System.IO.Directory.Delete(windowsOldPath, true);
            }
        }
        catch { }
    }

    // 清理系统错误报告
    private void CleanErrorReports()
    {
        try
        {
            string errorReportPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft", "Windows", "WER", "ReportArchive"
            );
            if (System.IO.Directory.Exists(errorReportPath))
            {
                CleanDirectory(errorReportPath);
            }
        }
        catch { }
    }

    // 清理注册表
    private void CleanRegistry()
    {
        try
        {
            // 这里需要实现注册表清理逻辑
            // 由于注册表操作风险较高，这里只做模拟
        }
        catch { }
    }

    // 清理目录
    private void CleanDirectory(string path)
    {
        try
        {
            foreach (string file in System.IO.Directory.GetFiles(path))
            {
                try { System.IO.File.Delete(file); } catch { }
            }
            foreach (string dir in System.IO.Directory.GetDirectories(path))
            {
                try { System.IO.Directory.Delete(dir, true); } catch { }
            }
        }
        catch { }
    }

    private void PopupBlockerButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到弹窗拦截页面
        HomePage.Visibility = Visibility.Collapsed;
        DiskSpaceAnalyzerPage.Visibility = Visibility.Collapsed;
        JunkCleanPage.Visibility = Visibility.Collapsed;
        SystemOptimizerPage.Visibility = Visibility.Collapsed;
        MemoryOptimizerPage.Visibility = Visibility.Collapsed;
        AppDataMonitorPage.Visibility = Visibility.Collapsed;
        SoftwareUninstallerPage.Visibility = Visibility.Collapsed;
        SuperSlimmerPage.Visibility = Visibility.Collapsed;
        HardwareOverviewPage.Visibility = Visibility.Collapsed;
        SystemToolboxPage.Visibility = Visibility.Collapsed;
        SystemScannerPage.Visibility = Visibility.Collapsed;
        FileShredderPage.Visibility = Visibility.Collapsed;
        PopupBlockerPage.Visibility = Visibility.Visible;
        FreezeRestorePage.Visibility = Visibility.Collapsed;
        SettingsAndSecurityPage.Visibility = Visibility.Collapsed;
        
        // 更新按钮状态
        foreach (System.Windows.Controls.Button button in ((StackPanel)((Grid)sender).Parent).Children)
        {
            button.Background = new SolidColorBrush(MediaColor.FromRgb(42, 42, 42));
        }
        (sender as System.Windows.Controls.Button).Background = new SolidColorBrush(MediaColor.FromRgb(0, 191, 255));
    }

    private void FreezeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到冰点还原页面
        HomePage.Visibility = Visibility.Collapsed;
        DiskSpaceAnalyzerPage.Visibility = Visibility.Collapsed;
        JunkCleanPage.Visibility = Visibility.Collapsed;
        SystemOptimizerPage.Visibility = Visibility.Collapsed;
        MemoryOptimizerPage.Visibility = Visibility.Collapsed;
        AppDataMonitorPage.Visibility = Visibility.Collapsed;
        SoftwareUninstallerPage.Visibility = Visibility.Collapsed;
        SuperSlimmerPage.Visibility = Visibility.Collapsed;
        HardwareOverviewPage.Visibility = Visibility.Collapsed;
        SystemToolboxPage.Visibility = Visibility.Collapsed;
        SystemScannerPage.Visibility = Visibility.Collapsed;
        FileShredderPage.Visibility = Visibility.Collapsed;
        PopupBlockerPage.Visibility = Visibility.Collapsed;
        FreezeRestorePage.Visibility = Visibility.Visible;
        SettingsAndSecurityPage.Visibility = Visibility.Collapsed;
        
        // 更新按钮状态
        foreach (System.Windows.Controls.Button button in ((StackPanel)((Grid)sender).Parent).Children)
        {
            button.Background = new SolidColorBrush(MediaColor.FromRgb(42, 42, 42));
        }
        (sender as System.Windows.Controls.Button).Background = new SolidColorBrush(MediaColor.FromRgb(0, 191, 255));
    }
}
}