// Copyright (c) Microsoft Corporation. All rights reserved.

using Microsoft.CloudMine.Core.Collectors.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Tests.IO
{
    public class InMemoryRecordWriter : IRecordWriter
    {
        private readonly List<Tuple<JObject, RecordContext>> records;

        public IEnumerable<string> OutputPaths => throw new NotImplementedException();

        public InMemoryRecordWriter()
        {
            this.records = new List<Tuple<JObject, RecordContext>>();
        }

        public void Clear()
        {
            this.records.Clear();
        }

        public List<Tuple<JObject, RecordContext>> GetRecords()
        {
            return new List<Tuple<JObject, RecordContext>>(this.records);
        }

        public void Dispose()
        {
            // Assume successful.
        }

        public Task FinalizeAsync()
        {
            // Assume successful.
            return Task.CompletedTask;
        }

        public Task NewOutputAsync(string outputSuffix, int fileIndex = 0)
        {
            throw new NotImplementedException();
        }

        public void SetOutputPathPrefix(string outputPathPrefix)
        {
            throw new NotImplementedException();
        }

        public Task WriteRecordAsync(JObject record, RecordContext context)
        {
            this.records.Add(Tuple.Create(record, context));
            return Task.CompletedTask;
        }
    }
}
