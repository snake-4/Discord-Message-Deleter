using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Discord_Delete_Messages
{
    public static class Utils
    {
        public delegate void RateLimitCallbackDelegate(int rateLimitTimeInSeconds);

        public static async Task<HttpResponseMessage> HttpRequestAndWaitRatelimit(HttpClient httpClient, HttpRequestMessage request,
            RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            //request.Headers.Add("X-RateLimit-Precision", "millisecond");
            while (true)
            {
                HttpResponseMessage response;
                try
                {
                    request = await request.CloneAsync();
                    response = await httpClient.SendAsync(request, ct);
                }
                catch (HttpRequestException)
                {
                    //Will try again on connection errors
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else if ((int)response.StatusCode == 429)
                {
                    //int rateLimitResetTime = int.Parse(response.Headers.GetValues("X-RateLimit-Reset-After").First().Replace(".", ""));
                    int rateLimitResetTime = int.Parse(response.Headers.GetValues("Retry-After").First());

                    rateLimitCallback?.Invoke(rateLimitResetTime);
                    await Task.Delay(TimeSpan.FromSeconds(rateLimitResetTime), ct);
                }
                else
                {
                    string excStr = "Unexpected response in HttpRequestAndWaitRatelimit: response.StatusCode is " + response.StatusCode;
                    if (response.Headers.TryGetValues("X-RateLimit-Remaining", out IEnumerable<string> XRateLimitRemainingValues))
                    {
                        excStr += " X-RateLimit-Remaining is " + XRateLimitRemainingValues.First();
                    }
                    string body = null;
                    if ((body = await response.Content.ReadAsStringAsync()) != null)
                    {
                        excStr += " response.Content is " + body;
                    }
                    throw new Exception(excStr);
                }
            }
        }

        public static async Task<string> HttpGetStringAndWaitRatelimit(HttpClient httpClient, Uri uri,
            RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return await (await HttpRequestAndWaitRatelimit(httpClient,
                new HttpRequestMessage(HttpMethod.Get, uri), rateLimitCallback, ct)).Content.ReadAsStringAsync();
        }

        public static async Task<string> HttpGetStringAndWaitRatelimit(HttpClient httpClient, string uri,
            RateLimitCallbackDelegate rateLimitCallback = null, CancellationToken ct = default)
        {
            return await HttpGetStringAndWaitRatelimit(httpClient, new Uri(uri), rateLimitCallback, ct);
        }

        //https://stackoverflow.com/a/3822913
        public static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static string ChromiumLevelDBReadString(LevelDB.DB database, string url, string keyName)
        {
            var rawKeyName = $"_{url}\0\u0001{keyName}";
            return database.Get(rawKeyName).Replace("\u0001", "").TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
        }

        public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await request.Content.CloneAsync().ConfigureAwait(false),
                Version = request.Version
            };
            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        public static async Task<HttpContent> CloneAsync(this HttpContent content)
        {
            if (content == null) return null;

            var ms = new MemoryStream();
            await content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;

            var clone = new StreamContent(ms);
            foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }
            return clone;
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            GeneralPropertyComparer<T, TKey> comparer = new GeneralPropertyComparer<T, TKey>(property);
            return items.Distinct(comparer);
        }

        internal class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
        {
            private Func<T, TKey> expr { get; set; }
            internal GeneralPropertyComparer(Func<T, TKey> expr)
            {
                this.expr = expr;
            }
            public bool Equals(T left, T right)
            {
                var leftProp = expr.Invoke(left);
                var rightProp = expr.Invoke(right);
                if (leftProp == null && rightProp == null)
                    return true;
                else if (leftProp == null ^ rightProp == null)
                    return false;
                else
                    return leftProp.Equals(rightProp);
            }
            public int GetHashCode(T obj)
            {
                var prop = expr.Invoke(obj);
                return (prop == null) ? 0 : prop.GetHashCode();
            }
        }
    }
}
