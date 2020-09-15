// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.IO
{
    public interface IQueue
    {
        Task PutObjectAsJsonStringAsync(object obj);
        Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive);
        Task PutMessageAsync(string message);
        Task PutMessageAsync(string message, TimeSpan timeToLive);
    }
}
