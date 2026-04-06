using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using IO = System.IO;

namespace WinShieldOptiPro
{
    public partial class SoftwareUninstaller : UserControl
    {
        private List<SoftwareInfo> thirdPartySoftwareList;
        private List<UWPSoftwareInfo> uwpSoftwareList;
        private List<ResidualInfo> residualList;

        // 系统核心组件保护列表
        private readonly List<string> protectedSystemComponents = new List<string>
        {
            "Microsoft Edge",
            "Windows Defender",
            "Windows Security",
            "Windows Store",
            "File Explorer",
            "Task Manager",
            "Control Panel",
            "Settings",
            "Command Prompt",
            "PowerShell",
            "Notepad",
            "Calculator"
        };

        // 可卸载的Windows自带软件列表
        private readonly List<string> removableUWPSoftware = new List<string>
        {
            "Microsoft Photos",
            "Mail and Calendar",
            "Weather",
            "Xbox",
            "Maps",
            "Sticky Notes",
            "Get Help",
            "Microsoft Solitaire Collection",
            "Mixed Reality Portal",
            "Paint 3D",
            "3D Viewer",
            "OneNote for Windows 10",
            "People",
            "Phone Link",
            "Skype",
            "Your Phone"
        };

        public SoftwareUninstaller()
        {
            InitializeComponent();
            InitializeEventHandlers();
            LoadThirdPartySoftware();
            LoadUWPSoftware();
        }

        private void InitializeEventHandlers()
        {
            // 窗口控制
            // UserControl does not have WindowState or Close() method
            // MinimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
            // CloseButton.Click += (s, e) => Close();

            // 第三方软件
            RefreshThirdPartyButton.Click += (s, e) => LoadThirdPartySoftware();
            SelectAllThirdPartyButton.Click += (s, e) => SelectAllThirdParty();
            UnselectAllThirdPartyButton.Click += (s, e) => UnselectAllThirdParty();
            UninstallThirdPartyButton.Click += (s, e) => UninstallThirdPartySoftware(false);
            SilentUninstallThirdPartyButton.Click += (s, e) => UninstallThirdPartySoftware(true);

            // UWP软件
            RefreshUWPButton.Click += (s, e) => LoadUWPSoftware();
            SelectAllUWPButton.Click += (s, e) => SelectAllUWP();
            UnselectAllUWPButton.Click += (s, e) => UnselectAllUWP();
            UninstallUWPButton.Click += (s, e) => UninstallUWPSoftware();
            RestoreUWPButton.Click += (s, e) => RestoreUWPSoftware();

            // 残留清理
            ScanResidualButton.Click += (s, e) => ScanResiduals();
            CleanResidualButton.Click += (s, e) => CleanSelectedResiduals();
            CleanAllResidualButton.Click += (s, e) => CleanAllResiduals();
        }

