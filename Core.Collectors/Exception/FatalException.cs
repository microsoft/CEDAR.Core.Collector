// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
