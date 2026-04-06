using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WinShieldOptiPro;

public partial class SplashScreen : Window
{
    private List<Ellipse> particles = new List<Ellipse>();
    private DispatcherTimer? animationTimer;
    private DispatcherTimer? progressTimer;
    private DispatcherTimer? statusTimer;
    private double progress = 0;
    private string[] statusMessages = { "系统初始化中...", "加载核心组件...", "校准系统参数...", "启动完成" };
    private int currentStatusIndex = 0;
    private bool isClosing = false;
    private Random random = new Random();

    public SplashScreen()
    {
        InitializeComponent();
        Loaded += SplashScreen_Loaded;
        Closed += SplashScreen_Closed;
    }

    private void SplashScreen_Closed(object sender, EventArgs e)
    {
        // 清理资源
        progressTimer?.Stop();
        animationTimer?.Stop();
        statusTimer?.Stop();
    }

    private void SplashScreen_Loaded(object sender, RoutedEventArgs e)
    {
        // 初始化粒子
        InitializeParticles();

        // 启动入场动画序列
        StartEntranceAnimation();
    }

    private void InitializeParticles()
    {
        int canvasWidth = 800; // 使用固定宽度，与XAML中设置的一致
        int canvasHeight = 600; // 使用固定高度，与XAML中设置的一致
        
        for (int i = 0; i < 30; i++)
        {
            Ellipse particle = new Ellipse
            {
                Width = random.Next(2, 5),
                Height = random.Next(2, 5),
                Fill = new SolidColorBrush(Color.FromArgb((byte)random.Next(80, 180), 0, 191, 255)),
                Opacity = random.NextDouble() * 0.6 + 0.2
            };
            Canvas.SetLeft(particle, random.Next(0, canvasWidth));
            Canvas.SetTop(particle, random.Next(0, canvasHeight));
            ParticleCanvas.Children.Add(particle);
            particles.Add(particle);
        }

        // 启动粒子动画
        animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        animationTimer.Tick += AnimationTimer_Tick;
        animationTimer.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        int canvasWidth = 800; // 使用固定宽度，与XAML中设置的一致
        int canvasHeight = 600; // 使用固定高度，与XAML中设置的一致
        
        foreach (var particle in particles)
        {
            double left = Canvas.GetLeft(particle);
            double top = Canvas.GetTop(particle);

            // 缓慢向上飘动
            left += (random.NextDouble() - 0.5) * 1.5;
            top -= random.NextDouble() * 1.5;

            // 边界检查
            if (left < 0) left = canvasWidth;
            if (left > canvasWidth) left = 0;
            if (top < 0) top = canvasHeight;
            if (top > canvasHeight) top = 0;

            Canvas.SetLeft(particle, left);
            Canvas.SetTop(particle, top);
        }
    }

    private void StartEntranceAnimation()
    {
        // 设置初始状态文本
        StatusText.Text = statusMessages[0];

        // Logo淡入动画
        DoubleAnimation logoFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.8),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        // Logo缩放动画
        ScaleTransform logoScale = new ScaleTransform(0.5, 0.5);
        LogoImage.RenderTransform = logoScale;
        LogoImage.RenderTransformOrigin = new Point(0.5, 0.5);

        DoubleAnimation logoScaleX = new DoubleAnimation
        {
            From = 0.5,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.8),
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 8 }
        };

        DoubleAnimation logoScaleY = new DoubleAnimation
        {
            From = 0.5,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.8),
            EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 8 }
        };

        // 软件名称动画
        DoubleAnimation nameFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.6),
            BeginTime = TimeSpan.FromSeconds(0.4)
        };

        // 状态文本动画
        DoubleAnimation statusFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5),
            BeginTime = TimeSpan.FromSeconds(0.6)
        };

        // 进度条容器
        Grid? progressContainer = ProgressBorder.Parent as Grid;

        // 进度条动画
        DoubleAnimation progressFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5),
            BeginTime = TimeSpan.FromSeconds(0.7)
        };

        // 版本信息动画
        DoubleAnimation versionFadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromSeconds(0.5),
            BeginTime = TimeSpan.FromSeconds(0.8)
        };

        // 启动所有动画
        LogoImage.BeginAnimation(OpacityProperty, logoFadeIn);
        logoScale.BeginAnimation(ScaleTransform.ScaleXProperty, logoScaleX);
        logoScale.BeginAnimation(ScaleTransform.ScaleYProperty, logoScaleY);
        AppNameText.BeginAnimation(OpacityProperty, nameFadeIn);
        StatusText.BeginAnimation(OpacityProperty, statusFadeIn);

        // 获取进度条的父容器并应用动画
        if (progressContainer != null)
        {
            progressContainer.SetValue(OpacityProperty, 1.0);
            progressContainer.BeginAnimation(OpacityProperty, progressFadeIn);
        }

        VersionText.BeginAnimation(OpacityProperty, versionFadeIn);

        // 启动加载进度
        DispatcherTimer startProgressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.0)
        };
        startProgressTimer.Tick += (s, args) =>
        {
            startProgressTimer.Stop();
            StartProgress();
        };
        startProgressTimer.Start();
    }

    private void StartProgress()
    {
        progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        progressTimer.Tick += ProgressTimer_Tick;
        progressTimer.Start();

        // 启动状态文本切换
        StartStatusTextSwitch();
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        progress += 0.8;
        if (progress >= 100)
        {
            progress = 100;
            progressTimer?.Stop();
            animationTimer?.Stop();
            statusTimer?.Stop();

            // 延迟关闭闪屏
            DispatcherTimer closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.8)
            };
            closeTimer.Tick += (s, args) =>
            {
                closeTimer.Stop();
                CloseSplashScreen();
            };
            closeTimer.Start();
        }

        // 更新进度条
        ProgressBorder.Width = (progress / 100) * 400;
    }

    private void CloseSplashScreen()
    {
        if (isClosing) return;
        isClosing = true;

        // 停止所有定时器
        progressTimer?.Stop();
        animationTimer?.Stop();
        statusTimer?.Stop();

        // 直接关闭窗口
        Close();
    }

    private void StartStatusTextSwitch()
    {
        statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        statusTimer.Tick += (s, args) =>
        {
            currentStatusIndex++;
            if (currentStatusIndex >= statusMessages.Length)
            {
                statusTimer?.Stop();
                return;
            }

            // 淡出旧文本
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.2)
            };

            fadeOut.Completed += (sender2, e2) =>
            {
                StatusText.Text = statusMessages[currentStatusIndex];

                // 淡入新文本
                DoubleAnimation fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.2)
                };
                StatusText.BeginAnimation(OpacityProperty, fadeIn);
            };

            StatusText.BeginAnimation(OpacityProperty, fadeOut);
        };
        statusTimer.Start();
    }
}
