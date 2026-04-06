using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Management;

namespace WinShieldOptiPro;

/// <summary>
/// Interaction logic for HardwareOverview.xaml
/// </summary>
public partial class HardwareOverview : UserControl
{
    public HardwareOverview()
    {
        InitializeComponent();
        Loaded += HardwareOverview_Loaded;
    }

    private void HardwareOverview_Loaded(object sender, RoutedEventArgs e)
    {
        // 异步获取硬件信息
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                // 获取CPU信息
                string cpuInfo = GetCPUInfo();
                Dispatcher.Invoke(() => CPUInfoText.Text = cpuInfo);

                // 获取内存信息
                string ramInfo = GetRAMInfo();
                Dispatcher.Invoke(() => RAMInfoText.Text = ramInfo);

                // 获取显卡信息
                string gpuInfo = GetGPUInfo();
                Dispatcher.Invoke(() => GPUInfoText.Text = gpuInfo);

                // 获取主板信息
                string motherboardInfo = GetMotherboardInfo();
                Dispatcher.Invoke(() => MotherboardInfoText.Text = motherboardInfo);

                // 获取声卡和网卡信息
                string audioNetworkInfo = GetAudioNetworkInfo();
                Dispatcher.Invoke(() => AudioNetworkInfoText.Text = audioNetworkInfo);

                // 获取显示器信息
                string monitorInfo = GetMonitorInfo();
                Dispatcher.Invoke(() => MonitorInfoText.Text = monitorInfo);

                // 获取硬盘信息
                string diskInfo = GetDiskInfo();
                Dispatcher.Invoke(() => DiskInfoText.Text = diskInfo);

                // 获取系统信息
                string systemInfo = GetSystemInfo();
                Dispatcher.Invoke(() => SystemInfoText.Text = systemInfo);
            }
            catch (Exception ex)
            {
                // 处理异常
                Dispatcher.Invoke(() =>
                {
                    CPUInfoText.Text = "获取失败";
                    RAMInfoText.Text = "获取失败";
                    GPUInfoText.Text = "获取失败";
                    MotherboardInfoText.Text = "获取失败";
                    AudioNetworkInfoText.Text = "获取失败";
                    MonitorInfoText.Text = "获取失败";
                    DiskInfoText.Text = "获取失败";
                    SystemInfoText.Text = "获取失败";
                });
            }
        });
    }

    // 获取CPU信息
    private string GetCPUInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.AppendLine($"处理器: {obj["Name"]}");
                info.AppendLine($"核心数: {obj["NumberOfCores"]}");
                info.AppendLine($"线程数: {obj["NumberOfLogicalProcessors"]}");
                info.AppendLine($"制造商: {obj["Manufacturer"]}");
                info.AppendLine($"处理器ID: {obj["ProcessorId"]}");
                break;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取内存信息
    private string GetRAMInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                ulong totalPhysicalMemory = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                double totalGB = totalPhysicalMemory / (1024.0 * 1024.0 * 1024.0);
                info.AppendLine($"总内存: {Math.Round(totalGB, 2)} GB");
                break;
            }

            // 获取内存模块信息
            searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            int slot = 1;
            foreach (ManagementObject obj in searcher.Get())
            {
                ulong capacity = Convert.ToUInt64(obj["Capacity"]);
                double capacityGB = capacity / (1024.0 * 1024.0 * 1024.0);
                info.AppendLine($"插槽 {slot}: {Math.Round(capacityGB, 2)} GB");
                info.AppendLine($"  制造商: {obj["Manufacturer"]}");
                info.AppendLine($"  型号: {obj["PartNumber"]}");
                info.AppendLine($"  速度: {obj["Speed"]} MHz");
                slot++;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取显卡信息
    private string GetGPUInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            int gpuIndex = 1;
            foreach (ManagementObject obj in searcher.Get())
            {
                info.AppendLine($"显卡 {gpuIndex}:");
                info.AppendLine($"  名称: {obj["Name"]}");
                info.AppendLine($"  制造商: {obj["Manufacturer"]}");
                info.AppendLine($"  显存: {Convert.ToInt64(obj["AdapterRAM"]) / (1024 * 1024 * 1024)} GB");
                info.AppendLine($"  驱动版本: {obj["DriverVersion"]}");
                gpuIndex++;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取主板信息
    private string GetMotherboardInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                info.AppendLine($"制造商: {obj["Manufacturer"]}");
                info.AppendLine($"产品: {obj["Product"]}");
                info.AppendLine($"版本: {obj["Version"]}");
                info.AppendLine($"序列号: {obj["SerialNumber"]}");
                break;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取声卡和网卡信息
    private string GetAudioNetworkInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            // 获取声卡信息
            info.AppendLine("声卡:");
            ManagementObjectSearcher audioSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");
            foreach (ManagementObject obj in audioSearcher.Get())
            {
                info.AppendLine($"  {obj["Name"]}");
            }

            // 获取网卡信息
            info.AppendLine("\n网卡:");
            ManagementObjectSearcher networkSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True");
            foreach (ManagementObject obj in networkSearcher.Get())
            {
                info.AppendLine($"  {obj["Name"]}");
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取显示器信息
    private string GetMonitorInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
            int monitorIndex = 1;
            foreach (ManagementObject obj in searcher.Get())
            {
                info.AppendLine($"显示器 {monitorIndex}:");
                info.AppendLine($"  型号: {obj["MonitorManufacturer"]} {obj["MonitorType"]}");
                info.AppendLine($"  分辨率: {obj["ScreenWidth"]}x{obj["ScreenHeight"]}");
                monitorIndex++;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取硬盘信息
    private string GetDiskInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            int diskIndex = 1;
            foreach (ManagementObject obj in searcher.Get())
            {
                info.AppendLine($"硬盘 {diskIndex}:");
                info.AppendLine($"  型号: {obj["Model"]}");
                info.AppendLine($"  接口: {obj["InterfaceType"]}");
                ulong size = Convert.ToUInt64(obj["Size"]);
                double sizeGB = size / (1024.0 * 1024.0 * 1024.0);
                info.AppendLine($"  容量: {Math.Round(sizeGB, 2)} GB");
                
                // 获取硬盘读写参数
                try
                {
                    string deviceId = obj["DeviceID"].ToString();
                    ManagementObjectSearcher perfSearcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name LIKE '{deviceId.Replace("\\\\.", "")}%'");
                    foreach (ManagementObject perfObj in perfSearcher.Get())
                    {
                        info.AppendLine($"  读取速度: {perfObj["DiskReadBytesPerSec"]} 字节/秒");
                        info.AppendLine($"  写入速度: {perfObj["DiskWriteBytesPerSec"]} 字节/秒");
                        break;
                    }
                }
                catch { }
                
                info.AppendLine($"  序列号: {obj["SerialNumber"]}");
                diskIndex++;
            }
        }
        catch { }
        return info.ToString();
    }

    // 获取系统信息
    private string GetSystemInfo()
    {
        StringBuilder info = new StringBuilder();
        try
        {
            // 获取操作系统信息
            ManagementObjectSearcher osSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in osSearcher.Get())
            {
                info.AppendLine($"Windows版本: {obj["Caption"]}");
                info.AppendLine($"系统位数: {(Environment.Is64BitOperatingSystem ? "64位" : "32位")}");
                info.AppendLine($"系统启动时间: {ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString())}");
                break;
            }

            // 获取BIOS信息
            ManagementObjectSearcher biosSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                info.AppendLine($"BIOS版本: {obj["SMBIOSBIOSVersion"]}");
                info.AppendLine($"BIOS制造商: {obj["Manufacturer"]}");
                break;
            }

            // 检查系统激活状态
            try
            {
                var licenseStatus = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", "LicenseStatus", 0);
                info.AppendLine($"激活状态: {(licenseStatus != null && (int)licenseStatus == 1 ? "已激活" : "未激活")}");
            }
            catch { }
        }
        catch { }
        return info.ToString();
    }

    // 一键复制功能
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StringBuilder allInfo = new StringBuilder();
            allInfo.AppendLine("=== 核心硬件信息 ===");
            allInfo.AppendLine("处理器 (CPU):");
            allInfo.AppendLine(CPUInfoText.Text);
            allInfo.AppendLine("\n内存条 (RAM):");
            allInfo.AppendLine(RAMInfoText.Text);
            allInfo.AppendLine("\n显卡 (GPU):");
            allInfo.AppendLine(GPUInfoText.Text);
            allInfo.AppendLine("\n主板:");
            allInfo.AppendLine(MotherboardInfoText.Text);
            allInfo.AppendLine("\n声卡 & 网卡:");
            allInfo.AppendLine(AudioNetworkInfoText.Text);
            allInfo.AppendLine("\n=== 外设硬件信息 ===");
            allInfo.AppendLine("显示器:");
            allInfo.AppendLine(MonitorInfoText.Text);
            allInfo.AppendLine("\n硬盘:");
            allInfo.AppendLine(DiskInfoText.Text);
            allInfo.AppendLine("\n=== 系统信息 ===");
            allInfo.AppendLine(SystemInfoText.Text);

            Clipboard.SetText(allInfo.ToString());
            MessageBox.Show("硬件信息已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("复制失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 导出文本功能
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StringBuilder allInfo = new StringBuilder();
            allInfo.AppendLine("=== WinShield OptiPro - 硬件配置报告 ===");
            allInfo.AppendLine($"生成时间: {DateTime.Now.ToString()}");
            allInfo.AppendLine();
            allInfo.AppendLine("=== 核心硬件信息 ===");
            allInfo.AppendLine("处理器 (CPU):");
            allInfo.AppendLine(CPUInfoText.Text);
            allInfo.AppendLine("\n内存条 (RAM):");
            allInfo.AppendLine(RAMInfoText.Text);
            allInfo.AppendLine("\n显卡 (GPU):");
            allInfo.AppendLine(GPUInfoText.Text);
            allInfo.AppendLine("\n主板:");
            allInfo.AppendLine(MotherboardInfoText.Text);
            allInfo.AppendLine("\n声卡 & 网卡:");
            allInfo.AppendLine(AudioNetworkInfoText.Text);
            allInfo.AppendLine("\n=== 外设硬件信息 ===");
            allInfo.AppendLine("显示器:");
            allInfo.AppendLine(MonitorInfoText.Text);
            allInfo.AppendLine("\n硬盘:");
            allInfo.AppendLine(DiskInfoText.Text);
            allInfo.AppendLine("\n=== 系统信息 ===");
            allInfo.AppendLine(SystemInfoText.Text);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, $"硬件配置报告_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt");

            File.WriteAllText(filePath, allInfo.ToString(), Encoding.UTF8);
            MessageBox.Show($"硬件信息已导出到: {filePath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}