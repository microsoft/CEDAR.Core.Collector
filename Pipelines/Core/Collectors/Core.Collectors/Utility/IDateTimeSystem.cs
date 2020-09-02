// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public interface IDateTimeSystem
    {
        DateTime UtcNow { get; }
    }

    public class DateTimeWrapper : IDateTimeSystem
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
