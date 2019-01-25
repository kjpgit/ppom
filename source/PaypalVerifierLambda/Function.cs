using System;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PaypalVerifierLambda
{
    public class Function
    {
        public string FunctionHandler(Amazon.Lambda.SNSEvents.SNSEvent input, ILambdaContext context)
        {
            string verifiedSQS = Environment.GetEnvironmentVariable("PPOM_VERIFIED_SQS");
            string errorSQS = Environment.GetEnvironmentVariable("PPOM_ERROR_SQS");
            string verifyURL = Environment.GetEnvironmentVariable("PPOM_VERIFY_URL");
            string message = input.Records[0].Sns.Message;

            Trace.Assert(verifiedSQS != null);
            Trace.Assert(errorSQS != null);
            Trace.Assert(verifyURL != null);

            string bodyRequest = "cmd=_notify-validate&" + message;
            Console.WriteLine($"SNS message: {bodyRequest}");
            Console.WriteLine($"SQS target: {verifiedSQS}");

            // Purposefully not trying to cache or reuse this client.
            // It's a Lambda and very low traffic.
            using (var client = new HttpClient()) {
                var bytes = UTF8Encoding.UTF8.GetBytes(bodyRequest);
                var content = new ByteArrayContent(bytes);
                var result = client.PostAsync(verifyURL, content).Result;
                result.EnsureSuccessStatusCode();
                var resultString = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"verify result: {result.StatusCode}, {resultString}");

                // Add to SQS queue
                var sqsClient = new Amazon.SQS.AmazonSQSClient();
                if (resultString == "VERIFIED") {
                    var response = sqsClient.SendMessageAsync(verifiedSQS, message).Result;
                } else {
                    var response = sqsClient.SendMessageAsync(errorSQS, message).Result;
                }
            }

            return "task finished";
        }
    }
}
