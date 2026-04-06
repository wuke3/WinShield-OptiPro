using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace WinShieldOptiPro
{
    public partial class RuleManager : Window
    {
        private List<string> blacklist = new List<string>();
        private List<string> whitelist = new List<string>();
        private string blacklistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "blacklist.txt");
        private string whitelistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinShieldOptiPro", "whitelist.txt");

        public RuleManager()
        {
            InitializeComponent();
            LoadRules();
            UpdateListBoxes();
            UpdateCounters();
        }

        private void LoadRules()
        {
            if (File.Exists(blacklistFilePath))
                blacklist = new List<string>(File.ReadAllLines(blacklistFilePath));
            if (File.Exists(whitelistFilePath))
                whitelist = new List<string>(File.ReadAllLines(whitelistFilePath));
        }

        private void UpdateListBoxes()
        {
            BlacklistBox.ItemsSource = blacklist;
            WhitelistBox.ItemsSource = whitelist;
        }

        private void UpdateCounters()
        {
            BlacklistCount.Text = $"({blacklist.Count})";
            WhitelistCount.Text = $"({whitelist.Count})";
        }

        private void AddToBlacklist(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.OpenFileDialog();
            dialog.Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*";
            dialog.Title = "选择要添加到黑名单的应用程序";
            
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!blacklist.Contains(processName))
                {
                    blacklist.Add(processName);
                    UpdateListBoxes();
                    UpdateCounters();
                }
            }
        }

        private void RemoveFromBlacklist(object sender, RoutedEventArgs e)
        {
            if (BlacklistBox.SelectedItem != null)
            {
                string selectedItem = BlacklistBox.SelectedItem.ToString();
                blacklist.Remove(selectedItem);
                UpdateListBoxes();
                UpdateCounters();
            }
        }

        private void AddToWhitelist(object sender, RoutedEventArgs e)
        {
            var dialog = new Forms.OpenFileDialog();
            dialog.Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*";
            dialog.Title = "选择要添加到白名单的应用程序";
            
            if (dialog.ShowDialog() == Forms.DialogResult.OK)
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!whitelist.Contains(processName))
                {
                    whitelist.Add(processName);
                    UpdateListBoxes();
                    UpdateCounters();
                }
            }
        }

        private void RemoveFromWhitelist(object sender, RoutedEventArgs e)
        {
            if (WhitelistBox.SelectedItem != null)
            {
                string selectedItem = WhitelistBox.SelectedItem.ToString();
                whitelist.Remove(selectedItem);
                UpdateListBoxes();
                UpdateCounters();
            }
        }

        private void SaveRules(object sender, RoutedEventArgs e)
        {
            File.WriteAllLines(blacklistFilePath, blacklist);
            File.WriteAllLines(whitelistFilePath, whitelist);
            MessageBox.Show("规则已保存");
            this.Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
