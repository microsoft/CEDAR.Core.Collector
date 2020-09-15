// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class RateLimitTableEntity : TableEntityWithContext
    {
        public const string GlobalOrganizationId = "*";

        public string Identity { get; set; }
        public string OrganizationIdString { get; set; }
        public string OrganizationName { get; set; }
        public long RateLimitLimit { get; set; }
        public long RateLimitRemaining { get; set; }
        public DateTime? RateLimitReset { get; set; }
        public DateTime? RetryAfter { get; set; }

        // Used for serialization.
        public RateLimitTableEntity()
        { 
        }

        // Used only to retrieve (lookup) entries.
        public RateLimitTableEntity(string identity, string organizationId, string organizationName)
            : this(identity, organizationId, organizationName, rateLimitLimit: long.MinValue, rateLimitRemaining: long.MinValue, rateLimitReset: null, retryAfter: null)
        {
        }

        public RateLimitTableEntity(string identity, string organizationId, string organizationName, long rateLimitLimit, long rateLimitRemaining, DateTime? rateLimitReset, DateTime? retryAfter)
        {
            this.PartitionKey = identity;
            this.RowKey = organizationId;

            this.Identity = identity;
            this.OrganizationIdString = organizationId;
            this.OrganizationName = organizationName;
            this.RateLimitLimit = rateLimitLimit;
            this.RateLimitRemaining = rateLimitRemaining;
            this.RateLimitReset = rateLimitReset;
            this.RetryAfter = retryAfter;

            this.AddContext("Identity", this.Identity);
            this.AddContext("OrganizationIdString", this.OrganizationIdString);
            this.AddContext("OrganizationName", this.OrganizationName);
            this.AddContext("RateLimitLimit", this.RateLimitLimit.ToString());
            this.AddContext("RateLimitRemaining", this.RateLimitRemaining.ToString());
            this.AddContext("RateLimitReset", $"{this.RateLimitReset:O}");
            this.AddContext("RetryAfter", $"{this.RetryAfter:O}");
        }
    }
}
