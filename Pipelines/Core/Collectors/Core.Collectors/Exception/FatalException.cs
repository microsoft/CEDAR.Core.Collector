// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Error
{
    public class FatalException : Exception
    {
        public FatalException(string message)
            : base(message)
        {
        }
    }
}
