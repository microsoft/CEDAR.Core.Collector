// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CloudMine.Core.Collectors.Utility;
using System;

namespace Microsoft.CloudMine.Core.Collectors.Tests.Utility
{
    public class MockDateTime : IDateTimeSystem
    {
        private DateTime utcNow;

        public MockDateTime()
            : this(DateTime.UtcNow)
        {
        }

        public MockDateTime(DateTime utcNow)
        {
            this.utcNow = utcNow;
        }

        public void PassTime(TimeSpan amount)
        {
            this.utcNow += amount;
        }

        public void SetTime(DateTime utcNow)
        {
            this.utcNow = utcNow;
        }

        public DateTime UtcNow => this.utcNow;
    }
}
