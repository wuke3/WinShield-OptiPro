using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinShieldOptiPro;

public partial class DiskSpaceAnalyzer : UserControl
{
    private CancellationTokenSource cancellationTokenSource;
    private Dictionary<string, long> directorySizes;
    private List<HighRiskItem> highRiskItems;

    public DiskSpaceAnalyzer()
    {
        InitializeComponent();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        // 开始扫描
        ScanButton.IsEnabled = false;
        ScanStatusText.Text = "正在扫描C盘...";
        ScanProgressBar.Visibility = Visibility.Visible;

        cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // 异步扫描C盘
            await Task.Run(() =>
            {
                directorySizes = new Dictionary<string, long>();
                highRiskItems = new List<HighRiskItem>();

                // 扫描C盘根目录
                ScanDirectory("C:\\", cancellationTokenSource.Token);

                // 扫描AppData目录
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (Directory.Exists(appDataPath))
                {
                    ScanDirectory(appDataPath, cancellationTokenSource.Token);
                }

                // 扫描ProgramData目录
                string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                if (Directory.Exists(programDataPath))
                {
                    ScanDirectory(programDataPath, cancellationTokenSource.Token);
                }

                // 检查高危占用项
                CheckHighRiskItems();
            }, cancellationTokenSource.Token);

            // 更新UI
            UpdateUI();
        }
        catch (OperationCanceledException)
        {
            ScanStatusText.Text = "扫描已取消";
        }
        catch (Exception ex)
        {
            ScanStatusText.Text = "扫描失败: " + ex.Message;
        }
        finally
        {
            ScanButton.IsEnabled = true;
            ScanProgressBar.Visibility = Visibility.Collapsed;
            ScanStatusText.Text = "扫描完成";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        // 刷新扫描
        ScanButton_Click(sender, e);
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        // 实现文件类型筛选功能
        string selectedFileType = (FileTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        // 这里可以根据选中的文件类型进行筛选
        ScanStatusText.Text = $"已筛选 {selectedFileType} 文件";
    }

    private void ScanDirectory(string path, CancellationToken token)
    {
        try
        {
            // 检查是否取消操作
            token.ThrowIfCancellationRequested();

            // 获取目录大小
            long size = CalculateDirectorySize(path, token);
            directorySizes[path] = size;

            // 递归扫描子目录
            foreach (string subDir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    ScanDirectory(subDir, token);
                }
                catch (Exception)
                {
                    // 跳过无权限的目录
                }
            }
        }
        catch (Exception)
        {
            // 跳过无权限的目录
        }
    }

    private long CalculateDirectorySize(string path, CancellationToken token)
    {
        long size = 0;
        try
        {
            // 检查是否取消操作
            token.ThrowIfCancellationRequested();

            // 获取文件大小
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(file);
                    size += fileInfo.Length;
                }
                catch (Exception)
                {
                    // 跳过无权限的文件
                }
            }

