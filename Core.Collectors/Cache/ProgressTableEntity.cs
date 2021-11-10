// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Cache
{
    public class ProgressTableEntity : TableEntityWithContext
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public string IdentifierPrefix { get; set; }
        public string ProgressIdentifier { get; set; }
        public TimeSpan CollectionFrequency { get; set; }
        public string SessionId { get; set; }
        public bool Succeeded { get; set; }

        // Used for serialization.
        public ProgressTableEntity()
        {
        }

        // Used only to retrieve (lookup) entries.
        public ProgressTableEntity(DateTime startDateUtc, string identifierPrefix, string endpointName, TimeSpan collectionFrequency)
            : this(startDateUtc, identifierPrefix, endpointName, collectionFrequency, sessionId: string.Empty, succeeded: false)
        {
        }

        public ProgressTableEntity(DateTime startDateUtc, string identifierPrefix, string progressIdentifier, TimeSpan collectionFrequency, string sessionId, bool succeeded)
        {
            DateTime endDateUtc = startDateUtc.Add(collectionFrequency);
            this.PartitionKey = GetPartitionKey(startDateUtc, collectionFrequency);
            this.RowKey = GetRowKey(identifierPrefix, progressIdentifier);

            this.StartDateUtc = startDateUtc;
            this.EndDateUtc = endDateUtc;
            this.IdentifierPrefix = identifierPrefix;
            this.ProgressIdentifier = progressIdentifier;
            this.CollectionFrequency = collectionFrequency;
            this.SessionId = sessionId;
            this.Succeeded = succeeded;

            this.AddContext("StartDateUtc", $"{this.StartDateUtc:O}");
            this.AddContext("IdentifierPrefix", this.IdentifierPrefix);
            this.AddContext("ProgressIdentifier", this.ProgressIdentifier);
            this.AddContext("CollectionFrequency", this.CollectionFrequency.ToString());
            this.AddContext("SessionId", this.SessionId);
            this.AddContext("Succeeded", this.Succeeded.ToString());
        }

        public static string GetPartitionKey(DateTime startDateUtc, TimeSpan collectionFrequency)
        {
            DateTime endDateUtc = startDateUtc.Add(collectionFrequency);
            return $"{startDateUtc:O}_{endDateUtc:O}";
        }

        public static string GetRowKey(string identifierPrefix, string progressIdentifier)
        {
            return $"{identifierPrefix}_{progressIdentifier}";
        }
    }
}
