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
            tasks = new List<Task>();
        }

        public void Add(Task task)
        {
            this.tasks.Add(task);
        }

        public Task WaitIfNeededAsync()
        {
            if (tasks.Count >= maxBatchSize)
            {
                return WaitAsync();
            }

            return Task.CompletedTask;
        }

        public async Task WaitAsync()
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Clear();
        }
    }

    public class AsyncTaskExecutor<T>
    {
        private readonly int maxBatchSize;
        private readonly List<Task<T>> tasks;

        public AsyncTaskExecutor(int maxBatchSize)
        {
            this.maxBatchSize = maxBatchSize;
            this.tasks = new List<Task<T>>();
        }

        public void Add(Task<T> task)
        {
            this.tasks.Add(task);
        }

        public Task<T[]> WaitIfNeededAsync()
        {
            if (tasks.Count >= maxBatchSize)
            {
                return WaitAsync();
            }

            return Task.FromResult(new T[0]);
        }

        public async Task<T[]> WaitAsync()
        {
            T[] result = await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Clear();
            return result;
        }
    }
}
