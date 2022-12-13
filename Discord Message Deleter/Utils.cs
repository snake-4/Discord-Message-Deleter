using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordMessageDeleter
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

                if ((int)response.StatusCode == 429)
                {
                    //int rateLimitResetTime = int.Parse(response.Headers.GetValues("X-RateLimit-Reset-After").First().Replace(".", ""));
                    int rateLimitResetTime = int.Parse(response.Headers.GetValues("Retry-After").First());

                    rateLimitCallback?.Invoke(rateLimitResetTime);
                    await Task.Delay(TimeSpan.FromSeconds(rateLimitResetTime), ct);
                }
                else if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                {
                    //Try again on 500 errors
                    continue;
                }
                else
                {
                    return response;
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

        public static void InvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action)
        {
            if (obj.InvokeRequired)
            {
                var args = new object[0];
                obj.Invoke(action, args);
            }
            else
            {
                action();
            }
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
