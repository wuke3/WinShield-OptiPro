using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;

namespace WinShieldOptiPro;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 添加全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            try
            {
                // 处理命令行参数
                if (e.Args.Length > 0)
                {
                    if (e.Args[0] == "/shred" && e.Args.Length > 1)
                    {
                        // 处理文件粉碎命令
                        string[] paths = new string[e.Args.Length - 1];
                        for (int i = 1; i < e.Args.Length; i++)
                        {
                            paths[i - 1] = e.Args[i];
                        }
                        // FileShredderService.Instance.ShredFiles(paths);
                        // 粉碎完成后退出应用
                        this.Shutdown();
                        return;
                    }
                }
                
                // 直接创建并显示主窗口
                MainWindow mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动错误: {ex.Message}\n{ex.StackTrace}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                this.Shutdown();
            }
        }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        System.Windows.MessageBox.Show($"UI错误: {e.Exception.Message}\n{e.Exception.StackTrace}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        e.Handled = true;
        // 不要立即关闭应用程序，让它继续运行
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            System.Windows.MessageBox.Show($"未处理错误: {ex.Message}\n{ex.StackTrace}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        // 不要立即关闭应用程序，让它继续运行
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        // 停止弹窗拦截服务
        // PopupBlockerService.Instance.Stop();
        // 停止文件粉碎服务
        // FileShredderService.Instance.Stop();
    }
}
