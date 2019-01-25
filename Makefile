SPREADSHEET_ID = "1qfgU2L_ZpC7EnzfOxhyjj2Mliyea3jYyENidsJAXVX4"
DATA_DIR = /home/karl/jgit/data
STORE_JSON = $(DATA_DIR)/storedata.json
BUILD_DIR = /tmp/build
PROD_DIR = /tmp/build.prod
DRY_RUN = --dry-run

nullstring :=
space := $(nullstring) $(nullstring)

LAMBDA_TEST_VARS  = LAMBDA_NET_SERIALIZER_DEBUG=true
LAMBDA_TEST_VARS += PPOM_SNS_ARN=arn:aws:sns:us-east-1:757437486362:unverified-ipn-test
LAMBDA_TEST_VARS += PPOM_VERIFIED_SQS=https://sqs.us-east-1.amazonaws.com/757437486362/verified-ipn-test
LAMBDA_TEST_VARS += PPOM_ERROR_SQS=https://sqs.us-east-1.amazonaws.com/757437486362/errors-ipn-test
LAMBDA_TEST_VARS += PPOM_VERIFY_URL=https://www.sandbox.paypal.com/cgi-bin/webscr
LAMBDA_TEST_VARS := $(subst $(space),;,$(LAMBDA_TEST_VARS))

LAMBDA_PROD_VARS  = LAMBDA_NET_SERIALIZER_DEBUG=true
LAMBDA_PROD_VARS += PPOM_SNS_ARN=arn:aws:sns:us-east-1:757437486362:unverified-ipn-prod
LAMBDA_PROD_VARS += PPOM_VERIFIED_SQS=https://sqs.us-east-1.amazonaws.com/757437486362/verified-ipn-prod
LAMBDA_PROD_VARS += PPOM_ERROR_SQS=https://sqs.us-east-1.amazonaws.com/757437486362/errors-ipn-prod
LAMBDA_PROD_VARS += PPOM_VERIFY_URL=https://www.paypal.com/cgi-bin/webscr
LAMBDA_PROD_VARS := $(subst $(space),;,$(LAMBDA_PROD_VARS))


help:
	@echo "clean:       remove build dir"
	@echo "all:         download spreadsheet and build site"
	@echo "download:    download google spreadsheet and convert to nice json"
	@echo "build:       build site (doesn't download spreadsheet)"
	@echo "serve:       start twistd at 0.0.0.0:8000 for test site"
	@echo
	@echo "upload-test: upload to test s3 bucket"
	@echo "upload-prod: upload to prod s3 bucket"
	@echo
	@echo "lambda-test: build and deploy test lambdas"
	@echo "lambda-prod: build and deploy prod lambdas"


# Web site deployment

clean:
	rm -rf $(BUILD_DIR) $(PROD_DIR)

all: download build

download:
	dotnet run -p source/GoogleSpreadsheetData -- $(SPREADSHEET_ID) $(STORE_JSON)

build:
	./build.sh $(DATA_DIR) $(BUILD_DIR)
	rm -rf $(PROD_DIR)
	cp -a $(BUILD_DIR) $(PROD_DIR)
	find $(PROD_DIR) -name '*.html' -print0 \
	    | xargs -0 perl -i -pe 's/<meta name="robots".*//'
	find $(PROD_DIR) -name '*.js' -print0 \
	    | xargs -0 perl -i -pe 's|https://d15f32nxt8eigc.cloudfront.net|https://www.patternedpom.com|'
	find $(PROD_DIR) -name '*.js' -print0 \
	    | xargs -0 perl -i -pe 's/www.sandbox.paypal.com/www.paypal.com/'
	find $(PROD_DIR) -name '*.js' -print0 \
	    | xargs -0 perl -i -pe 's/karl.pickett-facilitator\@gmail.com/patternedpomegranate\@gmail.com/'
	! grep -rI cloudfront.net $(PROD_DIR)

serve:
	twistd -n --pidfile="" web -p tcp:8000 --path $(BUILD_DIR) 

upload-test:
	dotnet run -p source/SyncS3 -- $(BUILD_DIR) sandbox.ppom $(DRY_RUN)

upload-prod:
	dotnet run -p source/SyncS3 -- $(PROD_DIR) www.patternedpom.com $(DRY_RUN)


# Lambda Deployment

lambda-test: lambda-test-r lambda-test-v

lambda-test-r:
	(cd source/PaypalReceiverLambda && dotnet lambda deploy-function PaypalReceiverLambdaTest \
	    --environment-variables "$(LAMBDA_TEST_VARS)" \
	    --function-role lambdatest)

lambda-test-v:
	(cd source/PaypalVerifierLambda && dotnet lambda deploy-function PaypalVerifierLambdaTest \
	    --environment-variables "$(LAMBDA_TEST_VARS)" \
	    --function-role lambdatest)


lambda-prod: lambda-prod-r lambda-prod-v

lambda-prod-r:
	(cd source/PaypalReceiverLambda && dotnet lambda deploy-function PaypalReceiverLambdaProd \
	    --environment-variables "$(LAMBDA_PROD_VARS)" \
	    --function-role lambdatest)

lambda-prod-v:
	(cd source/PaypalVerifierLambda && dotnet lambda deploy-function PaypalVerifierLambdaProd \
	    --environment-variables "$(LAMBDA_PROD_VARS)" \
	    --function-role lambdatest)
