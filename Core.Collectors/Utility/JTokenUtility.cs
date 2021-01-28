// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.CloudMine.Core.Collectors.Utility
{
    public static class JTokenUtility
    {
        private static bool IsNull(JToken token)
        {
            return token == null;
        }

        private static bool IsEmpty(JToken token)
        {
            return (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == string.Empty);
        }

        private static bool IsWhiteSpace(JToken token)
        {
            return token.Value<string>().All(char.IsWhiteSpace);
        }

        // This method is functionally the same as string.IsNullOrEmpty(), but it works with a JToken contains any type. 
        // This reduces the number of checks you need to make from 4 to 1 if you want to determine if the value of a JToken containing an unknown type is null or empty.
        public static bool IsNullOrEmpty(JToken token)
        {
            return IsNull(token) || IsEmpty(token);
        }

        // This method is functionally the same as string.IsNullOrWhiteSpace(), but it works with a JToken that contains any type.
        // This reduces the number of checks you need to make from 5 to 1 if you want to determine if the value of a JToken containing an unknown type is null, empty, or whitespace.
        public static bool IsNullOrWhiteSpace(JToken token)
        {
            return IsNull(token) || IsEmpty(token) || IsWhiteSpace(token);
        }
    }
}
