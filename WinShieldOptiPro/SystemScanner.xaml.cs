using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WinShieldOptiPro
{
    public partial class SystemScanner : UserControl
    {
        private Process scanProcess;
        private bool isScanning;
        private string customScanPath;
        private List<ThreatInfo> threats;
        private DispatcherTimer progressTimer;

        public SystemScanner()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeTimer();
            threats = new List<ThreatInfo>();
        }

        private void InitializeEvents()
        {
            QuickScanRadio.Checked += (s, e) => UpdateScanModeUI();
            FullScanRadio.Checked += (s, e) => UpdateScanModeUI();
            CustomScanRadio.Checked += (s, e) => UpdateScanModeUI();
        }

        private void InitializeTimer()
        {
            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += (s, e) => UpdateScanProgress();
        }

        private void UpdateScanModeUI()
        {
            if (CustomScanRadio.IsChecked == true)
            {
                BrowseButton.Visibility = Visibility.Visible;
                CustomPathText.Visibility = Visibility.Visible;
            }
            else
            {
                BrowseButton.Visibility = Visibility.Collapsed;
                CustomPathText.Visibility = Visibility.Collapsed;
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                customScanPath = folderDialog.SelectedPath;
                CustomPathText.Text = customScanPath;
            }
        }

        private void StartScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (isScanning) return;

            // 准备扫描
            isScanning = true;
            StartScanButton.IsEnabled = false;
            StopScanButton.IsEnabled = true;
            ShieldStatus.Text = "扫描中";
            ShieldStatus.Foreground = System.Windows.Media.Brushes.Yellow;
            ScanProgress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            ScanResult.Text = "正在启动扫描...";
            threats.Clear();
            ThreatList.Items.Clear();
            progressTimer.Start();

            // 确定扫描模式
            string scanCommand = GetScanCommand();
            if (string.IsNullOrEmpty(scanCommand))
            {
                ScanResult.Text = "请选择有效的扫描模式";
                ResetUI();
                return;
            }

            // 启动扫描进程
            StartScanProcess(scanCommand);
        }

        private string GetScanCommand()
        {
            if (QuickScanRadio.IsChecked == true)
            {
                return "Start-MpScan -ScanType QuickScan";
            }
            else if (FullScanRadio.IsChecked == true)
            {
                return "Start-MpScan -ScanType FullScan";
            }
            else if (CustomScanRadio.IsChecked == true)
            {
                if (string.IsNullOrEmpty(customScanPath) || !Directory.Exists(customScanPath))
                {
                    return null;
                }
                return $"Start-MpScan -ScanType CustomScan -ScanPath '{customScanPath}'";
            }
            return null;
        }

        private void StartScanProcess(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{command}\"", 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                scanProcess = new Process { StartInfo = psi };
                scanProcess.EnableRaisingEvents = true;
                scanProcess.Exited += (s, e) => ScanProcess_Exited();
                scanProcess.Start();

                // 异步读取输出
                ReadProcessOutput();
            }
            catch (Exception ex)
            {
                ScanResult.Text = "启动扫描失败: " + ex.Message;
                ResetUI();
            }
        }

        private async void ReadProcessOutput()
        {
            if (scanProcess == null) return;

            try
            {
                using (var reader = scanProcess.StandardOutput)
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        ProcessOutputLine(line);
                    }
                }

                using (var reader = scanProcess.StandardError)
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        ProcessErrorLine(line);
                    }
                }
            }
            catch { }
        }

        private void ProcessOutputLine(string line)
        {
            // 处理扫描输出
            if (line.Contains("威胁"))
            {
                // 解析威胁信息
                ParseThreatInfo(line);
            }
        }

        private void ProcessErrorLine(string line)
        {
            // 处理错误信息
            Dispatcher.Invoke(() =>
            {
                ScanResult.Text = "扫描错误: " + line;
            });
        }

        private void ParseThreatInfo(string line)
        {
            // 简单的威胁信息解析
            var threat = new ThreatInfo
            {
                Path = line,
                Type = "恶意软件"
            };
            
            Dispatcher.Invoke(() =>
            {
                threats.Add(threat);
                ThreatList.Items.Add(threat);
            });
        }

        private void UpdateScanProgress()
        {
            if (!isScanning) return;

            // 模拟进度更新
            // 实际项目中可以通过WMI或其他方式获取真实进度
            if (ScanProgress.Value < 95)
            {
                ScanProgress.Value += 1;
                ProgressText.Text = $"{ScanProgress.Value}%";
            }
        }

        private void ScanProcess_Exited()
        {
            Dispatcher.Invoke(() =>
            {
                CompleteScan();
            });
        }

        private void CompleteScan()
        {
            isScanning = false;
            progressTimer.Stop();
            ScanProgress.Value = 100;
            ProgressText.Text = "100%";

            if (threats.Count > 0)
            {
                ShieldStatus.Text = "发现威胁";
                ShieldStatus.Foreground = System.Windows.Media.Brushes.Red;
                ScanResult.Text = $"扫描完成，发现 {threats.Count} 个威胁";
            }
            else
            {
                ShieldStatus.Text = "扫描完成";
                ShieldStatus.Foreground = System.Windows.Media.Brushes.Green;
                ScanResult.Text = "扫描完成，未发现威胁";
            }

            ResetUI();
        }

        private void StopScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isScanning || scanProcess == null) return;

            try
            {
                scanProcess.Kill();
            }
            catch { }

            isScanning = false;
            progressTimer.Stop();
            ShieldStatus.Text = "已停止";
            ShieldStatus.Foreground = System.Windows.Media.Brushes.Orange;
            ScanResult.Text = "扫描已停止";
            ResetUI();
        }

        private void ClearResultsButton_Click(object sender, RoutedEventArgs e)
        {
            threats.Clear();
            ThreatList.Items.Clear();
            ScanResult.Text = "准备就绪";
            ShieldStatus.Text = "空闲";
            ShieldStatus.Foreground = System.Windows.Media.Brushes.Cyan;
            ScanProgress.Value = 0;
            ProgressText.Text = "0%";
            ScanProgress.Visibility = Visibility.Collapsed;
            ProgressText.Visibility = Visibility.Collapsed;
        }

        private void ResetUI()
        {
            StartScanButton.IsEnabled = true;
            StopScanButton.IsEnabled = false;
        }
    }

    public class ThreatInfo
    {
        public string Path { get; set; }
        public string Type { get; set; }
    }
}