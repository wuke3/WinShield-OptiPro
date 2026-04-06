using System;
using System.Collections.Generic;

namespace WinShieldOptiPro;

// 目录项类
public class DirectoryItem
{
    public string Name { get; set; }
    public long Size { get; set; }
    public string SizeText { get; set; }
    public List<DirectoryItem> SubItems { get; set; }
    public string Path { get; set; }

    public DirectoryItem(string name, long size, string path)
    {
        Name = name;
        Size = size;
        SizeText = FormatSize(size);
        SubItems = new List<DirectoryItem>();
        Path = path;
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
        else if (bytes >= 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0), 2)} MB";
        else if (bytes >= 1024)
            return $"{Math.Round(bytes / 1024.0, 2)} KB";
        else
            return $"{bytes} B";
    }
}

// 高危占用项类
public class HighRiskItem
{
    public string Name { get; set; }
    public long Size { get; set; }
    public string SizeText { get; set; }
    public string Description { get; set; }

    public HighRiskItem(string name, long size, string description)
    {
        Name = name;
        Size = size;
        SizeText = FormatSize(size);
        Description = description;
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
        else if (bytes >= 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0), 2)} MB";
        else if (bytes >= 1024)
            return $"{Math.Round(bytes / 1024.0, 2)} KB";
        else
            return $"{bytes} B";
    }
}

// 磁盘使用项类
public class DiskUsageItem
{
    public string Name { get; set; }
    public long Size { get; set; }
    public string SizeText { get; set; }
    public double Percentage { get; set; }

    public DiskUsageItem(string name, long size, double percentage)
    {
        Name = name;
        Size = size;
        SizeText = FormatSize(size);
        Percentage = percentage;
    }

    private string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0 * 1024.0), 2)} GB";
        else if (bytes >= 1024 * 1024)
            return $"{Math.Round(bytes / (1024.0 * 1024.0), 2)} MB";
        else if (bytes >= 1024)
            return $"{Math.Round(bytes / 1024.0, 2)} KB";
        else
            return $"{bytes} B";
    }
}