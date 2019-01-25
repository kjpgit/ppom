using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.SimpleNotificationService;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PaypalReceiverLambda
{
    public class Function
    {
        public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest input, ILambdaContext context)
        {
            Console.WriteLine($"testing fuck");
            Console.WriteLine($"body: {input.Body}");

            var snsArn = Environment.GetEnvironmentVariable("PPOM_SNS_ARN");

            var client = new AmazonSimpleNotificationServiceClient();
            var response = client.PublishAsync(snsArn, input.Body).Result;

            return new APIGatewayProxyResponse {
                Body = $"Sent message to {snsArn} {response.HttpStatusCode}",
                StatusCode = 200,
            };
        }
    }
}
