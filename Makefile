SPREADSHEET_ID = "1qfgU2L_ZpC7EnzfOxhyjj2Mliyea3jYyENidsJAXVX4"
DATA_DIR = /home/karl/jgit/data
STORE_JSON = $(DATA_DIR)/storedata.json
BUILD_DIR = /tmp/build
PROD_DIR = /tmp/build.prod
DRY_RUN = --dry-run


help:
	@echo "clean: remove build dir"
	@echo "all: download spreadsheet and build site"
	@echo "build: build site (doesn't download spreadsheet)"
	@echo "download: download google spreadsheet and convert to nice json"
	@echo "upload-test: upload to test s3 bucket"
	@echo "upload-prod: upload to prod s3 bucket"

download:
	dotnet run -p source/GoogleSpreadsheetData -- $(SPREADSHEET_ID) $(STORE_JSON)

upload-test:
	dotnet run -p source/SyncS3 -- $(BUILD_DIR) sandbox.ppom $(DRY_RUN)

upload-prod:
	dotnet run -p source/SyncS3 -- $(PROD_DIR) www.patternedpom.com $(DRY_RUN)

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

clean:
	rm -rf $(BUILD_DIR) $(PROD_DIR)

serve:
	twistd -n web -p tcp:8000 --path $(BUILD_DIR)

all: download build