            // 递归计算子目录大小
            foreach (string subDir in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    size += CalculateDirectorySize(subDir, token);
                }
                catch (Exception)
                {
                    // 跳过无权限的目录
                }
            }
        }
        catch (Exception)
        {
            // 跳过无权限的目录
        }
        return size;
    }

    private void CheckHighRiskItems()
    {
        // 检查休眠文件
        string hiberfilPath = "C:\\hiberfil.sys";
        if (File.Exists(hiberfilPath))
        {
            try
            {
                FileInfo hiberfilInfo = new FileInfo(hiberfilPath);
                highRiskItems.Add(new HighRiskItem("休眠文件 (hiberfil.sys)", hiberfilInfo.Length, "系统休眠时创建的文件，可通过关闭休眠功能释放空间"));
            }
            catch (Exception)
            {
            }
        }

        // 检查虚拟内存文件
        string pagefilePath = "C:\\pagefile.sys";
        if (File.Exists(pagefilePath))
        {
            try
            {
                FileInfo pagefileInfo = new FileInfo(pagefilePath);
                highRiskItems.Add(new HighRiskItem("虚拟内存文件 (pagefile.sys)", pagefileInfo.Length, "系统虚拟内存文件，可通过调整虚拟内存设置释放空间"));
            }
            catch (Exception)
            {
            }
        }

        // 检查WinSxS目录
        string winsxsPath = "C:\\Windows\\WinSxS";
        if (Directory.Exists(winsxsPath))
        {
            try
            {
                long winsxsSize = CalculateDirectorySize(winsxsPath, CancellationToken.None);
                highRiskItems.Add(new HighRiskItem("WinSxS目录", winsxsSize, "系统组件存储目录，可通过系统清理工具释放空间"));
            }
            catch (Exception)
            {
            }
        }

        // 检查微信缓存
        string wechatCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "Tencent", "WeChat", "WeChat Files");
        if (Directory.Exists(wechatCachePath))
        {
            try
            {
                long wechatSize = CalculateDirectorySize(wechatCachePath, CancellationToken.None);
                highRiskItems.Add(new HighRiskItem("微信缓存", wechatSize, "微信聊天记录和缓存文件，可通过微信设置清理"));
            }
            catch (Exception)
            {
            }
        }

        // 检查QQ缓存
        string qqCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "Tencent", "QQ");
        if (Directory.Exists(qqCachePath))
        {
            try
            {
                long qqSize = CalculateDirectorySize(qqCachePath, CancellationToken.None);
                highRiskItems.Add(new HighRiskItem("QQ缓存", qqSize, "QQ聊天记录和缓存文件，可通过QQ设置清理"));
            }
            catch (Exception)
            {
            }
        }
    }

    private void UpdateUI()
    {
        // 更新磁盘使用列表
        UpdateDiskUsageList();

        // 更新树形目录
        UpdateDirectoryTree();

        // 更新高危占用项
        UpdateHighRiskItems();
    }

    private void UpdateDiskUsageList()
    {
        // 清空列表
        DiskUsageListBox.Items.Clear();

        // 按大小排序目录
        var sortedDirectories = directorySizes.OrderByDescending(d => d.Value).Take(10);

        // 计算总大小
        long totalSize = sortedDirectories.Sum(d => d.Value);

        // 添加数据到列表
        foreach (var dir in sortedDirectories)
        {
            string directoryName = new DirectoryInfo(dir.Key).Name;
            double percentage = totalSize > 0 ? (double)dir.Value / totalSize * 100 : 0;
            DiskUsageListBox.Items.Add(new DiskUsageItem(directoryName, dir.Value, percentage));
        }
    }

    private void UpdateDirectoryTree()
    {
        // 清空树形目录
        DirectoryTreeView.Items.Clear();

        // 按大小排序目录
        var sortedDirectories = directorySizes.OrderByDescending(d => d.Value);

        // 创建根目录项
        string rootPath = @"C:";
        var rootDir = sortedDirectories.FirstOrDefault(d => d.Key == rootPath);
        long rootSize = rootDir.Key != null ? rootDir.Value : 0;
        DirectoryItem rootItem = new DirectoryItem(@"C:", rootSize, rootPath);

        // 添加子目录
        foreach (var dir in sortedDirectories.Where(d => d.Key != rootPath && d.Key.StartsWith(rootPath) && d.Key.Count(c => c == '\\') == 3))
        {
            DirectoryItem subItem = new DirectoryItem(new DirectoryInfo(dir.Key).Name, dir.Value, dir.Key);
            // 添加更深层的子目录
            AddSubDirectories(subItem, dir.Key);
            rootItem.SubItems.Add(subItem);
        }

        // 添加到树形目录
        DirectoryTreeView.Items.Add(rootItem);
    }

    private void AddSubDirectories(DirectoryItem parentItem, string parentPath)
    {
        // 查找当前目录的子目录
        var subDirectories = directorySizes.Where(d => d.Key.StartsWith(parentPath + "\\") && d.Key.Count(c => c == '\\') == parentPath.Count(c => c == '\\') + 1);

        foreach (var dir in subDirectories.OrderByDescending(d => d.Value))
        {
            DirectoryItem subItem = new DirectoryItem(new DirectoryInfo(dir.Key).Name, dir.Value, dir.Key);
            parentItem.SubItems.Add(subItem);
        }
    }

    private void UpdateHighRiskItems()
    {
        // 清空高危占用项列表
        HighRiskItemsListBox.Items.Clear();

        // 添加高危占用项
        foreach (var item in highRiskItems.OrderByDescending(i => i.Size))
        {
            HighRiskItemsListBox.Items.Add(item);
        }
    }
}