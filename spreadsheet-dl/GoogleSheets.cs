using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using static spreadsheet_dl.Extensions;

namespace spreadsheet_dl
{
    class SpreadSheetHeader
    {
        public SpreadSheetHeader(Sheet s)
        {
            this.columns = new Dictionary<int, String>();
            foreach (var (i, v) in Enumerate(s.Data[0].RowData[0].Values)) {
                this.columns[i] = v.FormattedValue;
            }
        }

        public String GetColumnName(int i)
        {
            return this.columns[i];
        }

        private Dictionary<int, String> columns;
    }


    class GoogleSheets
    {
        // If modifying these scopes, delete your previously saved credentials
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "Google Sheets API .NET Quickstart";

        static public void LoadSheet(String spreadsheetId, String filePath)
        {
            UserCredential credential;

            using (var stream = new FileStream("credentials.json",
                FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Spreadsheets.Get(spreadsheetId);
            request.IncludeGridData = true;
            Spreadsheet response = request.Execute();
            parse_sheet(response, filePath);
        }

        static Sheet get_sheet(Spreadsheet spreadsheet, String title)
        {
            foreach (Sheet s in spreadsheet.Sheets) {
                if (s.Properties.Title == title) {
                    return s;
                }
            }
            throw new Exception("Sheet not found: " + title);
        }

        static void parse_sheet(Spreadsheet spreadsheet, String filePath)
        {
            // Options
            Sheet s = get_sheet(spreadsheet, "Options");
            var root_obj = new JObject();
            root_obj["options"] = new JArray();

            foreach (var (i, v) in Enumerate(s.Data[0].RowData[0].Values))
            {
                if (String.IsNullOrWhiteSpace(v.FormattedValue)) {
                    continue;
                }

                var option = new JObject();
                option["title"] = v.FormattedValue.Trim();
                option["values"] = new JArray();

                foreach (var row in s.Data[0].RowData.Skip(1)) {
                    if (i >= row.Values.Count) {
                        continue;
                    }
                    
                    var option_value = row.Values[i].FormattedValue;
                    if (String.IsNullOrWhiteSpace(option_value)) {
                        continue;
                    }

                    ((JArray)option["values"]).Add(option_value.Trim());
                }

                ((JArray)root_obj["options"]).Add(option);
            }

            // Listings
            s = get_sheet(spreadsheet, "Products");
            var header = new SpreadSheetHeader(s);
            root_obj["products"] = new JArray();

            foreach (var row in s.Data[0].RowData.Skip(1)) {

                var product = new JObject();

                foreach (var (i, v) in Enumerate(row.Values))
                {
                    String column_name = header.GetColumnName(i);
                    String column_value = v.FormattedValue;
                    if (!String.IsNullOrWhiteSpace(column_name) &&
                        !String.IsNullOrWhiteSpace(column_value))
                    {
                        product[column_name] = column_value.Trim();
                    }
                }

                if (product.ContainsKey("id")) {
                    ((JArray)root_obj["products"]).Add(product);
                }
            }

            using (var stream = new StreamWriter(filePath)) {
                stream.Write(root_obj.ToString());
            }
        }
    }
}