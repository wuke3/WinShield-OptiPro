using System;
using System.Windows;

namespace TestApp
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("Test app started");
            
            // 创建一个简单的窗口
            Window window = new Window
            {
                Title = "Test Window",
                Width = 800,
                Height = 600
            };
            
            Application app = new Application();
            app.Run(window);
        }
    }
}