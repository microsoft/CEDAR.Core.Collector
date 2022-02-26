// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public static class SecurityUtils
    {
        /// <summary>
        /// Returns a secure string to a plain text string. This method should never exist in a perfect world 
        /// and only be used if there is an API that does not accept SecureString objects.
        /// </summary>
        public static string ToPlainString(this SecureString secureString)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static SecureString ToSecureString(this string input)
        {
            var result = new SecureString();
            foreach (var character in input.ToCharArray())
            {
                result.AppendChar(character);
            }
            return result;
        }
    }
}