        // 第三方软件管理
        private void LoadThirdPartySoftware()
        {
            StatusText.Text = "正在扫描第三方软件...";
            ProgressBar.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                thirdPartySoftwareList = GetInstalledSoftware();
                Dispatcher.Invoke(() =>
                {
                    ThirdPartySoftwareList.ItemsSource = thirdPartySoftwareList;
                    StatusText.Text = $"找到 {thirdPartySoftwareList.Count} 个第三方软件";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private List<SoftwareInfo> GetInstalledSoftware()
        {
            List<SoftwareInfo> softwareList = new List<SoftwareInfo>();

            try
            {
                // 使用WMI查询已安装的软件
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
                ManagementObjectCollection products = searcher.Get();

                foreach (ManagementObject product in products)
                {
                    string name = product["Name"]?.ToString();
                    string version = product["Version"]?.ToString();
                    string publisher = product["Vendor"]?.ToString();
                    string uninstallString = product["UninstallString"]?.ToString();

                    if (!string.IsNullOrEmpty(name))
                    {
                        softwareList.Add(new SoftwareInfo
                        {
                            Name = name,
                            Version = version,
                            Publisher = publisher,
                            UninstallString = uninstallString
                        });
                    }
                }

                // 从注册表获取更多软件信息
                softwareList.AddRange(GetSoftwareFromRegistry("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"));
                softwareList.AddRange(GetSoftwareFromRegistry("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"));

                // 去重
                softwareList = softwareList.GroupBy(s => s.Name).Select(g => g.First()).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取软件列表失败: {ex.Message}");
            }

            return softwareList;
        }

        private List<SoftwareInfo> GetSoftwareFromRegistry(string registryPath)
        {
            List<SoftwareInfo> softwareList = new List<SoftwareInfo>();

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (Microsoft.Win32.RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    string name = subKey.GetValue("DisplayName")?.ToString();
                                    string version = subKey.GetValue("DisplayVersion")?.ToString();
                                    string publisher = subKey.GetValue("Publisher")?.ToString();
                                    string uninstallString = subKey.GetValue("UninstallString")?.ToString();

                                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(uninstallString))
                                    {
                                        softwareList.Add(new SoftwareInfo
                                        {
                                            Name = name,
                                            Version = version,
                                            Publisher = publisher,
                                            UninstallString = uninstallString
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从注册表获取软件列表失败: {ex.Message}");
            }

            return softwareList;
        }

        private void SelectAllThirdParty()
        {
            foreach (SoftwareInfo software in thirdPartySoftwareList)
            {
                software.IsSelected = true;
            }
            ThirdPartySoftwareList.Items.Refresh();
        }

        private void UnselectAllThirdParty()
        {
            foreach (SoftwareInfo software in thirdPartySoftwareList)
            {
                software.IsSelected = false;
            }
            ThirdPartySoftwareList.Items.Refresh();
        }

        private void UninstallThirdPartySoftware(bool silent)
        {
            var selectedSoftware = thirdPartySoftwareList.Where(s => s.IsSelected).ToList();
            if (selectedSoftware.Count == 0)
            {
                MessageBox.Show("请选择要卸载的软件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusText.Text = "正在卸载软件...";
            ProgressBar.Visibility = Visibility.Visible;
            
            // 显示动画
            UninstallAnimationGrid.Visibility = Visibility.Visible;
            StartUninstallAnimation();

            Task.Run(() =>
            {
                // 等待动画完成
                System.Threading.Thread.Sleep(2000);
                
                foreach (var software in selectedSoftware)
                {
                    try
                    {
                        UninstallSoftware(software, silent);
                        // 清理残留
                        CleanSoftwareResiduals(software.Name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"卸载 {software.Name} 失败: {ex.Message}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    // 隐藏动画
                    UninstallAnimationGrid.Visibility = Visibility.Collapsed;
                    
                    LoadThirdPartySoftware();
                    StatusText.Text = "卸载完成";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void UninstallSoftware(SoftwareInfo software, bool silent)
        {
            if (string.IsNullOrEmpty(software.UninstallString))
                return;

            string uninstallCommand = software.UninstallString;
            string arguments = "";

            // 处理MSI安装程序
            if (uninstallCommand.Contains("msiexec.exe"))
            {
                if (silent)
                {
                    arguments = "/x " + uninstallCommand.Split(' ').Last() + " /qn /norestart";
                    uninstallCommand = "msiexec.exe";
                }
            }
            // 处理其他安装程序
            else
            {
                if (silent)
                {
                    // 尝试添加静默卸载参数
                    if (!uninstallCommand.Contains("/S") && !uninstallCommand.Contains("/silent"))
                    {
                        arguments = "/S";
                    }
                }
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = uninstallCommand,
                Arguments = arguments,
                UseShellExecute = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        // UWP软件管理
        private void LoadUWPSoftware()
        {
            StatusText.Text = "正在扫描Windows自带软件...";
            ProgressBar.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                uwpSoftwareList = GetUWPApps();
                Dispatcher.Invoke(() =>
                {
                    UWPSoftwareList.ItemsSource = uwpSoftwareList;
                    StatusText.Text = $"找到 {uwpSoftwareList.Count} 个Windows自带软件";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private List<UWPSoftwareInfo> GetUWPApps()
        {
            List<UWPSoftwareInfo> apps = new List<UWPSoftwareInfo>();

            try
            {
                // 使用PowerShell获取UWP应用
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "Get-AppxPackage | Select-Object Name, Publisher, PackageFullName",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                using (StreamReader reader = process.StandardOutput)
                {
                    string output = reader.ReadToEnd();
                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 3; i < lines.Length; i++) // 跳过标题行
                    {
                        string line = lines[i].Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            // 解析PowerShell输出
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                string name = parts[0];
                                string publisher = parts[1];
                                string packageFullName = string.Join(" ", parts.Skip(2));

                                // 转换包名为可读名称
                                string displayName = GetUWPAppDisplayName(name);

                                bool isProtected = protectedSystemComponents.Contains(displayName);
                                bool isRemovable = removableUWPSoftware.Contains(displayName);

                                apps.Add(new UWPSoftwareInfo
                                {
                                    Name = displayName,
                                    Publisher = publisher,
                                    PackageFullName = packageFullName,
                                    IsProtected = isProtected,
                                    IsRemovable = isRemovable
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取UWP应用列表失败: {ex.Message}");
            }

            return apps;
        }

        private string GetUWPAppDisplayName(string packageName)
        {
            // 映射包名到可读名称
            Dictionary<string, string> appNameMap = new Dictionary<string, string>
            {
                { "Microsoft.Windows.Photos", "Microsoft Photos" },
                { "Microsoft.windowscommunicationsapps", "Mail and Calendar" },
                { "Microsoft.BingWeather", "Weather" },
                { "Microsoft.Xbox", "Xbox" },
                { "Microsoft.WindowsMaps", "Maps" },
                { "Microsoft.MicrosoftStickyNotes", "Sticky Notes" },
                { "Microsoft.GetHelp", "Get Help" },
                { "Microsoft.MicrosoftSolitaireCollection", "Microsoft Solitaire Collection" },
                { "Microsoft.MixedReality.Portal", "Mixed Reality Portal" },
                { "Microsoft.Paint3D", "Paint 3D" },
                { "Microsoft.Microsoft3DViewer", "3D Viewer" },
                { "Microsoft.Office.OneNote", "OneNote for Windows 10" },
                { "Microsoft.People", "People" },
                { "Microsoft.YourPhone", "Phone Link" },
                { "Microsoft.SkypeApp", "Skype" }
            };

            return appNameMap.TryGetValue(packageName, out string displayName) ? displayName : packageName;
        }

        private void SelectAllUWP()
        {
            foreach (UWPSoftwareInfo app in uwpSoftwareList)
            {
                if (app.IsRemovable && !app.IsProtected)
                {
                    app.IsSelected = true;
                }
            }
            UWPSoftwareList.Items.Refresh();
        }

        private void UnselectAllUWP()
        {
            foreach (UWPSoftwareInfo app in uwpSoftwareList)
            {
                app.IsSelected = false;
            }
            UWPSoftwareList.Items.Refresh();
        }

        private void UninstallUWPSoftware()
        {
            var selectedApps = uwpSoftwareList.Where(a => a.IsSelected && a.IsRemovable && !a.IsProtected).ToList();
            if (selectedApps.Count == 0)
            {
                MessageBox.Show("请选择要卸载的Windows自带软件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusText.Text = "正在卸载Windows自带软件...";
            ProgressBar.Visibility = Visibility.Visible;
            
            // 显示动画
            UninstallAnimationGrid.Visibility = Visibility.Visible;
            StartUninstallAnimation();

            Task.Run(() =>
            {
                // 等待动画完成
                System.Threading.Thread.Sleep(2000);
                
                foreach (var app in selectedApps)
                {
                    try
                    {
                        UninstallUWPApp(app);
                        // 清理残留
                        CleanSoftwareResiduals(app.Name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"卸载 {app.Name} 失败: {ex.Message}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    // 隐藏动画
                    UninstallAnimationGrid.Visibility = Visibility.Collapsed;
                    
                    LoadUWPSoftware();
                    StatusText.Text = "卸载完成";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void UninstallUWPApp(UWPSoftwareInfo app)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"Remove-AppxPackage -Package {app.PackageFullName}",
                UseShellExecute = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        private void RestoreUWPSoftware()
        {
            // 打开Microsoft Store让用户手动恢复
            Process.Start("ms-windows-store:");
        }

        // 残留清理
        private void ScanResiduals()
        {
            StatusText.Text = "正在扫描软件残留...";
            ProgressBar.Visibility = Visibility.Visible;

            Task.Run(() =>
            {
                residualList = new List<ResidualInfo>();

                // 扫描AppData目录
                ScanAppDataResiduals();
                // 扫描注册表残留
                ScanRegistryResiduals();
                // 扫描无效快捷方式
                ScanInvalidShortcuts();
                // 扫描废弃系统服务
                ScanOrphanedServices();

                Dispatcher.Invoke(() =>
                {
                    ResidualList.ItemsSource = residualList;
                    StatusText.Text = $"找到 {residualList.Count} 项残留";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void ScanAppDataResiduals()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string localLowAppDataPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");

            // 扫描AppData目录
            ScanDirectoryForResiduals(appDataPath);
            ScanDirectoryForResiduals(localAppDataPath);
            ScanDirectoryForResiduals(localLowAppDataPath);
        }

        private void ScanDirectoryForResiduals(string directoryPath)
        {
            if (!IO.Directory.Exists(directoryPath))
                return;

            try
            {
                foreach (string subDir in IO.Directory.GetDirectories(directoryPath))
                {
                    // 检查是否为已卸载软件的残留目录
                    if (IsOrphanedDirectory(subDir))
                    {
                        long size = GetDirectorySize(subDir);
                        residualList.Add(new ResidualInfo
                        {
                            Path = subDir,
                            Size = FormatSize(size)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描目录 {directoryPath} 失败: {ex.Message}");
            }
        }

        private bool IsOrphanedDirectory(string directoryPath)
        {
            // 简单判断：检查目录是否对应已安装的软件
            string dirName = IO.Path.GetFileName(directoryPath);
            return !thirdPartySoftwareList.Any(s => s.Name.Contains(dirName, StringComparison.OrdinalIgnoreCase));
        }

        private long GetDirectorySize(string directoryPath)
        {
            long size = 0;
            try
            {
                foreach (string file in IO.Directory.GetFiles(directoryPath, "*.*", IO.SearchOption.AllDirectories))
                {
                    if (IO.File.Exists(file))
                    {
                        IO.FileInfo fileInfo = new IO.FileInfo(file);
                        size += fileInfo.Length;
                    }
                }
            }
            catch { }
            return size;
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{(bytes / (1024 * 1024 * 1024.0)):F2} GB";
            else if (bytes >= 1024 * 1024)
                return $"{(bytes / (1024 * 1024.0)):F2} MB";
            else if (bytes >= 1024)
                return $"{(bytes / 1024.0):F2} KB";
            else
                return $"{bytes} B";
        }

        private void ScanRegistryResiduals()
        {
            // 扫描注册表中的无效卸载项
            string[] registryPaths = {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };

            foreach (string path in registryPaths)
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (Microsoft.Win32.RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey != null)
                                    {
                                        string displayName = subKey.GetValue("DisplayName")?.ToString();
                                        string uninstallString = subKey.GetValue("UninstallString")?.ToString();

                                        if (!string.IsNullOrEmpty(displayName) && (!string.IsNullOrEmpty(uninstallString) || IsRegistryEntryOrphaned(displayName)))
                                        {
                                            residualList.Add(new ResidualInfo
                                            {
                                                Path = $"Registry: {path}\\{subKeyName}",
                                                Size = "0 B"
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫描注册表 {path} 失败: {ex.Message}");
                }
            }
        }

        private bool IsRegistryEntryOrphaned(string displayName)
        {
            // 检查注册表项是否对应已安装的软件
            return !thirdPartySoftwareList.Any(s => s.Name == displayName);
        }

        private void ScanInvalidShortcuts()
        {
            // 扫描桌面快捷方式
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            ScanShortcutsInDirectory(desktopPath);

            // 扫描开始菜单快捷方式
            string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            ScanShortcutsInDirectory(startMenuPath);

            // 扫描快速启动栏
            string quickLaunchPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Internet Explorer", "Quick Launch");
            ScanShortcutsInDirectory(quickLaunchPath);
        }

        private void ScanShortcutsInDirectory(string directoryPath)
        {
            if (!IO.Directory.Exists(directoryPath))
                return;

            try
            {
                foreach (string file in IO.Directory.GetFiles(directoryPath, "*.lnk", IO.SearchOption.AllDirectories))
                {
                    if (IsInvalidShortcut(file))
                    {
                        residualList.Add(new ResidualInfo
                        {
                            Path = file,
                            Size = new IO.FileInfo(file).Length.ToString() + " B"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描快捷方式 {directoryPath} 失败: {ex.Message}");
            }
        }

        private bool IsInvalidShortcut(string shortcutPath)
        {
            try
            {
                // 读取快捷方式目标
                // 这里简化处理，实际需要使用COM接口读取快捷方式目标
                return true; // 暂时全部标记为无效，实际需要更复杂的判断
            }
            catch
            {
                return true;
            }
        }

        private void ScanOrphanedServices()
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service");
                ManagementObjectCollection services = searcher.Get();

                foreach (ManagementObject service in services)
                {
                    string serviceName = service["Name"]?.ToString();
                    string displayName = service["DisplayName"]?.ToString();
                    string pathName = service["PathName"]?.ToString();

                    if (!string.IsNullOrEmpty(serviceName) && !string.IsNullOrEmpty(pathName))
                    {
                        // 检查服务可执行文件是否存在
                        string executablePath = pathName.Trim('"');
                        if (!File.Exists(executablePath))
                        {
                            residualList.Add(new ResidualInfo
                            {
                                Path = $"Service: {displayName} ({serviceName})",
                                Size = "0 B"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"扫描废弃服务失败: {ex.Message}");
            }
        }

        private void CleanSelectedResiduals()
        {
            var selectedResiduals = residualList.Where(r => r.IsSelected).ToList();
            if (selectedResiduals.Count == 0)
            {
                MessageBox.Show("请选择要清理的残留", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusText.Text = "正在清理残留...";
            ProgressBar.Visibility = Visibility.Visible;
            
            // 显示动画
            UninstallAnimationGrid.Visibility = Visibility.Visible;
            StartUninstallAnimation();

            Task.Run(() =>
            {
                // 等待动画完成
                System.Threading.Thread.Sleep(2000);
                
                foreach (var residual in selectedResiduals)
                {
                    try
                    {
                        CleanResidual(residual);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"清理残留 {residual.Path} 失败: {ex.Message}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    // 隐藏动画
                    UninstallAnimationGrid.Visibility = Visibility.Collapsed;
                    
                    ScanResiduals();
                    StatusText.Text = "清理完成";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void CleanAllResiduals()
        {
            if (residualList.Count == 0)
            {
                MessageBox.Show("没有残留需要清理", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("确定要清理所有残留吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                return;

            StatusText.Text = "正在清理所有残留...";
            ProgressBar.Visibility = Visibility.Visible;
            
            // 显示动画
            UninstallAnimationGrid.Visibility = Visibility.Visible;
            StartUninstallAnimation();

            Task.Run(() =>
            {
                // 等待动画完成
                System.Threading.Thread.Sleep(2000);
                
                foreach (var residual in residualList)
                {
                    try
                    {
                        CleanResidual(residual);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"清理残留 {residual.Path} 失败: {ex.Message}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    // 隐藏动画
                    UninstallAnimationGrid.Visibility = Visibility.Collapsed;
                    
                    ScanResiduals();
                    StatusText.Text = "清理完成";
                    ProgressBar.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void CleanResidual(ResidualInfo residual)
        {
            if (residual.Path.StartsWith("Registry:"))
            {
                // 清理注册表项
                string registryPath = residual.Path.Substring(9); // 移除 "Registry: " 前缀
                try
                {
                    string[] parts = registryPath.Split('\\');
                    string keyPath = string.Join("\\", parts.Take(parts.Length - 1));
                    string subKeyName = parts.Last();

                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath, true))
                    {
                        if (key != null)
                        {
                            key.DeleteSubKey(subKeyName, false);
                        }
                    }
                }
                catch { }
            }
            else if (residual.Path.StartsWith("Service:"))
            {
                // 清理废弃服务
                string serviceName = residual.Path.Substring(8).Split('(').Last().Trim(')');
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"delete {serviceName}",
                        UseShellExecute = true
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                    }
                }
                catch { }
            }
            else if (IO.File.Exists(residual.Path))
            {
                // 清理文件
                try
                {
                    IO.File.Delete(residual.Path);
                }
                catch { }
            }
            else if (IO.Directory.Exists(residual.Path))
            {
                // 清理目录
                try
                {
                    IO.Directory.Delete(residual.Path, true);
                }
                catch { }
            }
        }

        private void CleanSoftwareResiduals(string softwareName)
        {
            // 清理软件残留
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 清理AppData中的残留目录
            CleanAppDataResiduals(appDataPath, softwareName);
            CleanAppDataResiduals(localAppDataPath, softwareName);

            // 清理注册表残留
            CleanRegistryResiduals(softwareName);

            // 清理快捷方式残留
            CleanShortcutResiduals(softwareName);
        }

        private void CleanAppDataResiduals(string basePath, string softwareName)
        {
            if (!IO.Directory.Exists(basePath))
                return;

            try
            {
                foreach (string subDir in IO.Directory.GetDirectories(basePath))
                {
                    string dirName = IO.Path.GetFileName(subDir);
                    if (dirName.Contains(softwareName, StringComparison.OrdinalIgnoreCase))
                    {
                        IO.Directory.Delete(subDir, true);
                    }
                }
            }
            catch { }
        }

        private void CleanRegistryResiduals(string softwareName)
        {
            string[] registryPaths = {
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
            };

            foreach (string path in registryPaths)
            {
                try
                {
                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path, true))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (Microsoft.Win32.RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey != null)
                                    {
                                        string displayName = subKey.GetValue("DisplayName")?.ToString();
                                        if (displayName == softwareName)
                                        {
                                            key.DeleteSubKey(subKeyName, false);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void CleanShortcutResiduals(string softwareName)
        {
            string[] shortcutPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Internet Explorer", "Quick Launch")
            };

            foreach (string path in shortcutPaths)
            {
                if (!IO.Directory.Exists(path))
                    continue;

                try
                {
                    foreach (string file in IO.Directory.GetFiles(path, "*.lnk", IO.SearchOption.AllDirectories))
                    {
                        if (IO.Path.GetFileName(file).Contains(softwareName, StringComparison.OrdinalIgnoreCase))
                        {
                            IO.File.Delete(file);
                        }
                    }
                }
                catch { }
            }
        }

        private void StartUninstallAnimation()
        {
            // 清空画布
            UninstallAnimationCanvas.Children.Clear();
            
            Random random = new Random();
            
            // 创建多个软件图标动画
            for (int i = 0; i < 15; i++)
            {
                // 创建软件图标
                Rectangle appIcon = new Rectangle
                {
                    Width = 50,
                    Height = 50,
                    Fill = new LinearGradientBrush(Colors.Green, Colors.DarkGreen, 0),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                
                // 创建软件标签
                Rectangle appLabel = new Rectangle
                {
                    Width = 50,
                    Height = 12,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                
                // 设置初始位置
                double startX = random.Next(0, (int)UninstallAnimationCanvas.Width - 50);
                double startY = random.Next(0, (int)UninstallAnimationCanvas.Height - 62);
                
                Canvas.SetLeft(appIcon, startX);
                Canvas.SetTop(appIcon, startY);
                Canvas.SetLeft(appLabel, startX);
                Canvas.SetTop(appLabel, startY + 50);
                
                UninstallAnimationCanvas.Children.Add(appIcon);
                UninstallAnimationCanvas.Children.Add(appLabel);
                
                // 创建动画
                DoubleAnimation translateAnimationX = new DoubleAnimation
                {
                    From = startX,
                    To = startX + (random.NextDouble() - 0.5) * 200,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation translateAnimationY = new DoubleAnimation
                {
                    From = startY,
                    To = startY - 300,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation rotateAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation scaleAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0.2,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                // 应用动画
                appIcon.BeginAnimation(Canvas.LeftProperty, translateAnimationX);
                appIcon.BeginAnimation(Canvas.TopProperty, translateAnimationY);
                appIcon.BeginAnimation(OpacityProperty, opacityAnimation);
                
                RotateTransform rotateTransform = new RotateTransform();
                appIcon.RenderTransform = rotateTransform;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                
                ScaleTransform scaleTransform = new ScaleTransform();
                appIcon.RenderTransform = new TransformGroup
                {
                    Children = { rotateTransform, scaleTransform }
                };
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                
                // 为标签应用相同的动画
                appLabel.BeginAnimation(Canvas.LeftProperty, translateAnimationX);
                appLabel.BeginAnimation(Canvas.TopProperty, translateAnimationY);
                appLabel.BeginAnimation(OpacityProperty, opacityAnimation);
                appLabel.RenderTransform = new TransformGroup
                {
                    Children = { new RotateTransform(), new ScaleTransform() }
                };
                ((RotateTransform)((TransformGroup)appLabel.RenderTransform).Children[0]).BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                ((ScaleTransform)((TransformGroup)appLabel.RenderTransform).Children[1]).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                ((ScaleTransform)((TransformGroup)appLabel.RenderTransform).Children[1]).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            
            // 添加粒子效果
            for (int i = 0; i < 40; i++)
            {
                Ellipse particle = new Ellipse
                {
                    Width = random.Next(2, 5),
                    Height = random.Next(2, 5),
                    Fill = new SolidColorBrush(Color.FromArgb((byte)random.Next(100, 255), 0, 191, 255))
                };
                
                double startX = UninstallAnimationCanvas.Width / 2;
                double startY = UninstallAnimationCanvas.Height / 2;
                
                Canvas.SetLeft(particle, startX);
                Canvas.SetTop(particle, startY);
                
                UninstallAnimationCanvas.Children.Add(particle);
                
                // 创建粒子动画
                DoubleAnimation particleAnimationX = new DoubleAnimation
                {
                    From = startX,
                    To = startX + (random.NextDouble() - 0.5) * 400,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation particleAnimationY = new DoubleAnimation
                {
                    From = startY,
                    To = startY + (random.NextDouble() - 0.5) * 400,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                DoubleAnimation particleOpacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(2)
                };
                
                particle.BeginAnimation(Canvas.LeftProperty, particleAnimationX);
                particle.BeginAnimation(Canvas.TopProperty, particleAnimationY);
                particle.BeginAnimation(OpacityProperty, particleOpacityAnimation);
            }
        }
    }

    public class SoftwareInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }
        public string UninstallString { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class UWPSoftwareInfo : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string PackageFullName { get; set; }
        public bool IsProtected { get; set; }
        public bool IsRemovable { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ResidualInfo : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Size { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}