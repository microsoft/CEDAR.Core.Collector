// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Collector.Tests
{
    [TestClass]
    public class CachingCollectorTests
    {
        [TestMethod]
        public void ShallIgnoreCache()
        {
            bool ignoreCache = true;
            bool scheduled = true;
            DateTime utcNow = DateTime.Parse("2022-05-31T05:30:00");
            DateTime sliceEndDate = DateTime.Parse("2022-05-31T05:00:00");
            TimeSpan sliceFrequency = TimeSpan.FromHours(1);

            // Ignore cache if ignoreCache = true.
            bool actual = CachingCollectorUtils.ShallIgnoreCache(ignoreCache, scheduled, utcNow, sliceEndDate, sliceFrequency);
            Assert.IsTrue(actual);

            ignoreCache = false;
            scheduled = false;
            utcNow = DateTime.Parse("2022-05-31T05:30:00");
            sliceEndDate = DateTime.Parse("2022-05-31T05:00:00");
            sliceFrequency = TimeSpan.FromHours(1);

            // Look-up cache if neither scheduled or ignoreCache
            actual = CachingCollectorUtils.ShallIgnoreCache(ignoreCache, scheduled, utcNow, sliceEndDate, sliceFrequency);
            Assert.IsFalse(actual);

            ignoreCache = false;
            scheduled = true;
            utcNow = DateTime.Parse("2022-05-31T05:30:00");
            sliceEndDate = DateTime.Parse("2022-05-31T00:00:00");
            sliceFrequency = TimeSpan.FromHours(1);

            // Look-up cache if scheduled but old
            actual = CachingCollectorUtils.ShallIgnoreCache(ignoreCache, scheduled, utcNow, sliceEndDate, sliceFrequency);
            Assert.IsFalse(actual);

            ignoreCache = false;
            scheduled = true;
            utcNow = DateTime.Parse("2022-05-31T05:30:00");
            sliceEndDate = DateTime.Parse("2022-05-31T05:00:00");
            sliceFrequency = TimeSpan.FromHours(1);

            // Skip cache if scheduled and recent
            actual = CachingCollectorUtils.ShallIgnoreCache(ignoreCache, scheduled, utcNow, sliceEndDate, sliceFrequency);
            Assert.IsTrue(actual);
        }
    }
}
