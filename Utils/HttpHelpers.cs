using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

using AwsSecretsManagerService.Http;

namespace AwsSecretsManagerService.Utils
{
    /// <summary>
    /// Various Http helper routines
    /// </summary>
    public static class HttpHelpers
    {
        // The Set of accepted and valid Url characters per RFC3986. Characters outside of this set will be encoded.
        private const string validUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        /// <summary>
        /// Construct a HttpWebRequest onto the specified endpoint and populate
        /// the headers.
        /// </summary>
        /// <param name="endpointUri">The endpoint to call</param>
        /// <param name="httpMethod">GET, PUT etc</param>
        /// <param name="headers">The set of headers to apply to the request</param>
        /// <returns>Initialized HttpWebRequest instance</returns>
        public static HttpWebRequest ConstructWebRequest(Uri endpointUri,
                                                         string httpMethod,
                                                         IDictionary<string, string> headers,
                                                         string requestBody)
        {
            var request = (HttpWebRequest)WebRequest.Create(endpointUri);
            request.Method = httpMethod;

            foreach (var header in headers.Keys)
            {
                // not all headers can be set via the dictionary
                if (header.Equals(HttpHeaderNames.Host, StringComparison.OrdinalIgnoreCase))
                {
                    request.Host = headers[header];
                }
                else if (header.Equals(HttpHeaderNames.Content_Length, StringComparison.OrdinalIgnoreCase))
                {
                    request.ContentLength = long.Parse(headers[header]);
                }
                else if (header.Equals(HttpHeaderNames.Content_Type, StringComparison.OrdinalIgnoreCase))
                {
                    request.ContentType = headers[header];
                }
                else
                {
                    request.Headers.Add(header, headers[header]);
                }
            }

            if (!string.IsNullOrEmpty(requestBody))
            {
                var buffer = new byte[8192]; // arbitrary buffer size                        
                var requestStream = request.GetRequestStream();
                using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)))
                {
                    var bytesRead = 0;
                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }
                }
            }

            return request;
        }

        public static string GetResponseBody(HttpWebRequest request)
        {
            // Get the response and read any body into a string, then display.
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseBody = ReadResponseBody(response);
                    return responseBody;
                }
                else
                    throw new Exception($"\n-- HTTP call failed, status code: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Reads the response data from the service call, if any
        /// </summary>
        /// <param name="response">
        /// The response instance obtained from the previous request
        /// </param>
        /// <returns>The body content of the response</returns>
        public static string ReadResponseBody(HttpWebResponse response)
        {
            if (response == null)
                throw new ArgumentNullException("response", "Value cannot be null");

            // Then, open up a reader to the response and read the contents to a string
            // and return that to the caller.
            string responseBody = string.Empty;
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var reader = new StreamReader(responseStream))
                    {
                        responseBody = reader.ReadToEnd();
                    }
                }
            }
            return responseBody;
        }


        /// <summary>
        /// Helper routine to url encode canonicalized header names and values for safe
        /// inclusion in the presigned url.
        /// </summary>
        /// <param name="data">The string to encode</param>
        /// <param name="isPath">Whether the string is a URL path or not</param>
        /// <returns>The encoded string</returns>
        public static string UrlEncode(string data, bool isPath = false)
        {
            var encoded = new StringBuilder(data.Length * 2);
            string unreservedChars = String.Concat(validUrlCharacters, (isPath ? "/:" : ""));

            foreach (char symbol in Encoding.UTF8.GetBytes(data))
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                    encoded.Append(symbol);
                else
                    encoded.Append("%").Append(String.Format("{0:X2}", (int)symbol));
            }

            return encoded.ToString();
        }
    }
}
