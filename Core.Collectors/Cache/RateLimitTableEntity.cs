// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;

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
        public string Resource { get; set; }
        public string StatusCode { get; set; }

        // Used for serialization.
        public RateLimitTableEntity()
        { 
        }

        // Used only to retrieve (lookup) entries.
        public RateLimitTableEntity(string identity, string organizationId, string organizationName)
            : this(identity, organizationId, organizationName, rateLimitLimit: long.MinValue, rateLimitRemaining: long.MinValue, rateLimitReset: null, retryAfter: null, resource: null, statusCode: HttpStatusCode.OK.ToString())
        {
        }

        // Used only to retrieve (lookup) entries.
        public RateLimitTableEntity(string identity, string organizationId, string organizationName, string resource)
            : this(identity, organizationId, organizationName, rateLimitLimit: long.MinValue, rateLimitRemaining: long.MinValue, rateLimitReset: null, retryAfter: null, resource, statusCode: HttpStatusCode.OK.ToString())
        {
        }

        // Used for backwards-compatibility purposes.
        public RateLimitTableEntity(string identity, string organizationId, string organizationName, long rateLimitLimit, long rateLimitRemaining, DateTime? rateLimitReset, DateTime? retryAfter)
            : this(identity, organizationId, organizationName, rateLimitLimit, rateLimitRemaining, rateLimitReset, retryAfter, resource: null, statusCode: HttpStatusCode.OK.ToString())
        {
        }

        public RateLimitTableEntity(string identity, string organizationId, string organizationName, long rateLimitLimit, long rateLimitRemaining, DateTime? rateLimitReset, DateTime? retryAfter, string resource, string statusCode)
        {
            this.PartitionKey = identity;
            this.RowKey = GetRowKey(organizationId, resource);

            this.Identity = identity;
            this.OrganizationIdString = organizationId;
            this.OrganizationName = organizationName;
            this.RateLimitLimit = rateLimitLimit;
            this.RateLimitRemaining = rateLimitRemaining;
            this.RateLimitReset = rateLimitReset;
            this.RetryAfter = retryAfter;
            this.Resource = resource;
            this.StatusCode = statusCode;

            this.AddContext("Identity", this.Identity);
            this.AddContext("OrganizationIdString", this.OrganizationIdString);
            this.AddContext("OrganizationName", this.OrganizationName);
            this.AddContext("RateLimitLimit", this.RateLimitLimit.ToString());
            this.AddContext("RateLimitRemaining", this.RateLimitRemaining.ToString());
            this.AddContext("RateLimitReset", $"{this.RateLimitReset:O}");
            this.AddContext("RetryAfter", $"{this.RetryAfter:O}");
            this.AddContext("Resource", this.Resource);
            this.AddContext("StatusCode", this.StatusCode);
        }

        public static string GetRowKey(string organizationId, string resource)
        {
            return string.IsNullOrWhiteSpace(resource) ? organizationId : $"{organizationId}_{resource}";
        }
    }
}
