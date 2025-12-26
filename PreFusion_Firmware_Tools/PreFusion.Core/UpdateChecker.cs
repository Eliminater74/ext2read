using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PreFusion.Core
{
    public class ReleaseInfo
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("assets")]
        public List<ReleaseAsset> Assets { get; set; }
    }

    public class ReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string DownloadUrl { get; set; }
    }

    public static class UpdateChecker
    {
        private const string RepoOwner = "Eliminater74";
        private const string RepoName = "PreFusion-Firmware-Tools";
        private const string UserAgent = "PreFusion-Updater";

        public static async Task<ReleaseInfo?> CheckForUpdateAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                string url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                var response = await client.GetAsync(url);
                
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<ReleaseInfo>(json);

                if (release == null) return null;

                // Compare versions
                // Tag format usually "v1.0.0" -> strip 'v'
                string remoteVerStr = release.TagName.TrimStart('v');
                if (Version.TryParse(remoteVerStr, out Version remoteVer))
                {
                    var currentVer = Assembly.GetEntryAssembly()?.GetName().Version;
                    if (currentVer != null && remoteVer > currentVer)
                    {
                        return release;
                    }
                }
                
                return null; // Up to date
            }
            catch (Exception)
            {
                return null; // Network error or API limit
            }
        }

        public static async Task DownloadAndInstallAsync(string downloadUrl, IProgress<float> progress = null)
        {
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "PreFusion_Setup.exe");
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    if (totalBytes.HasValue && progress != null)
                    {
                        progress.Report((float)totalRead / totalBytes.Value);
                    }
                }

                // Run Installer
                Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                throw new Exception("Download failed: " + ex.Message);
            }
        }
    }
}
