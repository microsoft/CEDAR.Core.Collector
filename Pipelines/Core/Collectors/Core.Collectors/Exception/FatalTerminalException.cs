// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
