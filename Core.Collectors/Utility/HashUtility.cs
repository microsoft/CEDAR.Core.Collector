// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Security.Cryptography;
using System.Text;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public static class HashUtility
    {
        // ToDo: kivancm: we should switch to SHA512 since it is more secure and faster. However, this will be a breaking change for many caching scenarios. This needs to be planned out carefully.
        public static string ComputeSha256(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            // ComputeHash - returns byte array  
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            return ConvertByteArrayToString(bytes);
        }

        public static string ComputeSha512(string input)
        {
            using SHA512 sha512 = SHA512.Create();
            // ComputeHash - returns byte array  
            byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));

            return ConvertByteArrayToString(bytes);
        }

        private static string ConvertByteArrayToString(byte[] bytes)
        {
            // Convert byte array to a string   
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
