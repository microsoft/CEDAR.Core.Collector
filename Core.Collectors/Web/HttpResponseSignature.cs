// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Net;
using System.Text.RegularExpressions;

namespace Microsoft.CloudMine.Core.Collectors.Web
{
    public class HttpResponseSignature
    {
        public HttpStatusCode statusCode;
        public Regex responseMessageRegex;

        public HttpResponseSignature(HttpStatusCode statusCode, string responseMessageRegex)
        {
            this.statusCode = statusCode;
            this.responseMessageRegex = new Regex($"^{responseMessageRegex}$", RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public bool Matches(HttpStatusCode statusCode, string responseMessage)
        {
            return this.statusCode == statusCode && this.responseMessageRegex.IsMatch(responseMessage);
        }
    }
}
