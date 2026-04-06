using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WinShieldOptiPro
{
    public partial class GlassTooltip : Window
    {
        private DispatcherTimer hideTimer;
        private bool isFadingOut;

        public GlassTooltip()
    {
        InitializeComponent();
        Loaded += GlassTooltip_Loaded;
    }

        private void GlassTooltip_Loaded(object sender, RoutedEventArgs e)
        {
            // 初始化隐藏定时器
            hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            hideTimer.Tick += HideTimer_Tick;

            // 初始隐藏
            Opacity = 0;
            Visibility = Visibility.Hidden;
        }

        public void ShowTooltip(string text, double x, double y)
        {
            // 设置提示文本
            TooltipText.Text = text;

            // 设置位置
            Left = x;
            Top = y;

            // 显示窗口
            Visibility = Visibility.Visible;

            // 重置隐藏定时器
            hideTimer.Stop();
            hideTimer.Start();

            // 重置淡出标志
            isFadingOut = false;

            // 淡入动画
            DoubleAnimation fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            BeginAnimation(OpacityProperty, fadeInAnimation);
        }

        private void HideTimer_Tick(object sender, EventArgs e)
        {
            hideTimer.Stop();
            FadeOut();
        }

        private void FadeOut()
        {
            if (isFadingOut)
                return;

            isFadingOut = true;

            // 淡出动画
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            fadeOutAnimation.Completed += (s, args) =>
            {
                Visibility = Visibility.Hidden;
            };
            BeginAnimation(OpacityProperty, fadeOutAnimation);
        }
    }
}
