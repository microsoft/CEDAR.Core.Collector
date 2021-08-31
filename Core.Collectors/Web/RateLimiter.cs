// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Authentication;
using Microsoft.CloudMine.Core.Collectors.Cache;
using Microsoft.CloudMine.Core.Collectors.Telemetry;
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

        private TimeSpan cacheInvalidationFrequency;
        private RateLimitTableEntity cachedResult;
        private DateTime cacheDateUtc;

        static RateLimiter()
        {
            string rateLimitOverride = Environment.GetEnvironmentVariable("RateLimitOverride");
            RateLimitOverride = string.IsNullOrWhiteSpace(rateLimitOverride) ? null : (long?)long.Parse(rateLimitOverride);
        }

        public RateLimiter(string organizationId,
                           string organizationName,
                           ICache<RateLimitTableEntity> rateLimiterCache,
                           ITelemetryClient telemetryClient,
                           bool expectRateLimitingHeaders)
            : this(organizationId, organizationName, rateLimiterCache, telemetryClient, expectRateLimitingHeaders, cacheInvalidationFrequency: TimeSpan.FromTicks(0))
        {
        }

        public RateLimiter(string organizationId,
                           string organizationName,
                           ICache<RateLimitTableEntity> rateLimiterCache,
                           ITelemetryClient telemetryClient,
                           bool expectRateLimitingHeaders,
                           TimeSpan cacheInvalidationFrequency)
        {
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
                retryAfterDate = DateTime.UtcNow.AddSeconds(retryAfter);
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

            rateLimitRemaining = rateLimitRemaining == long.MinValue ? existingRecord.RateLimitRemaining : rateLimitRemaining;
            rateLimitLimit = rateLimitLimit == long.MinValue ? existingRecord.RateLimitLimit : rateLimitLimit;
            rateLimitResetDate = rateLimitResetDate == null ? existingRecord.RateLimitReset : rateLimitResetDate;

            this.cachedResult = new RateLimitTableEntity(identity, this.OrganizationId, this.OrganizationName, rateLimitLimit, rateLimitRemaining, rateLimitResetDate, retryAfterDate);
            await this.rateLimiterCache.CacheAsync(this.cachedResult).ConfigureAwait(false);
            this.cacheDateUtc = DateTime.UtcNow;
        }

        public async Task UpdateStatsAsync(string identity, string requestUrl, HttpResponseMessage response)
        {
            if (!(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified))
            {
                // Don't attempt to extract out rate-limiting details for unsuccessful requests. Some (e.g., 404 might have it) but some (e.g., 502) does not have it.
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
                retryAfterDate = DateTime.UtcNow.AddSeconds(retryAfter);
            }

            long rateLimitReset = GetRateLimitHeaderValue(responseHeaders, "X-RateLimit-Reset");
            DateTime? rateLimitResetDate = null;
            if (rateLimitReset != long.MinValue)
            {
                rateLimitResetDate = Epoch.AddSeconds(rateLimitReset);
            }

            if (rateLimitLimit == long.MinValue || rateLimitRemaining == long.MinValue || rateLimitResetDate == DateTime.MinValue)
            {
                // The response does not include the required headers to update rate limiter stats. Potentially log this in telemetry and return.
                if (expectRateLimitingHeaders)
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

            await this.rateLimiterCache.CacheAsync(new RateLimitTableEntity(identity, this.OrganizationId, this.OrganizationName, rateLimitLimit, rateLimitRemaining, rateLimitResetDate, retryAfterDate)).ConfigureAwait(false);
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

        protected async Task<RateLimitTableEntity> GetTableEntity(IAuthentication authentication)
        {
            TimeSpan elapsedSinceLastLookup = DateTime.UtcNow - this.cacheDateUtc;
            if (this.cachedResult == null || elapsedSinceLastLookup >= cacheInvalidationFrequency)
            {
                this.cachedResult = await this.rateLimiterCache.RetrieveAsync(new RateLimitTableEntity(authentication.Identity, this.OrganizationId, this.OrganizationName)).ConfigureAwait(false);
                this.cacheDateUtc = DateTime.UtcNow;
            }
            return this.cachedResult;
        }

        public async Task WaitIfNeededAsync(IAuthentication authentication)
        {
            await this.GetTableEntity(authentication).ConfigureAwait(false);
            if (this.cachedResult == null)
            {
                return;
            }

            await WaitIfNeededAsync(authentication, this.cachedResult).ConfigureAwait(false);
        }

        protected abstract Task WaitIfNeededAsync(IAuthentication authentication, RateLimitTableEntity tableEntity);
    }
}
