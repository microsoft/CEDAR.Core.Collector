// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace Microsoft.CloudMine.Core.Collectors.Error
{
    public class FatalTerminalException : FatalException
    {
        public FatalTerminalException(string message)
            : base(message)
        {
        }
    }
}
