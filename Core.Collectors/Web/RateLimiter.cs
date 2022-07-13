// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Cache;
using Microsoft.CloudMine.Core.Collectors.Utility;
using Microsoft.CloudMine.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public abstract class RateLimiter : IRateLimiter
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long? RateLimitOverride;

        private readonly ICache<RateLimitTableEntity> rateLimiterCache;
        protected ITelemetryClient TelemetryClient { get; private set; }
        private readonly bool expectRateLimitingHeaders;

        protected string OrganizationId { get; private set; }
        protected string OrganizationName { get; private set; }
        protected IDateTimeSystem DateTimeSystem { get; private set; }

        private readonly TimeSpan cacheInvalidationFrequency;
        private RateLimitTableEntity cachedResult;
        private DateTime cacheDateUtc;

        public const string RateLimitGlobalResource = "*";

        static RateLimiter()
        {
            string rateLimitOverride = Environment.GetEnvironmentVariable("RateLimitOverride");
            RateLimitOverride = string.IsNullOrWhiteSpace(rateLimitOverride) ? null : (long?)long.Parse(rateLimitOverride);
        }

        public RateLimiter(string organizationId,
                           string organizationName,
                           ICache<RateLimitTableEntity> rateLimiterCache,
                           ITelemetryClient telemetryClient,
                           bool expectRateLimitingHeaders,
                           TimeSpan cacheInvalidationFrequency,
                           IDateTimeSystem dateTimeSystem = null)
        {
            this.DateTimeSystem = dateTimeSystem ?? new DateTimeWrapper();

            this.OrganizationId = organizationId;
            this.OrganizationName = organizationName;
            this.rateLimiterCache = rateLimiterCache;
            this.TelemetryClient = telemetryClient;
            this.expectRateLimitingHeaders = expectRateLimitingHeaders;
            this.cacheInvalidationFrequency = cacheInvalidationFrequency;

            this.cachedResult = null;
            this.cacheDateUtc = DateTime.MinValue;
        }

        public async Task UpdateRetryAfterAsync(string identity, string requestUrl, HttpResponseMessage response)
        {
            HttpResponseHeaders responseHeaders = response.Headers;
            long rateLimitRemaining = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Remaining");
            long rateLimitLimit = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Limit");

            long retryAfter = GetRetryAfter(responseHeaders);
            DateTime? retryAfterDate = null;
            if (retryAfter != long.MinValue)
            {
                retryAfterDate = this.DateTimeSystem.UtcNow.AddSeconds(retryAfter);
            }

            long rateLimitReset = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Reset");
            DateTime? rateLimitResetDate = null;
            if (rateLimitReset != long.MinValue)
            {
                rateLimitResetDate = Epoch.AddSeconds(rateLimitReset);
            }

            // When this method is called, it is expected that some of the required headers (e.g., rate-limit limit) might be missing. 
            // Since the goal of this method is to update "Retry-After" column stored, permit those missing values and instead keep the existing values stored (if any).
            RateLimitTableEntity existingRecord = await this.rateLimiterCache.RetrieveAsync(new RateLimitTableEntity(identity, this.OrganizationId, this.OrganizationName)).ConfigureAwait(false);
            if (existingRecord == null)
            {
                // There is no record, bail out.
                return;
            }

            string rateLimitResource = GetRateLimitResource(responseHeaders);
            rateLimitRemaining = rateLimitRemaining == long.MinValue ? existingRecord.RateLimitRemaining : rateLimitRemaining;
            rateLimitLimit = rateLimitLimit == long.MinValue ? existingRecord.RateLimitLimit : rateLimitLimit;
            rateLimitResetDate = rateLimitResetDate == null ? existingRecord.RateLimitReset : rateLimitResetDate;

            this.cachedResult = new RateLimitTableEntity(identity, this.OrganizationId, this.OrganizationName, rateLimitLimit, rateLimitRemaining, rateLimitResetDate, retryAfterDate, rateLimitResource, response.StatusCode.ToString());
            await this.rateLimiterCache.CacheAsync(this.cachedResult).ConfigureAwait(false);
            this.cacheDateUtc = this.DateTimeSystem.UtcNow;
        }

        public async Task UpdateStatsAsync(string identity, string requestUrl, HttpResponseMessage response)
        {
            if (!(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified || response.StatusCode == HttpStatusCode.TooManyRequests))
            {
                // Don't attempt to extract out rate-limiting details for unsuccessful requests except 429s. Some (e.g., 404 might have it) but some (e.g., 502) does not have it.
                // These responses also should be very little compared to others, so we won't lose much.
                return;
            }

            HttpResponseHeaders responseHeaders = response.Headers;
            long rateLimitRemaining = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Remaining");
            // Discard the rate limit override value if the rate limit remaining is greater than the override value
            long rateLimitLimit = RateLimitOverride.HasValue && rateLimitRemaining <= RateLimitOverride ? RateLimitOverride.Value : GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Limit");

            long retryAfter = GetRetryAfter(responseHeaders);
            DateTime? retryAfterDate = null;
            if (retryAfter != long.MinValue)
            {
                retryAfterDate = this.DateTimeSystem.UtcNow.AddSeconds(retryAfter);
            }

            long rateLimitReset = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Reset");
            DateTime? rateLimitResetDate = null;
            if (rateLimitReset != long.MinValue)
            {
                rateLimitResetDate = Epoch.AddSeconds(rateLimitReset);
            }

            string rateLimitResource = GetRateLimitResource(responseHeaders);

            if (rateLimitLimit == long.MinValue || rateLimitRemaining == long.MinValue || rateLimitResetDate == DateTime.MinValue)
            {
                // The response does not include the required headers to update rate limiter stats. Potentially log this in telemetry and return.
                if (this.expectRateLimitingHeaders)
                {
                    string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Dictionary<string, string> properties = new Dictionary<string, string>()
                    {
                        { "RateLimitLimit", rateLimitLimit == long.MinValue ? "Missing" : rateLimitLimit.ToString() },
                        { "RateLimitRemaining", rateLimitRemaining == long.MinValue ? "Missing" : rateLimitRemaining.ToString() },
                        { "RateLimitReset", rateLimitResetDate == null ? "Missing" : $"{rateLimitResetDate:O}" },
                        { "RetryAfter", retryAfterDate == null ? "Missing" : $"{retryAfterDate:O}" },
                        { "ResponseStatusCode", response.StatusCode.ToString() },
                        { "ResponseContent", responseContent },
                        { "RequestUrl", requestUrl },
                    };

                    this.TelemetryClient.TrackEvent("RateLimiterError", properties);
                }

                return;
            }

            this.cachedResult = new RateLimitTableEntity(identity, this.OrganizationId, this.OrganizationName, rateLimitLimit, rateLimitRemaining, rateLimitResetDate, retryAfterDate, rateLimitResource, response.StatusCode.ToString());
            await this.rateLimiterCache.CacheAsync(this.cachedResult).ConfigureAwait(false);
            this.cacheDateUtc = this.DateTimeSystem.UtcNow;
        }

        public static long GetRetryAfter(HttpResponseHeaders responseHeaders)
        {
            return GetRateLimitHeaderValue(responseHeaders, "Retry-After");
        }

        public static long GetRateLimitHeaderValue(HttpResponseHeaders responseHeaders, string header)
        {
            long result = long.MinValue;
            if (responseHeaders.TryGetValues(header, out IEnumerable<string> resultValues))
            {
                result = long.Parse(resultValues.First());
            }
            return result;
        }

        public static string GetRateLimitResource(HttpResponseHeaders responseHeaders)
        {
            string result = RateLimitGlobalResource;
            if (responseHeaders.TryGetValues("X-RateLimit-Resource", out IEnumerable<string> resultValues))
            {
                result = resultValues.First();
            }
            return result;
        }

        public async Task WaitIfNeededAsync(IAuthentication authentication)
        {
            TimeSpan elapsedSinceLastLookup = this.DateTimeSystem.UtcNow - this.cacheDateUtc;
            if (this.cachedResult == null || elapsedSinceLastLookup >= this.cacheInvalidationFrequency)
            {
                this.cachedResult = await this.rateLimiterCache.RetrieveAsync(new RateLimitTableEntity(authentication.Identity, this.OrganizationId, this.OrganizationName)).ConfigureAwait(false);
                this.cacheDateUtc = this.DateTimeSystem.UtcNow;
            }

            if (this.cachedResult == null)
            {
                return;
            }

            await WaitIfNeededAsync(authentication, this.cachedResult).ConfigureAwait(false);
        }

        protected abstract Task WaitIfNeededAsync(IAuthentication authentication, RateLimitTableEntity tableEntity);
    }
}
