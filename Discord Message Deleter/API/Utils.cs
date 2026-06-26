using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMessageDeleter.API;

public static class Utils
{
    public delegate void RateLimitCallbackDelegate(TimeSpan rateLimitTime);

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan delta)
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;

        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryAfterDate)
            return retryAfterDate > DateTimeOffset.UtcNow ? retryAfterDate - DateTimeOffset.UtcNow : TimeSpan.Zero;

        if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues) &&
            double.TryParse(retryAfterValues.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var retryAfterSeconds))
        {
            return retryAfterSeconds > 0 ? TimeSpan.FromSeconds(retryAfterSeconds) : TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(1);
    }

    public static async Task<HttpResponseMessage> HttpRequest(HttpClient httpClient, Func<HttpRequestMessage> requestFactory,
        RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        while (true)
        {
            HttpResponseMessage response;
            try
            {
                using var request = requestFactory();
                response = await httpClient.SendAsync(request, ct);
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                continue;
            }

            if ((int)response.StatusCode == 429)
            {
                var retryAfter = GetRetryDelay(response);

                rateLimitCallback?.Invoke(retryAfter);
                response.Dispose();
                await Task.Delay(retryAfter, ct);
            }
            else if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
            {
                response.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
                continue;
            }
            else
            {
                return response;
            }
        }
    }

    public static async Task<string> HttpGetString(HttpClient httpClient, string uri,
        RateLimitCallbackDelegate? rateLimitCallback = null, CancellationToken ct = default)
    {
        using var response = await HttpRequest(httpClient, () => new HttpRequestMessage(HttpMethod.Get, uri), rateLimitCallback, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }
}
