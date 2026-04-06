using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WinShieldOptiPro;

public partial class TrayPopup : Window
{
    private MainWindow mainWindow;
    private DispatcherTimer shieldAnimationTimer;
    
    public TrayPopup(MainWindow mainWindow)
    {
        InitializeComponent();
        this.mainWindow = mainWindow;
        Loaded += TrayPopup_Loaded;
    }
    
    private void TrayPopup_Loaded(object sender, RoutedEventArgs e)
    {
        // 启动防护盾旋转动画
        StartShieldRotation();
        // 更新系统状态
        UpdateSystemStatus();
    }
    
    private void StartShieldRotation()
    {
        DoubleAnimation rotationAnimation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(10),
            RepeatBehavior = RepeatBehavior.Forever
        };
        ShieldRotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
    }
    
    private void UpdateSystemStatus()
    {
        // 模拟系统健康度和C盘状态
        SystemStatusText.Text = "系统健康度: 100%";
        DiskStatusText.Text = "C盘: 充足";
    }
    
    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // 鼠标离开时关闭悬浮页
        Close();
    }
    
    private void QuickCleanButton_Click(object sender, RoutedEventArgs e)
    {
        // 触发一键清理
        mainWindow.QuickCleanButton_Click(sender, e);
        Close();
    }
    
    private void SystemOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // 触发一键优化
        mainWindow.SystemOptimizeButton_Click(sender, e);
        Close();
    }
    
    private void MemoryOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到内存优化页面
        mainWindow.MemoryOptimizerButton_Click(sender, e);
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        Close();
    }
    
    private void FullScanButton_Click(object sender, RoutedEventArgs e)
    {
        // 切换到静默查杀页面
        mainWindow.SystemScannerButton_Click(sender, e);
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        Close();
    }
    
    private void OpenMainWindowButton_Click(object sender, RoutedEventArgs e)
    {
        // 打开主界面
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        Close();
    }
    
    private void StartupToggle_Click(object sender, RoutedEventArgs e)
    {
        // 处理开机自启设置
        bool isEnabled = StartupToggle.IsChecked == true;
        // 这里可以实现开机自启的设置逻辑
    }
    
    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        // 退出软件
        Application.Current.Shutdown();
    }
}