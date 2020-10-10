// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Context
{
    [Serializable]
    public class FunctionContext
    {
        public string SessionId { get; set; }

        public string InvocationId { get; set; }

        public DateTime FunctionStartDate { get; set; }

        public string CollectorType { get; set; }

        /// <summary>
        /// In case a function triggers another one, used to keep relation between multiple invocations.
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;

        /// <summary>
        /// For functions that are running on a schedule, represents the time the collection should have started.
        /// </summary>
        public DateTime SliceDate { get; set; }
    }
}
