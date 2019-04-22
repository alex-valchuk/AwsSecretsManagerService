using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using AwsSecretsManagerService.Signers;
using AwsSecretsManagerService.Utils;
using AwsSecretsManagerService.Http;

namespace AwsSecretsManagerService
{
    /// <summary>
    /// Clinet to get secrets from Amazon Secrets Manager with Signature V4 authorization
    /// </summary>
    public class AwsSecretsManagerClient
    {
        private const string httpMethod = "POST";
        private const string serviceName = "secretsmanager";

        private readonly string accessKey;
        private readonly string secretKey;
        private readonly string region;
        private readonly Uri endpointUri;

        public AwsSecretsManagerClient(string accessKey, string secretKey, string region)
        {
            this.accessKey = accessKey;
            this.secretKey = secretKey;
            this.region = region;

            this.endpointUri = new Uri($"https://{serviceName}.{region}.amazonaws.com");
        }

        public AwsSecret GetSecret(string secretId)
        {
            try
            {
                var body = $"{{ \"SecretId\": \"{secretId}\" }}";
                var headers = this.SetHeaders(body);

                var request = HttpHelpers.ConstructWebRequest(endpointUri, httpMethod, headers, body);
                var responseBody = HttpHelpers.GetResponseBody(request);

                var secret = JsonConvert.DeserializeObject<AwsSecret>(responseBody);
                return secret;
            }
            catch
            {
                throw;
            }
        }

        private Dictionary<string, string> SetHeaders(string body)
        {
            // hash the Base64 version of the body and pass this to the signer as the body hash
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var bodyHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(bodyBytes);
            var bodyHashString = AWS4SignerBase.ToHexString(bodyHash, true);

            var headers = new Dictionary<string, string>
            {
                { HttpHeaderNames.Content_Length, body.Length.ToString() },
                { HttpHeaderNames.Content_Type, "application/x-amz-json-1.1" },
                { HttpHeaderNames.X_Amz_Content_SHA256, bodyHashString },
                { HttpHeaderNames.X_Amz_Target, $"{serviceName}.GetSecretValue" }
            };

            this.CalculateAuthorizationHeader(headers, bodyHashString);

            return headers;
        }

        private void CalculateAuthorizationHeader(Dictionary<string, string> headers, string bodyHashString)
        {
            // precompute hash of the body content
            var signer = new AWS4PostSigner
            {
                EndpointUri = endpointUri,
                HttpMethod = httpMethod,
                Service = serviceName,
                Region = region
            };

            var authorization = signer.ComputeSignature(headers,
                                                        "",   // no query parameters
                                                        bodyHashString,
                                                        accessKey,
                                                        secretKey);

            headers.Add("Authorization", authorization);
        }
    }
}
