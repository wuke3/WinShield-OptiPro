using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WinShieldOptiPro
{
    public class FileShredderService
    {
        private static FileShredderService _instance;
        private const string ContextMenuKey = @"Software\Classes\*\shell\WinShieldFileShredder";
        private const string ContextMenuCommandKey = @"Software\Classes\*\shell\WinShieldFileShredder\command";
        private const string DirectoryContextMenuKey = @"Software\Classes\Directory\shell\WinShieldFileShredder";
        private const string DirectoryContextMenuCommandKey = @"Software\Classes\Directory\shell\WinShieldFileShredder\command";
        private string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinShieldOptiPro", "FileShredder.log");

        public static FileShredderService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FileShredderService();
                }
                return _instance;
            }
        }

        private FileShredderService()
        {
            // 确保日志目录存在
            string logDir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        public void Start()
        {
            // 检查并添加右键菜单
            if (!IsContextMenuAdded())
            {
                AddContextMenu();
            }
        }

        public void Stop()
        {
            // 移除右键菜单
            RemoveContextMenu();
        }

        public bool IsContextMenuAdded()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ContextMenuKey))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public void AddContextMenu()
        {
            try
            {
                // 添加文件右键菜单
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ContextMenuKey))
                {
                    key.SetValue("Icon", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinShieldOptiPro.exe"));
                    key.SetValue("MUIVerb", "文件粉碎");
                }

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ContextMenuCommandKey))
                {
                    key.SetValue("", $"\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinShieldOptiPro.exe")}\" /shred \"%1\"");
                }

                // 添加目录右键菜单
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DirectoryContextMenuKey))
                {
                    key.SetValue("Icon", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinShieldOptiPro.exe"));
                    key.SetValue("MUIVerb", "文件粉碎");
                }

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DirectoryContextMenuCommandKey))
                {
                    key.SetValue("", $"\"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinShieldOptiPro.exe")}\" /shred \"%1\"");
                }

                Log("右键菜单已添加");
            }
            catch (Exception ex)
            {
                Log($"添加右键菜单失败: {ex.Message}");
            }
        }

        public void RemoveContextMenu()
        {
            try
            {
                // 移除文件右键菜单
                Registry.CurrentUser.DeleteSubKeyTree(ContextMenuKey, false);

                // 移除目录右键菜单
                Registry.CurrentUser.DeleteSubKeyTree(DirectoryContextMenuKey, false);

                Log("右键菜单已移除");
            }
            catch (Exception ex)
            {
                Log($"移除右键菜单失败: {ex.Message}");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetProcessId(IntPtr hProcess);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        public bool ShredFiles(IEnumerable<string> filePaths, bool showConfirmation = true)
        {
            if (filePaths == null || !filePaths.Any())
                return false;

            // 二次确认
            if (showConfirmation)
            {
                string message = $"确定要粉碎以下文件/目录吗？\n\n{string.Join("\n", filePaths)}\n\n粉碎后数据将不可恢复！";
                System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                    message, "文件粉碎确认", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return false;
            }

            bool allSuccess = true;

            foreach (string path in filePaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        ShredFile(path);
                        Log($"文件粉碎成功: {path}");
                    }
                    else if (Directory.Exists(path))
                    {
                        ShredDirectory(path);
                        Log($"目录粉碎成功: {path}");
                    }
                    else
                    {
                        Log($"路径不存在: {path}");
                        allSuccess = false;
                    }
                }
                catch (Exception ex)
                {
                    Log($"粉碎失败: {path} - {ex.Message}");
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        private void ShredFile(string filePath)
        {
            // 尝试解除文件占用
            ReleaseFileLock(filePath);

            // 尝试获取文件权限
            SetFilePermissions(filePath);

            try
            {
                // 使用SDelete逻辑，多次覆盖文件内容
                OverwriteFile(filePath);

                // 删除文件
                File.Delete(filePath);
            }
            catch
            {
                // 如果删除失败，尝试在系统重启时删除
                MoveFileEx(filePath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                Log($"文件将在系统重启后删除: {filePath}");
            }
        }

        private void ShredDirectory(string directoryPath)
        {
            // 递归处理目录内的所有文件和子目录
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                ShredFile(file);
            }

            foreach (string subDir in Directory.GetDirectories(directoryPath))
            {
                ShredDirectory(subDir);
            }

            try
            {
                // 删除空目录
                Directory.Delete(directoryPath);
            }
            catch
            {
                // 如果删除失败，尝试在系统重启时删除
                MoveFileEx(directoryPath, null, MOVEFILE_DELAY_UNTIL_REBOOT);
                Log($"目录将在系统重启后删除: {directoryPath}");
            }
        }

        private void OverwriteFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                long fileLength = fs.Length;
                byte[] randomData = new byte[4096];
                Random random = new Random();

                // 多次覆盖文件内容
                for (int pass = 0; pass < 3; pass++)
                {
                    fs.Position = 0;
                    long remaining = fileLength;

                    while (remaining > 0)
                    {
                        int bytesToWrite = (int)Math.Min(randomData.Length, remaining);
                        random.NextBytes(randomData);
                        fs.Write(randomData, 0, bytesToWrite);
                        remaining -= bytesToWrite;
                    }

                    fs.Flush();
                    fs.Position = 0;
                }
            }
        }

        private void ReleaseFileLock(string filePath)
        {
            try
            {
                // 这里可以实现更复杂的文件占用解除逻辑
                // 例如使用Handle工具或其他方法
            }
            catch { }
        }

        private void SetFilePermissions(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                FileSecurity fileSecurity = fileInfo.GetAccessControl();
                SecurityIdentifier currentUser = WindowsIdentity.GetCurrent().User;

                fileSecurity.AddAccessRule(new FileSystemAccessRule(
                    currentUser, 
                    FileSystemRights.FullControl, 
                    AccessControlType.Allow));

                fileInfo.SetAccessControl(fileSecurity);
            }
            catch { }
        }

        private void Log(string message)
        {
            try
            {
                string logEntry = $"[{DateTime.Now}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch { }
        }
    }
}