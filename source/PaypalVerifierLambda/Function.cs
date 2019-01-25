using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SQS;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PaypalVerifierLambda
{
    public class Function
    {
        public async Task FunctionHandler(SNSEvent input, ILambdaContext context)
        {
            string verifiedSQS = Environment.GetEnvironmentVariable("PPOM_VERIFIED_SQS");
            string errorSQS = Environment.GetEnvironmentVariable("PPOM_ERROR_SQS");
            string verifyURL = Environment.GetEnvironmentVariable("PPOM_VERIFY_URL");
            string message = input.Records[0].Sns.Message;

            Trace.Assert(!String.IsNullOrWhiteSpace(verifiedSQS));
            Trace.Assert(!String.IsNullOrWhiteSpace(errorSQS));
            Trace.Assert(!String.IsNullOrWhiteSpace(verifyURL));
            Trace.Assert(!String.IsNullOrWhiteSpace(message));

            string bodyRequest = "cmd=_notify-validate&" + message;
            Console.WriteLine($"SNS message: {bodyRequest}");
            Console.WriteLine($"SQS target: {verifiedSQS}");

            // Purposefully not trying to cache or reuse this client.
            // It's a Lambda and very low traffic.
            using (var client = new HttpClient()) {
                // NB: Paypal profile configured to use UTF8.
                // Tested with unicode characters in a product name.
                var bytes = UTF8Encoding.UTF8.GetBytes(bodyRequest);
                var content = new ByteArrayContent(bytes);
                var result = await client.PostAsync(verifyURL, content);
                result.EnsureSuccessStatusCode();
                var resultString = await result.Content.ReadAsStringAsync();
                Console.WriteLine($"verify result: {result.StatusCode}, {resultString}");

                // Add to SQS queue
                var sqsClient = new AmazonSQSClient();
                if (resultString == "VERIFIED") {
                    await sqsClient.SendMessageAsync(verifiedSQS, message);
                } else {
                    await sqsClient.SendMessageAsync(errorSQS, message);
                }
            }
        }
    }
}
