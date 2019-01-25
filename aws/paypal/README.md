## Overview

Paypal IPN handling.

Based off of their example AWS code https://github.com/paypal/ipn-code-samples/blob/master/aws/paypal-ipn-cloudformation.yml,
but changed to use C# instead of ancient python 2.7, and proper POST of the message.


## Overview

* Receiver: API Gateway -> [PaypalReceiverLambda](../../source/PaypalReceiverLambda/) -> SNS Topic

* Verifier: SNS Topic -> [PaypalVerifierLambda](../../source/PaypalVerifierLambda/) -> SQS Topic

The reason Paypal recommends two queues is clever: it is needed to prevent lost messages.
Imagine the verifier lambda dies right after it POSTs the verify message to paypal, but before it publishes to SQS.
The message would be lost forever.

But with a separate SNS queue, SNS will drive retries until the Lambda verifier process fully succeeds.

Note that you can still get *duplicate* messages in SQS, e.g. if the verifier dies right after it publishes to SQS.  
But that's ok, and could be prevented by writing to DynamoDB when you read the SQS queue.  
It's not necessary for this site since it's just for digital purchase emails, and a rare duplicate email is fine.
(Also, Amazon SES *sadly* doesn't support idempotent email sending based on a unique message ID anyway!)
