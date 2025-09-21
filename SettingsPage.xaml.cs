using System;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BuraunzaApp
{
    public partial class SettingsPage : ContentPage
    {
        // 设置类
        public class BrowserSettings
        {
            public string HomePageUrl { get; set; } = "local://default.html";
            public string SearchEngine { get; set; } = "必应";
            public string StartupBehavior { get; set; } = "打开主页";
            public string CookiePolicy { get; set; } = "允许所有Cookie";
            public bool TrackingProtection { get; set; } = true;
            public string Theme { get; set; } = "跟随系统";
            public int FontSize { get; set; } = 16;
            public double ZoomLevel { get; set; } = 1.0;
            public bool JavaScriptEnabled { get; set; } = true;
            public bool PopupBlocking { get; set; } = true;
            public bool HardwareAcceleration { get; set; } = true;
            public bool DevToolsEnabled { get; set; } = false;
            
            // 下载设置
            public string DownloadDirectory { get; set; } = string.Empty;
            public bool DownloadConfirmation { get; set; } = true;
            public bool AutoOpenDownloads { get; set; } = false;
        }

        private BrowserSettings? _currentSettings;

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            InitializeControls();
        }

        private void LoadSettings()
        {
            try
            {
                var settingsJson = Preferences.Get("BrowserSettings", string.Empty);
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    _currentSettings = System.Text.Json.JsonSerializer.Deserialize<BrowserSettings>(settingsJson);
                }
                else
                {
                    _currentSettings = new BrowserSettings();
                }
            }
            catch
            {
                _currentSettings = new BrowserSettings();
            }
        }

        private void SaveSettings()
        {
            try
            {
                if (_currentSettings != null)
                {
                    var settingsJson = System.Text.Json.JsonSerializer.Serialize(_currentSettings);
                    Preferences.Set("BrowserSettings", settingsJson);
                    
                    // 显示保存成功提示
                    DisplayAlert("成功", "设置已保存", "确定");
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("错误", "保存设置时发生错误: " + ex.Message, "确定");
            }
        }

        private void InitializeControls()
        {
            // 初始化控件值
            if (_currentSettings == null) return;
            
            HomePageEntry.Text = _currentSettings.HomePageUrl;
            
            SearchEnginePicker.SelectedItem = _currentSettings.SearchEngine;
            StartupBehaviorPicker.SelectedItem = _currentSettings.StartupBehavior;
            CookiePolicyPicker.SelectedItem = _currentSettings.CookiePolicy;
            
            TrackingProtectionSwitch.IsToggled = _currentSettings.TrackingProtection;
            
            ThemePicker.SelectedItem = _currentSettings.Theme;
            
            FontSizeSlider.Value = _currentSettings.FontSize;
            FontSizeLabel.Text = $"{_currentSettings.FontSize}px";
            
            ZoomLevelSlider.Value = _currentSettings.ZoomLevel;
            ZoomLevelLabel.Text = $"{_currentSettings.ZoomLevel * 100}%";
            
            JavaScriptSwitch.IsToggled = _currentSettings.JavaScriptEnabled;
            PopupBlockingSwitch.IsToggled = _currentSettings.PopupBlocking;
            HardwareAccelerationSwitch.IsToggled = _currentSettings.HardwareAcceleration;
            DevToolsSwitch.IsToggled = _currentSettings.DevToolsEnabled;
            
            // 下载设置
            DownloadDirectoryEntry.Text = _currentSettings.DownloadDirectory;
            DownloadConfirmationSwitch.IsToggled = _currentSettings.DownloadConfirmation;
            AutoOpenDownloadsSwitch.IsToggled = _currentSettings.AutoOpenDownloads;
        }

        // 事件处理方法
        private void OnHomePageChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.HomePageUrl = HomePageEntry.Text;
        }

        private void OnSearchEngineChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.SearchEngine = SearchEnginePicker.SelectedItem?.ToString() ?? "必应";
        }

        private void OnStartupBehaviorChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.StartupBehavior = StartupBehaviorPicker.SelectedItem?.ToString() ?? "打开主页";
        }

        private void OnCookiePolicyChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.CookiePolicy = CookiePolicyPicker.SelectedItem?.ToString() ?? "允许所有Cookie";
        }

        private void OnTrackingProtectionToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.TrackingProtection = e.Value;
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.Theme = ThemePicker.SelectedItem?.ToString() ?? "跟随系统";
        }

        private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.FontSize = (int)e.NewValue;
            FontSizeLabel.Text = $"{(int)e.NewValue}px";
        }

        private void OnZoomLevelChanged(object sender, ValueChangedEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.ZoomLevel = e.NewValue;
            ZoomLevelLabel.Text = $"{e.NewValue * 100}%";
        }

        private void OnJavaScriptToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.JavaScriptEnabled = e.Value;
        }

        private void OnPopupBlockingToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.PopupBlocking = e.Value;
        }

        private void OnHardwareAccelerationToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.HardwareAcceleration = e.Value;
        }

        private void OnDevToolsToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.DevToolsEnabled = e.Value;
        }

        private async void OnClearDataClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("确认", "确定要清除所有浏览数据吗？此操作不可撤销。", "确定", "取消");
            if (result)
            {
                // 这里可以添加清除浏览数据的逻辑
                await DisplayAlert("完成", "浏览数据已清除", "确定");
            }
        }

        private void OnSaveSettingsClicked(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private async void OnResetSettingsClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("确认", "确定要恢复默认设置吗？", "确定", "取消");
            if (result)
            {
                _currentSettings = new BrowserSettings();
                InitializeControls();
                SaveSettings();
                await DisplayAlert("完成", "设置已恢复默认值", "确定");
            }
        }

        // 下载设置事件处理方法
        private void OnDownloadDirectoryChanged(object sender, EventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.DownloadDirectory = DownloadDirectoryEntry.Text;
        }

        private async void OnSelectDownloadDirectoryClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "选择下载目录",
                    FileTypes = null
                });
                
                if (result != null)
                {
                    var directory = Path.GetDirectoryName(result.FullPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        DownloadDirectoryEntry.Text = directory;
                        if (_currentSettings != null)
                            _currentSettings.DownloadDirectory = directory;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("错误", "选择目录时发生错误: " + ex.Message, "确定");
            }
        }

        private void OnDownloadConfirmationToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.DownloadConfirmation = e.Value;
        }

        private void OnAutoOpenDownloadsToggled(object sender, ToggledEventArgs e)
        {
            if (_currentSettings != null)
                _currentSettings.AutoOpenDownloads = e.Value;
        }

        // 静态方法用于获取设置
        public static BrowserSettings GetCurrentSettings()
        {
            try
            {
                var settingsJson = Preferences.Get("BrowserSettings", string.Empty);
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    var settings = System.Text.Json.JsonSerializer.Deserialize<BrowserSettings>(settingsJson);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // 忽略错误，返回默认设置
            }
            return new BrowserSettings();
        }
    }
}