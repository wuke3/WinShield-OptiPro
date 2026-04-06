using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WinShieldOptiPro
{
    public partial class FileShredder : UserControl
    {
        private List<FileItem> fileItems = new List<FileItem>();

        public class FileItem
        {
            public string Path { get; set; }
            public string Type { get; set; }
            public string Size { get; set; }
        }

        public FileShredder()
        {
            InitializeComponent();
            Loaded += FileShredder_Loaded;
            AddFilesButton.Click += AddFilesButton_Click;
            AddFolderButton.Click += AddFolderButton_Click;
            ClearListButton.Click += ClearListButton_Click;
            ShredButton.Click += ShredButton_Click;
            ToggleContextMenuButton.Click += ToggleContextMenuButton_Click;
        }

        private void FileShredder_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateContextMenuStatus();
            FilesDataGrid.ItemsSource = fileItems;
        }

        private void UpdateContextMenuStatus()
        {
            bool isAdded = FileShredderService.Instance.IsContextMenuAdded();
            ContextMenuStatusText.Text = isAdded ? "已启用" : "已禁用";
            ToggleContextMenuButton.Content = isAdded ? "禁用右键菜单" : "启用右键菜单";
        }

        private void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "选择要粉碎的文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    AddFileToGrid(filePath);
                }
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择要粉碎的文件夹"
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AddFileToGrid(folderDialog.SelectedPath);
            }
        }

        private void AddFileToGrid(string path)
        {
            if (File.Exists(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                fileItems.Add(new FileItem
                {
                    Path = path,
                    Type = "文件",
                    Size = FormatSize(fileInfo.Length)
                });
            }
            else if (Directory.Exists(path))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                long totalSize = GetDirectorySize(dirInfo);
                fileItems.Add(new FileItem
                {
                    Path = path,
                    Type = "文件夹",
                    Size = FormatSize(totalSize)
                });
            }

            FilesDataGrid.ItemsSource = null;
            FilesDataGrid.ItemsSource = fileItems;
        }

        private long GetDirectorySize(DirectoryInfo directory)
        {
            long size = 0;
            try
            {
                foreach (FileInfo file in directory.GetFiles())
                {
                    size += file.Length;
                }
                foreach (DirectoryInfo subDir in directory.GetDirectories())
                {
                    size += GetDirectorySize(subDir);
                }
            }
            catch { }
            return size;
        }

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

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            fileItems.Clear();
            FilesDataGrid.ItemsSource = null;
            FilesDataGrid.ItemsSource = fileItems;
        }

        private void ShredButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileItems.Count == 0)
            {
                MessageBox.Show("请先添加要粉碎的文件或文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<string> paths = new List<string>();
            foreach (FileItem item in fileItems)
            {
                paths.Add(item.Path);
            }

            StatusText.Text = "正在粉碎...";
            
            // 显示动画
            ShredAnimationGrid.Visibility = Visibility.Visible;
            StartShredAnimation();

            // 异步粉碎文件
            System.Threading.Tasks.Task.Run(() =>
            {
                // 等待动画完成
                System.Threading.Thread.Sleep(2000);
                
                bool success = FileShredderService.Instance.ShredFiles(paths);

                Dispatcher.Invoke(() =>
                {
                    // 隐藏动画
                    ShredAnimationGrid.Visibility = Visibility.Collapsed;
                    
                    if (success)
                    {
                        StatusText.Text = "粉碎完成！";
                        fileItems.Clear();
                        FilesDataGrid.ItemsSource = null;
                        FilesDataGrid.ItemsSource = fileItems;
                    }
                    else
                    {
                        StatusText.Text = "部分文件粉碎失败，请查看日志";
                    }
                });
            });
        }

        private void StartShredAnimation()
        {
            // 清空画布
            ShredAnimationCanvas.Children.Clear();
            
            Random random = new Random();
            
            // 创建多个文件图标动画
            for (int i = 0; i < 20; i++)
            {
                // 创建文件图标
                Rectangle fileIcon = new Rectangle
                {
                    Width = 40,
                    Height = 50,
                    Fill = new LinearGradientBrush(Colors.Blue, Colors.DarkBlue, 0),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                
                // 创建文件标签
                Rectangle fileLabel = new Rectangle
                {
                    Width = 40,
                    Height = 10,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                
                // 设置初始位置
                double startX = random.Next(0, (int)ShredAnimationCanvas.Width - 40);
                double startY = random.Next(0, (int)ShredAnimationCanvas.Height - 60);
                
                Canvas.SetLeft(fileIcon, startX);
                Canvas.SetTop(fileIcon, startY);
                Canvas.SetLeft(fileLabel, startX);
                Canvas.SetTop(fileLabel, startY + 50);
                
                ShredAnimationCanvas.Children.Add(fileIcon);
                ShredAnimationCanvas.Children.Add(fileLabel);
                
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
                fileIcon.BeginAnimation(Canvas.LeftProperty, translateAnimationX);
                fileIcon.BeginAnimation(Canvas.TopProperty, translateAnimationY);
                fileIcon.BeginAnimation(OpacityProperty, opacityAnimation);
                
                RotateTransform rotateTransform = new RotateTransform();
                fileIcon.RenderTransform = rotateTransform;
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                
                ScaleTransform scaleTransform = new ScaleTransform();
                fileIcon.RenderTransform = new TransformGroup
                {
                    Children = { rotateTransform, scaleTransform }
                };
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
                
                // 为标签应用相同的动画
                fileLabel.BeginAnimation(Canvas.LeftProperty, translateAnimationX);
                fileLabel.BeginAnimation(Canvas.TopProperty, translateAnimationY);
                fileLabel.BeginAnimation(OpacityProperty, opacityAnimation);
                fileLabel.RenderTransform = new TransformGroup
                {
                    Children = { new RotateTransform(), new ScaleTransform() }
                };
                ((RotateTransform)((TransformGroup)fileLabel.RenderTransform).Children[0]).BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
                ((ScaleTransform)((TransformGroup)fileLabel.RenderTransform).Children[1]).BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
                ((ScaleTransform)((TransformGroup)fileLabel.RenderTransform).Children[1]).BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            
            // 添加粒子效果
            for (int i = 0; i < 50; i++)
            {
                Ellipse particle = new Ellipse
                {
                    Width = random.Next(2, 5),
                    Height = random.Next(2, 5),
                    Fill = new SolidColorBrush(Color.FromArgb((byte)random.Next(100, 255), 0, 191, 255))
                };
                
                double startX = ShredAnimationCanvas.Width / 2;
                double startY = ShredAnimationCanvas.Height / 2;
                
                Canvas.SetLeft(particle, startX);
                Canvas.SetTop(particle, startY);
                
                ShredAnimationCanvas.Children.Add(particle);
                
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

        private void ToggleContextMenuButton_Click(object sender, RoutedEventArgs e)
        {
            bool isAdded = FileShredderService.Instance.IsContextMenuAdded();
            if (isAdded)
            {
                FileShredderService.Instance.RemoveContextMenu();
            }
            else
            {
                FileShredderService.Instance.AddContextMenu();
            }
            UpdateContextMenuStatus();
        }
    }
}