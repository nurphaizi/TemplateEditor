using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows.Media.Imaging;
using Serilog;

namespace TemplateEdit
{

    public class ImagePipelineLoader
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly SemaphoreSlim _downloadLimiter = new SemaphoreSlim(5); // max parallel downloads
        private readonly string _cacheFolder;
        private readonly MemoryImageCache _memoryCache = new MemoryImageCache(30);
        public ImagePipelineLoader()
        {
            _cacheFolder = Path.Combine(Path.GetTempPath(), "PrintImageCache");
            Directory.CreateDirectory(_cacheFolder);
        }

        public async Task<BitmapImage> LoadForPrintAsync(string source, int decodeWidth = 1600)
        {
            var cached = _memoryCache.Get(source);
            if (cached != null)
                return cached;

            string localPath = IsRemoteUri(source)
                  ? await DownloadToCacheAsync(source)
                  : NormalizeLocalPath(source);
            var bitmap = LoadBitmap(localPath, decodeWidth);
            _memoryCache.Add(source, bitmap);
            return bitmap;
        }

        private bool IsRemoteUri(string source)
        {
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps;
        }

        private string NormalizeLocalPath(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
                return uri.LocalPath;

            return Path.GetFullPath(path);
        }

        private async Task<string> DownloadToCacheAsync(string url)
        {
            await _downloadLimiter.WaitAsync();
            try
            {
                var uri = new Uri(url);
                string safeFileName = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(url));
                string filePath = Path.Combine(_cacheFolder, safeFileName);

                if (!File.Exists(filePath))
                {
                    var bytes = await _http.GetByteArrayAsync(uri);
                    Log.Information($"Downloaded {url} to cache with size {bytes.Length} bytes");
                    await File.WriteAllBytesAsync(filePath, bytes);
                }
                return filePath;
            }
            finally
            {
                _downloadLimiter.Release();
            }
        }
        public static byte[] BitmapImageToBytes(BitmapImage bitmap)
        {
            if (bitmap == null)
                return null;
            if (bitmap.CanFreeze && !bitmap.IsFrozen)
            {
                bitmap.Freeze();
            }
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
        private BitmapImage LoadBitmap(string path, int decodeWidth)
        {
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.DecodePixelWidth = decodeWidth; // critical for memory control
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
