using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using System.Collections.Generic;

namespace BuraunzaApp
{
    public class DownloadItem
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long DownloadedBytes { get; set; }
        public DownloadStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;
    }

    public enum DownloadStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed,
        Cancelled
    }

    public class DownloadManager
    {
        private static DownloadManager? _instance;
        public static DownloadManager Instance => _instance ??= new DownloadManager();

        public List<DownloadItem> Downloads { get; } = new List<DownloadItem>();
        public event EventHandler<DownloadItem>? DownloadProgressChanged;
        public event EventHandler<DownloadItem>? DownloadCompleted;

        private DownloadManager() { }

        static DownloadManager()
        {
            _instance = new DownloadManager();
        }

        public string GetDefaultDownloadDirectory()
        {
            // 首先检查设置中是否有自定义下载目录
            var settings = SettingsPage.GetCurrentSettings();
            if (!string.IsNullOrEmpty(settings.DownloadDirectory) && Directory.Exists(settings.DownloadDirectory))
            {
                return settings.DownloadDirectory;
            }
            
            // 如果没有自定义目录，使用系统默认下载目录
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                // Windows: 使用用户下载文件夹
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else if (DeviceInfo.Platform == DevicePlatform.MacCatalyst)
            {
                // macOS: 使用用户下载文件夹
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
            else
            {
                // 其他平台使用应用数据目录
                return FileSystem.AppDataDirectory;
            }
        }

        public async Task<DownloadItem> DownloadFileAsync(string url, string? customDirectory = null)
        {
            var downloadItem = new DownloadItem
            {
                Url = url ?? throw new ArgumentNullException(nameof(url)),
                FileName = Path.GetFileName(url)! ?? "download.bin",
                StartTime = DateTime.Now,
                Status = DownloadStatus.Pending
            };

            // 确定下载目录
            var downloadDir = customDirectory ?? GetDefaultDownloadDirectory();
            Directory.CreateDirectory(downloadDir);

            // 确保文件名唯一
            var baseName = Path.GetFileNameWithoutExtension(downloadItem.FileName);
            var extension = Path.GetExtension(downloadItem.FileName);
            var counter = 1;
            var finalPath = Path.Combine(downloadDir, downloadItem.FileName);

            while (File.Exists(finalPath))
            {
                downloadItem.FileName = $"{baseName} ({counter}){extension}";
                finalPath = Path.Combine(downloadDir, downloadItem.FileName);
                counter++;
            }

            downloadItem.DestinationPath = finalPath;
            Downloads.Add(downloadItem);

            try
            {
                downloadItem.Status = DownloadStatus.Downloading;
                DownloadProgressChanged?.Invoke(this, downloadItem);

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);

                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                downloadItem.TotalBytes = response.Content.Headers.ContentLength ?? -1;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;
                var totalRead = 0L;

                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                    downloadItem.DownloadedBytes = totalRead;
                    DownloadProgressChanged?.Invoke(this, downloadItem);
                }

                downloadItem.Status = DownloadStatus.Completed;
                downloadItem.EndTime = DateTime.Now;
                DownloadCompleted?.Invoke(this, downloadItem);

                return downloadItem;
            }
            catch (Exception ex)
            {
                downloadItem.Status = DownloadStatus.Failed;
                downloadItem.ErrorMessage = ex.Message;
                downloadItem.EndTime = DateTime.Now;
                DownloadCompleted?.Invoke(this, downloadItem);

                // 清理部分下载的文件
                if (File.Exists(finalPath))
                {
                    try { File.Delete(finalPath); } catch { }
                }

                throw new DownloadException("文件下载失败", ex);
            }
        }

        public void CancelDownload(DownloadItem downloadItem)
        {
            if (downloadItem.Status == DownloadStatus.Downloading)
            {
                downloadItem.Status = DownloadStatus.Cancelled;
                downloadItem.EndTime = DateTime.Now;
                DownloadCompleted?.Invoke(this, downloadItem);
            }
        }

        public void ClearCompletedDownloads()
        {
            Downloads.RemoveAll(item => item.Status == DownloadStatus.Completed || 
                                       item.Status == DownloadStatus.Failed || 
                                       item.Status == DownloadStatus.Cancelled);
        }

        public string GetHumanReadableSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class DownloadException : Exception
    {
        public DownloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}