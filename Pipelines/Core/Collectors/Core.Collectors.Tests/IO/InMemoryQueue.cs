// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Tests.IO
{
    public class InMemoryQueue : IQueue
    {
        private readonly List<string> messages;

        public InMemoryQueue()
        {
            this.messages = new List<string>();
        }

        public Task PutMessageAsync(string message)
        {
            this.messages.Add(message);
            return Task.CompletedTask;
        }

        public Task PutObjectAsJsonStringAsync(object obj)
        {
            string message = JsonConvert.SerializeObject(obj);
            return this.PutMessageAsync(message);
        }

        public List<string> GetMessages()
        {
            return new List<string>(this.messages);
        }

        public Task PutMessageAsync(string message, TimeSpan timeToLive)
        {
            return this.PutMessageAsync(message);
        }

        public Task PutObjectAsJsonStringAsync(object obj, TimeSpan timeToLive)
        {
            return this.PutObjectAsJsonStringAsync(obj);
        }
    }
}
