using System;
using System.IO;
using System.Diagnostics;
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
        public async Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest input, ILambdaContext context)
        {
            var snsArn = Environment.GetEnvironmentVariable("PPOM_SNS_ARN");
            Trace.Assert(snsArn != null);

            var client = new AmazonSimpleNotificationServiceClient();
            var response = await client.PublishAsync(snsArn, input.Body);

            var ret = new APIGatewayProxyResponse();
            ret.StatusCode = 200;
            ret.Body = "";
            ret.Headers = new Dictionary<string, string>();
            ret.Headers["x-ppom-sns-queue"] = snsArn;

            return ret;
        }
    }
}
