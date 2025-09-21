using System.Collections.Generic;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using System.Net;
using System.Threading.Tasks;

namespace BuraunzaApp
{
    public partial class MainPage : ContentPage
    {
        private readonly string _homePageUrl;
        private readonly Stack<string> _backStack = new();
        private readonly Stack<string> _forwardStack = new();
        private string? _currentDownloadDirectory;

        public MainPage()
        {
            InitializeComponent();

            // 从XAML中获取控件引用
            BackButton = this.FindByName<Button>("BackButton");
            ForwardButton = this.FindByName<Button>("ForwardButton");

            UrlEntry = this.FindByName<Entry>("UrlEntry");
            StatusLabel = this.FindByName<Label>("StatusLabel");

            // 设置主页URL - 使用local://协议访问默认页面
            _homePageUrl = "local://default.html";

            // 初始化下载管理器
            InitializeDownloadManager();

            // 应用启动时自动加载local://协议内容
            LoadUrl(_homePageUrl);
        }

        private void LoadUrl(string url)
        {
            if (url.StartsWith("local://"))
            {
                var fileName = url.Substring("local://".Length);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "default.html";
                }
                
                // 统一使用GetLocalHtmlPath方法处理local://协议，确保一致的行为
                string filePath = GetLocalHtmlPath(fileName);
                
                // 如果是data URI，使用HtmlWebViewSource
                if (filePath.StartsWith("data:text/html"))
                {
                    // 解析data URI中的HTML内容
                    string htmlContent = Uri.UnescapeDataString(filePath.Substring(22)); // 跳过 "data:text/html;charset=utf-8,"
                    Browser.Source = new HtmlWebViewSource
                    {
                        Html = htmlContent
                    };
                }
                else
                {
                    // 否则使用UrlWebViewSource
                    Browser.Source = new UrlWebViewSource { Url = filePath };
                }
            }
            // 对于来自历史堆栈的URL，直接加载，不进行搜索处理
            else if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("file://") || url.StartsWith("data:text/html"))
            {
                Browser.Source = new UrlWebViewSource { Url = url };
            }
            else if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                // 如果不是有效的URL，尝试使用必应进行搜索
                url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(url);
                uri = new Uri(url);
                Browser.Source = new UrlWebViewSource { Url = url };
            }
            else
            {
                Browser.Source = new UrlWebViewSource { Url = url };
            }
            
            UrlEntry.Text = url;
            StatusLabel.Text = "正在加载: " + url;
        }

        private async void LoadLocalFile(string fileName)
        {
            await EnsureLocalHtmlFilesCopiedAsync();
            var localPath = Path.Combine(FileSystem.Current.AppDataDirectory, "Localfile", fileName);
            if (File.Exists(localPath))
            {
                Browser.Source = new UrlWebViewSource { Url = localPath };
            }
            else
            {
                // 文件不存在时显示错误信息
                Browser.Source = new HtmlWebViewSource 
                { 
                    Html = $"&lt;html&gt;&lt;body style='font-family: Segoe UI; padding: 20px;'&gt;&lt;h3&gt;文件未找到&lt;/h3&gt;&lt;p&gt;文件 {fileName} 不存在于本地文件目录中。&lt;/p&gt;&lt;/body&gt;&lt;/html&gt;" 
                };
            }
            
            UrlEntry.Text = Browser.Source.ToString();
            StatusLabel.Text = "加载完成";
        }

        private void OnUrlEntered(object sender, EventArgs e)
        {
            var url = UrlEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                string finalUrl = url;
                
                // 处理local://协议 - 指向程序文件夹下的Resources/Localfile目录
                if (url.StartsWith("local://"))
                {
                    var fileName = url.Substring("local://".Length);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = "default.html";
                    }
                    finalUrl = GetLocalHtmlPath(fileName);
                }
                // 如果输入的是搜索关键词而不是URL，则使用必应搜索
                else if (!url.Contains(".") && !url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://") && !url.StartsWith("local://"))
                {
                    finalUrl = "https://www.bing.com/search?q=" + Uri.EscapeDataString(url);
                }
                else if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://") && !url.StartsWith("local://"))
                {
                    finalUrl = "https://" + url;
                }
                
                // 保存当前URL到后退堆栈
                var currentUrl = Browser.Source?.ToString();
                _backStack.Push(!string.IsNullOrEmpty(currentUrl) ? currentUrl : (UrlEntry.Text ?? ""));
                _forwardStack.Clear();
                
                // 更新UI和执行导航
                UrlEntry.Text = url; // 显示原始输入的URL而非转换后的URL
                StatusLabel.Text = "正在加载: " + url;
                
                // 直接设置WebView的Source进行导航
                if (finalUrl.StartsWith("data:text/html"))
                {
                    // 解析data URI中的HTML内容
                    string htmlContent = Uri.UnescapeDataString(finalUrl.Substring(22)); // 跳过 "data:text/html;charset=utf-8,"
                    Browser.Source = new HtmlWebViewSource
                    {
                        Html = htmlContent
                    };
                }
                else
                {
                    Browser.Source = new UrlWebViewSource { Url = finalUrl };
                }
                
                UpdateNavigationButtons();
            }
        }

        private void OnBackClicked(object sender, EventArgs e)
        {
            if (_backStack.Count > 0)
            {
                // 保存当前URL到前进堆栈
                _forwardStack.Push(Browser.Source?.ToString() ?? UrlEntry.Text);
                var previousUrl = _backStack.Pop();
                // 直接使用原始URL进行导航，不经过搜索处理
                Browser.Source = new UrlWebViewSource { Url = previousUrl };
                UrlEntry.Text = previousUrl;
                StatusLabel.Text = "正在加载: " + previousUrl;
                UpdateNavigationButtons();
            }
        }

        private void OnForwardClicked(object sender, EventArgs e)
        {
            if (_forwardStack.Count > 0)
            {
                // 保存当前URL到后退堆栈
                _backStack.Push(Browser.Source?.ToString() ?? UrlEntry.Text);
                var nextUrl = _forwardStack.Pop();
                // 直接使用原始URL进行导航，不经过搜索处理
                Browser.Source = new UrlWebViewSource { Url = nextUrl };
                UrlEntry.Text = nextUrl;
                StatusLabel.Text = "正在加载: " + nextUrl;
                UpdateNavigationButtons();
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            Browser.Reload();
            StatusLabel.Text = "正在刷新页面...";
        }

        private void OnHomeClicked(object sender, EventArgs e)
        {
            _backStack.Push(Browser.Source?.ToString() ?? "");
            _forwardStack.Clear();
            LoadUrl(_homePageUrl);
            UpdateNavigationButtons();
        }

        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            // 显示下载选项菜单
            var action = await DisplayActionSheet("下载选项", "取消", null, 
                "下载当前页面", "更改下载目录", "打开下载文件夹");

            switch (action)
            {
                case "下载当前页面":
                    await DownloadCurrentPageAsync();
                    break;
                case "更改下载目录":
                    await ChangeDownloadDirectoryAsync();
                    break;
                case "打开下载文件夹":
                    OpenDownloadDirectory();
                    break;
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // 导航到设置页面
            await Shell.Current.GoToAsync("//SettingsPage");
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
        {
            StatusLabel.Text = "正在加载: " + e.Url;
        }

        private void OnNavigated(object sender, WebNavigatedEventArgs e)
        {
            StatusLabel.Text = "加载完成";
            UrlEntry.Text = e.Url;
            
            // 确保导航历史堆栈正确更新
            // 注意：这里不再向堆栈中添加内容，因为URL导航操作应该在各自的事件处理程序中管理历史
            
            // 更新导航按钮状态
            UpdateNavigationButtons();
        }

        private void UpdateNavigationButtons()
        {
            BackButton.IsEnabled = _backStack.Count > 0;
            ForwardButton.IsEnabled = _forwardStack.Count > 0;
        }

        private void InitializeDownloadManager()
        {
            // 设置默认下载目录，确保不会为null
            var defaultDir = DownloadManager.Instance.GetDefaultDownloadDirectory();
            _currentDownloadDirectory = !string.IsNullOrEmpty(defaultDir) ? defaultDir : FileSystem.AppDataDirectory;

            // 订阅下载事件
            DownloadManager.Instance.DownloadProgressChanged += OnDownloadProgressChanged;
            DownloadManager.Instance.DownloadCompleted += OnDownloadCompleted;
        }

        private void OnDownloadProgressChanged(object? sender, DownloadItem downloadItem)
        {
            // 在主线程更新UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusLabel.Text = $"下载中: {downloadItem.FileName} - {downloadItem.Progress:0}%";
            });
        }

        private void OnDownloadCompleted(object? sender, DownloadItem downloadItem)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (downloadItem.Status)
                {
                    case DownloadStatus.Completed:
                        StatusLabel.Text = $"下载完成: {downloadItem.FileName}";
                        ShowDownloadCompleteNotification(downloadItem);
                        break;
                    case DownloadStatus.Failed:
                        StatusLabel.Text = $"下载失败: {downloadItem.ErrorMessage}";
                        break;
                    case DownloadStatus.Cancelled:
                        StatusLabel.Text = "下载已取消";
                        break;
                }
            });
        }

        private async void ShowDownloadCompleteNotification(DownloadItem downloadItem)
        {
            // 显示下载完成通知
            await DisplayAlert("下载完成", $"文件 {downloadItem.FileName} 已下载完成\n保存位置: {downloadItem.DestinationPath}", "打开文件夹", "确定");
        }

        public async Task DownloadCurrentPageAsync()
        {
            var currentUrl = Browser.Source?.ToString();
            if (string.IsNullOrEmpty(currentUrl) || currentUrl.StartsWith("local://"))
            {
                await DisplayAlert("下载错误", "无法下载当前页面", "确定");
                return;
            }

            try
            {
                var downloadDir = _currentDownloadDirectory ?? DownloadManager.Instance.GetDefaultDownloadDirectory() ?? FileSystem.AppDataDirectory;
                await DownloadManager.Instance.DownloadFileAsync(currentUrl, downloadDir);
            }
            catch (Exception ex)
            {
                await DisplayAlert("下载错误", $"下载失败: {ex.Message}", "确定");
            }
        }

        public async Task DownloadFileAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                await DisplayAlert("下载错误", "下载链接无效", "确定");
                return;
            }

            try
            {
                var downloadDir = _currentDownloadDirectory ?? DownloadManager.Instance.GetDefaultDownloadDirectory() ?? FileSystem.AppDataDirectory;
                await DownloadManager.Instance.DownloadFileAsync(url, downloadDir);
            }
            catch (Exception ex)
            {
                await DisplayAlert("下载错误", $"下载失败: {ex.Message}", "确定");
            }
        }

        public async Task ChangeDownloadDirectoryAsync()
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "选择下载目录",
                FileTypes = null
            });

            if (result != null)
            {
                _currentDownloadDirectory = Path.GetDirectoryName(result.FullPath);
                await DisplayAlert("下载设置", $"下载目录已更改为: {_currentDownloadDirectory}", "确定");
            }
        }

        public void OpenDownloadDirectory()
        {
            try
            {
                var directory = _currentDownloadDirectory ?? DownloadManager.Instance.GetDefaultDownloadDirectory() ?? FileSystem.AppDataDirectory;
                if (Directory.Exists(directory))
                {
                    // 在文件资源管理器中打开目录
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("错误", $"无法打开下载目录: {ex.Message}", "确定");
            }
        }

        private string GetLocalHtmlPath(string fileName)
        {
            // 首先确保文件已复制
            EnsureLocalHtmlFilesCopiedAsync().Wait();
            
            // 检查文件是否已经复制到应用数据目录
            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Localfile", fileName);
            
            if (File.Exists(targetPath))
            {
                // 如果文件已复制，使用复制后的文件
                return $"file:///{targetPath.Replace("\\", "/")}";
            }
            else
            {
                // 如果文件未复制，尝试使用应用包内的文件
                try
                {
                    // 先检查应用包中是否存在该文件
                    var exists = FileSystem.OpenAppPackageFileAsync(fileName).Result != null;
                    if (exists)
                    {
                        return $"file:///{fileName}";
                    }
                }
                catch { }
                
                // 如果所有方法都失败，返回一个包含错误信息的HTML
                return "data:text/html;charset=utf-8," + Uri.EscapeDataString("<html><body style='font-family: Segoe UI; padding: 20px;'><h3>文件未找到</h3><p>无法访问本地文件: " + fileName + "</p></body></html>");
            }
        }

        private async Task EnsureLocalHtmlFilesCopiedAsync()
        {
            try
            {
                // 创建目标目录结构
                var targetDir = Path.Combine(FileSystem.AppDataDirectory, "Resources", "Localfile");
                Directory.CreateDirectory(targetDir);

                // 要复制的HTML文件列表
                var htmlFiles = new[] { "default.html", "test.html" };

                foreach (var file in htmlFiles)
                {
                    var targetPath = Path.Combine(targetDir, file);
                    
                    // 如果目标文件不存在，则从应用包复制
                    if (!File.Exists(targetPath))
                    {
                        using var sourceStream = await FileSystem.OpenAppPackageFileAsync(file);
                        using var targetStream = File.Create(targetPath);
                        await sourceStream.CopyToAsync(targetStream);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果复制失败，回退到使用根目录文件
                Console.WriteLine($"复制HTML文件失败: {ex.Message}");
            }
        }

        private void OnSearchClicked(object sender, EventArgs e)
        {
            // 这里可以实现搜索按钮的逻辑，例如触发地址栏的导航
            OnUrlEntered(UrlEntry, EventArgs.Empty);
        }
    }
}
