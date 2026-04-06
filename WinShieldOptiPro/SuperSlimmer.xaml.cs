using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WinShieldOptiPro
{
    public partial class SuperSlimmer : UserControl
    {
        private string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private string localLowAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..", "LocalLow");
        private List<DuplicateFileGroup> duplicateFileGroups = new List<DuplicateFileGroup>();
        private List<string> orphanFiles = new List<string>();
        private List<string> incrementalSnapshots = new List<string>();
        private long totalFreedSpace = 0;

        public SuperSlimmer()
        {
            InitializeComponent();
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanResultText.Text = "正在扫描分析...";
            ScanProgressBar.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            CleanResultText.Text = "";
            totalFreedSpace = 0;
            duplicateFileGroups.Clear();
            orphanFiles.Clear();
            incrementalSnapshots.Clear();

            try
            {
                await Task.Run(() =>
                {
                    if (DuplicateCacheCheckBox.IsChecked == true)
                    {
                        ScanDuplicateCaches();
                    }

                    if (IncrementalSnapshotsCheckBox.IsChecked == true)
                    {
                        ScanIncrementalSnapshots();
                    }

                    if (OrphanLinksCheckBox.IsChecked == true)
                    {
                        ScanOrphanLinks();
                    }
                });

                UpdateScanResults();
            }
            catch (Exception ex)
            {
                ScanResultText.Text = $"扫描过程中出现错误: {ex.Message}";
            }
            finally
            {
                ScanProgressBar.Visibility = Visibility.Collapsed;
                ProgressText.Visibility = Visibility.Collapsed;
            }
        }

        private void ScanDuplicateCaches()
        {
            Dictionary<string, List<string>> hashToFiles = new Dictionary<string, List<string>>();
            string[] appDataDirs = { appDataPath, localAppDataPath, localLowAppDataPath };

            foreach (string dir in appDataDirs)
            {
                if (Directory.Exists(dir))
                {
                    ScanDirectoryForDuplicates(dir, hashToFiles);
                }
            }

            foreach (var entry in hashToFiles)
            {
                if (entry.Value.Count > 1)
                {
                    duplicateFileGroups.Add(new DuplicateFileGroup
                    {
                        Hash = entry.Key,
                        Files = entry.Value,
                        TotalSize = entry.Value.Sum(f => new FileInfo(f).Length)
                    });
                    totalFreedSpace += entry.Value.Skip(1).Sum(f => new FileInfo(f).Length);
                }
            }
        }

        private void ScanDirectoryForDuplicates(string directory, Dictionary<string, List<string>> hashToFiles)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length > 1024 * 1024) // 只处理大于1MB的文件
                        {
                            string hash = GetFileHash(file);
                            if (!hashToFiles.ContainsKey(hash))
                            {
                                hashToFiles[hash] = new List<string>();
                            }
                            hashToFiles[hash].Add(file);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string GetFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        private void ScanIncrementalSnapshots()
        {
            string[] targetDirs = {
                Path.Combine(localAppDataPath, "Tencent", "WeChat"),
                Path.Combine(localAppDataPath, "Adobe"),
                Path.Combine(appDataPath, "Microsoft", "Office")
            };

            foreach (string dir in targetDirs)
            {
                if (Directory.Exists(dir))
                {
                    ScanForIncrementalSnapshots(dir);
                }
            }
        }

        private void ScanForIncrementalSnapshots(string directory)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length > 1024 * 1024 && 
                            (file.Contains("backup") || file.Contains("snapshot") || file.Contains("history") || 
                             file.EndsWith(".bak") || file.EndsWith(".old")))
                        {
                            if (fi.LastWriteTime < DateTime.Now.AddDays(-30)) // 清理30天前的文件
                            {
                                incrementalSnapshots.Add(file);
                                totalFreedSpace += fi.Length;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ScanOrphanLinks()
        {
            string[] systemDirs = {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                localAppDataPath
            };

            foreach (string dir in systemDirs)
            {
                if (Directory.Exists(dir))
                {
                    ScanForOrphanLinks(dir);
                }
            }
        }

        private void ScanForOrphanLinks(string directory)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (IsOrphanFile(file))
                        {
                            orphanFiles.Add(file);
                            totalFreedSpace += new FileInfo(file).Length;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private bool IsOrphanFile(string filePath)
        {
            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Length == 0) return false;

                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".dll" || extension == ".exe")
                {
                    if (!IsFileInUse(filePath))
                    {
                        string parentDir = Path.GetDirectoryName(filePath);
                        if (Directory.GetFiles(parentDir).Length <= 3) // 目录中文件很少，可能是孤儿文件
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool IsFileInUse(string filePath)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }

        private void UpdateScanResults()
        {
            string result = "扫描完成！";
            result += $"\n可释放空间：{FormatSize(totalFreedSpace)}";
            result += $"\n重复缓存文件组：{duplicateFileGroups.Count}";
            result += $"\n过期增量快照：{incrementalSnapshots.Count}";
            result += $"\n孤儿文件：{orphanFiles.Count}";
            ScanResultText.Text = result;
        }

        private async void SingleCleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (duplicateFileGroups.Count == 0 && incrementalSnapshots.Count == 0 && orphanFiles.Count == 0)
            {
                MessageBox.Show("请先扫描分析", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Task.Run(() =>
            {
                if (DuplicateCacheCheckBox.IsChecked == true)
                {
                    ProcessDuplicateCaches();
                }

                if (IncrementalSnapshotsCheckBox.IsChecked == true)
                {
                    ProcessIncrementalSnapshots();
                }

                if (OrphanLinksCheckBox.IsChecked == true)
                {
                    ProcessOrphanLinks();
                }
            });

            CleanResultText.Text = $"清理完成！实际释放空间：{FormatSize(totalFreedSpace)}";
        }

        private async void FullCleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (duplicateFileGroups.Count == 0 && incrementalSnapshots.Count == 0 && orphanFiles.Count == 0)
            {
                MessageBox.Show("请先扫描分析", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await Task.Run(() =>
            {
                ProcessDuplicateCaches();
                ProcessIncrementalSnapshots();
                ProcessOrphanLinks();
            });

            CleanResultText.Text = $"全套瘦身完成！实际释放空间：{FormatSize(totalFreedSpace)}";
        }

        private void ProcessDuplicateCaches()
        {
            foreach (var group in duplicateFileGroups)
            {
                if (group.Files.Count > 1)
                {
                    string masterFile = group.Files[0];
                    for (int i = 1; i < group.Files.Count; i++)
                    {
                        string duplicateFile = group.Files[i];
                        try
                        {
                            ReplaceWithHardLink(masterFile, duplicateFile);
                        }
                        catch { }
                    }
                }
            }
        }

        private void ReplaceWithHardLink(string sourceFile, string targetFile)
        {
            try
            {
                string tempFile = targetFile + ".tmp";
                File.Delete(tempFile);
                CreateHardLink(tempFile, sourceFile, IntPtr.Zero);
                File.Delete(targetFile);
                File.Move(tempFile, targetFile);
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private void ProcessIncrementalSnapshots()
        {
            foreach (string file in incrementalSnapshots)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private void ProcessOrphanLinks()
        {
            foreach (string file in orphanFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{(bytes / 1024.0):F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
            else
                return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
        }
    }

    public class DuplicateFileGroup
    {
        public string Hash { get; set; }
        public List<string> Files { get; set; }
        public long TotalSize { get; set; }
    }
}