using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Andoromeda.Framework.GitHub
{
    public static class GitHubSynchronizer
    {
        private static HttpClient client = new HttpClient() { BaseAddress = new Uri("https://github.com") };
        private static Regex commitRegex = new Regex(@"(?<=class=""message js-navigation-open"" data-pjax=""true"" href=""/[a-zA-Z0-9-_+ ]{1,}\/[a-zA-Z0-9-_+ ]{1,}\/commit\/)[a-f0-9]{40,40}");

        public static async Task<string> GetRepositoryLatestHashAsync(string user, string repo, string branch)
        {
            using (var response = await client.GetAsync($"/{user}/{repo}/commits/{branch}"))
            {
                var html = await response.Content.ReadAsStringAsync();
                var matches = commitRegex.Matches(html);

                if (matches.Count == 0)
                {
                    return null;
                }

                return matches[0].Value;
            }
        }

        public static async Task CreateOrUpdateRepositoryAsync(string user, string repo, string branch, string dst)
        {
            var latestHash = await GetRepositoryLatestHashAsync(user, repo, branch);

            // Check the current hash if the update is not needed.
            if (Directory.Exists(dst) && File.Exists(Path.Combine(dst, "hash.lock")))
            {
                var currentHash = File.ReadAllText(Path.Combine(dst, "hash.lock"));
                if (currentHash == latestHash)
                {
                    return;
                }
            }

            var stamp = DateTime.UtcNow.Ticks;
            await DownloadRepositoryZipAsync(user, repo, branch, dst + '-' + stamp);
            File.WriteAllText(Path.Combine(dst + '-' + stamp, "hash.lock"), latestHash);

            if (Directory.Exists(dst)) 
            {
                Directory.Move(dst, dst + $"-{stamp}-bak");
            }

            Directory.Move(dst + '-' + stamp, dst);

            if (Directory.Exists(dst + $"-{stamp}-bak"))
            {
                Directory.Delete(dst + $"-{stamp}-bak", true);
            }
        }

        private static async Task DownloadRepositoryZipAsync(string user, string repo, string branch, string dst)
        {
            using (var response = await client.GetAsync($"/{user}/{repo}/archive/{branch}.zip"))
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    ExtractAll(stream, dst);
                }
            }
        }

        public static void ExtractAll(Stream stream, string dest)
        {
            using (var archive = new ZipArchive(stream))
            {
                foreach (var x in archive.Entries)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(dest + x.FullName)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dest + x.FullName));
                    if (x.Length == 0 && string.IsNullOrEmpty(Path.GetExtension(x.FullName)))
                        continue;
                    using (var entryStream = x.Open())
                    using (var destStream = File.OpenWrite(dest + x.FullName))
                    {
                        entryStream.CopyTo(destStream);
                    }
                }
            }
        }
    }
}
