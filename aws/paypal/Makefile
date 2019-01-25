# Use ACTION=create-stack to override, for first use
ACTION = update-stack

help:
	@echo usage: prod or test

prod:
	aws cloudformation $(ACTION) --stack-name ipn-prod \
	    --template-body file://ipn-sqs.yaml \
	    --parameters file://parameters-prod.json \
	    --capabilities CAPABILITY_IAM

test:
	aws cloudformation $(ACTION) --stack-name ipn-test \
	    --template-body file://ipn-sqs.yaml \
	    --parameters file://parameters-test.json \
	    --capabilities CAPABILITY_IAM
