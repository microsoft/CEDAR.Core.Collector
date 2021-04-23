// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public class AsyncTaskExecutor
    {
        private readonly int maxBatchSize;
        private readonly List<Task> tasks;

        public AsyncTaskExecutor(int maxBatchSize)
        {
            this.maxBatchSize = maxBatchSize;
        }

        public void Add(Task task)
        {
            this.tasks.Add(task);
        }

        public async Task WaitIfNeededAsync()
        {
            if (tasks.Count >= maxBatchSize)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
                tasks.Clear();
            }
        }
    }
}
