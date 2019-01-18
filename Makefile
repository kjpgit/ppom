SPREADSHEET_ID = "1qfgU2L_ZpC7EnzfOxhyjj2Mliyea3jYyENidsJAXVX4"
DATA_DIR = /home/karl/jgit/data
STORE_JSON = $(DATA_DIR)/storedata.json
BUILD_DIR = /tmp/build


help:
	@echo "all: download and build site"
	@echo "clean: remove build dir"
	@echo "build: build site (doesn't download spreadsheet)"
	@echo "download: download google spreadsheet and convert to nice json"

download:
	dotnet run -p source/GoogleSpreadsheetData -- $(SPREADSHEET_ID) $(STORE_JSON)

build:
	./build.sh $(DATA_DIR) $(BUILD_DIR)

clean:
	rm -rf $(BUILD_DIR)

all: download build
