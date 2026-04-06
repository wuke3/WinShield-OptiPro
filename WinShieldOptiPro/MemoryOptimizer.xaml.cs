using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinShieldOptiPro
{
    public partial class MemoryOptimizer : UserControl
    {
        private PerformanceCounter memoryCounter = null!;
        private DispatcherTimer updateTimer = null!;
        private CancellationTokenSource? backgroundOptimizeCts;
        private bool isAutoOptimizeEnabled;
        private long totalMemory;
        private long beforeOptimizeMemory;
        
        // 进程信息类
        public class ProcessInfo
        {
            public string ProcessName { get; set; } = string.Empty;
            public string MemoryUsage { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
        
        // Windows API 导入
        [DllImport("psapi.dll")]        
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPerformanceInfo([Out] out PerformanceInformation performanceInformation, [In] int size);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, int minimumWorkingSetSize, int maximumWorkingSetSize);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct PerformanceInformation
        {
            public int Size;
            public IntPtr CommitTotal;
            public IntPtr CommitLimit;
            public IntPtr CommitPeak;
            public IntPtr PhysicalTotal;
            public IntPtr PhysicalAvailable;
            public IntPtr SystemCache;
            public IntPtr KernelTotal;
            public IntPtr KernelPaged;
            public IntPtr KernelNonPaged;
            public IntPtr PageSize;
            public int HandlesCount;
            public int ProcessCount;
            public int ThreadCount;
        }
        
        public MemoryOptimizer()
        {
            InitializeComponent();
            InitializeMemoryCounter();
            InitializeTimer();
            InitializeEventHandlers();
            UpdateMemoryStatus();
            UpdateProcessList();
        }
        
        private void InitializeMemoryCounter()
        {
            memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        
        // 释放资源
        public void Dispose()
        {
            if (memoryCounter != null)
            {
                memoryCounter.Dispose();
            }
            if (updateTimer != null)
            {
                updateTimer.Stop();
            }
            if (backgroundOptimizeCts != null)
            {
                backgroundOptimizeCts.Cancel();
                backgroundOptimizeCts.Dispose();
            }
        }
        
        private void InitializeTimer()
        {
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += (sender, e) => UpdateMemoryStatus();
            updateTimer.Start();
        }
        
        private void InitializeEventHandlers()
        {
            OptimizeButton.Click += (sender, e) => OptimizeMemory();
            BackgroundOptimizeButton.Click += (sender, e) => ToggleBackgroundOptimization();
            ProcessOptimizeButton.Click += (sender, e) => OptimizeProcesses();
            AutoOptimizeToggle.Checked += (sender, e) => EnableAutoOptimize();
            AutoOptimizeToggle.Unchecked += (sender, e) => DisableAutoOptimize();
        }
        
        private void UpdateMemoryStatus()
        {
            try
            {
                PerformanceInformation pi = new PerformanceInformation();
                if (GetPerformanceInfo(out pi, Marshal.SizeOf(typeof(PerformanceInformation))))
                {
                    totalMemory = (long)pi.PhysicalTotal.ToInt64() * (long)pi.PageSize.ToInt64() / (1024 * 1024 * 1024);
                    long availableMemory = (long)pi.PhysicalAvailable.ToInt64() * (long)pi.PageSize.ToInt64() / (1024 * 1024 * 1024);
                    long usedMemory = totalMemory - availableMemory;
                    long systemCache = (long)pi.SystemCache.ToInt64() * (long)pi.PageSize.ToInt64() / (1024 * 1024 * 1024);
                    
                    // 计算可释放内存（系统缓存 + 部分闲置内存）
                    long freedMemory = systemCache + (availableMemory / 4);
                    
                    TotalMemoryText.Text = $"{totalMemory} GB";
                    UsedMemoryText.Text = $"{usedMemory} GB";
                    AvailableMemoryText.Text = $"{availableMemory} GB";
                    FreedMemoryText.Text = $"{freedMemory} GB";
                    
                    int memoryUsagePercent = (int)((usedMemory / (double)totalMemory) * 100);
                    MemoryUsageBar.Value = memoryUsagePercent;
                    MemoryUsageText.Text = $"{memoryUsagePercent}%";
                    
                    // 自动优化检查
                    if (isAutoOptimizeEnabled && memoryUsagePercent > 80)
                    {
                        OptimizeMemory();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "更新内存状态失败";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void UpdateProcessList()
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => p.WorkingSet64 > 0)
                    .OrderByDescending(p => p.WorkingSet64)
                    .Take(20)
                    .Select(p => new ProcessInfo
                    {
                        ProcessName = p.ProcessName,
                        MemoryUsage = $"{(p.WorkingSet64 / (1024 * 1024))} MB",
                        Status = p.Responding ? "运行中" : "无响应"
                    })
                    .ToList();
                
                ProcessList.ItemsSource = processes;
            }
            catch (Exception ex)
            {
                StatusText.Text = "更新进程列表失败";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void OptimizeMemory()
        {
            try
            {
                StatusText.Text = "正在优化内存...";
                StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                
                // 记录优化前内存状态
                PerformanceInformation piBefore = new PerformanceInformation();
                GetPerformanceInfo(out piBefore, Marshal.SizeOf(typeof(PerformanceInformation)));
                beforeOptimizeMemory = (long)piBefore.PhysicalAvailable.ToInt64() * (long)piBefore.PageSize.ToInt64() / (1024 * 1024 * 1024);
                
                // 1. 清理系统缓存（参考EmptyStandbyList）
                ClearStandbyList();
                
                // 2. 优化当前进程内存
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                
                // 3. 延迟更新以显示效果
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateMemoryStatus();
                        UpdateProcessList();
                        
                        // 计算释放的内存
                        PerformanceInformation piAfter = new PerformanceInformation();
                        GetPerformanceInfo(out piAfter, Marshal.SizeOf(typeof(PerformanceInformation)));
                        long afterOptimizeMemory = (long)piAfter.PhysicalAvailable.ToInt64() * (long)piAfter.PageSize.ToInt64() / (1024 * 1024 * 1024);
                        long freedMemory = afterOptimizeMemory - beforeOptimizeMemory;
                        
                        StatusText.Text = $"内存优化完成，释放了 {freedMemory} GB 内存";
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                    });
                });
            }
            catch (Exception ex)
            {
                StatusText.Text = "内存优化失败";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void ClearStandbyList()
        {
            try
            {
                // 参考EmptyStandbyList项目，使用Windows API清理内存
                // 这里使用SetProcessWorkingSetSize来清理当前进程内存
                // 对于系统级别的内存清理，需要更高的权限
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        // 只清理非系统关键进程
                        if (!IsSystemCriticalProcess(process.ProcessName))
                        {
                            SetProcessWorkingSetSize(process.Handle, -1, -1);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        
        private bool IsSystemCriticalProcess(string processName)
        {
            string[] criticalProcesses = { "system", "svchost", "winlogon", "csrss", "lsass", "smss", "services", "explorer" };
            return criticalProcesses.Contains(processName.ToLower());
        }
        
        private void ToggleBackgroundOptimization()
        {
            if (backgroundOptimizeCts == null || backgroundOptimizeCts.IsCancellationRequested)
            {
                // 启动后台优化
                backgroundOptimizeCts = new CancellationTokenSource();
                var token = backgroundOptimizeCts.Token;
                
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            // 检查内存占用
                            PerformanceInformation pi = new PerformanceInformation();
                            if (GetPerformanceInfo(out pi, Marshal.SizeOf(typeof(PerformanceInformation))))
                            {
                                long totalMemory = (long)pi.PhysicalTotal.ToInt64() * (long)pi.PageSize.ToInt64() / (1024 * 1024 * 1024);
                                long availableMemory = (long)pi.PhysicalAvailable.ToInt64() * (long)pi.PageSize.ToInt64() / (1024 * 1024 * 1024);
                                int memoryUsagePercent = (int)(((totalMemory - availableMemory) / (double)totalMemory) * 100);
                                
                                // 当内存占用超过75%时进行优化
                                if (memoryUsagePercent > 75)
                                {
                                    ClearStandbyList();
                                    await Task.Delay(60000, token); // 优化后等待1分钟
                                }
                                else
                                {
                                    await Task.Delay(30000, token); // 每30秒检查一次
                                }
                            }
                            else
                            {
                                await Task.Delay(30000, token);
                            }
                        }
                        catch
                        {
                            await Task.Delay(30000, token);
                        }
                    }
                }, token);
                
                StatusText.Text = "后台智能优化已启动";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                BackgroundOptimizeButton.Content = "停止后台优化";
            }
            else if (backgroundOptimizeCts != null)
            {
                // 停止后台优化
                backgroundOptimizeCts.Cancel();
                backgroundOptimizeCts = null;
                
                StatusText.Text = "后台智能优化已停止";
                StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                BackgroundOptimizeButton.Content = "后台智能优化";
            }
        }
        
        private void OptimizeProcesses()
        {
            try
            {
                StatusText.Text = "正在优化进程内存...";
                StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
                
                // 获取所有进程
                Process[] processes = Process.GetProcesses();
                int optimizedCount = 0;
                
                foreach (Process process in processes)
                {
                    try
                    {
                        // 只优化非系统关键进程且内存占用较大的进程
                        if (!IsSystemCriticalProcess(process.ProcessName) && process.WorkingSet64 > 100 * 1024 * 1024) // 大于100MB
                        {
                            SetProcessWorkingSetSize(process.Handle, -1, -1);
                            optimizedCount++;
                        }
                    }
                    catch { }
                }
                
                // 更新状态
                UpdateMemoryStatus();
                UpdateProcessList();
                
                StatusText.Text = $"进程内存优化完成，优化了 {optimizedCount} 个进程";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                StatusText.Text = "进程内存优化失败";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void EnableAutoOptimize()
        {
            isAutoOptimizeEnabled = true;
            StatusText.Text = "自动优化已启用";
            StatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        
        private void DisableAutoOptimize()
        {
            isAutoOptimizeEnabled = false;
            StatusText.Text = "自动优化已禁用";
            StatusText.Foreground = System.Windows.Media.Brushes.Yellow;
        }
    }
}