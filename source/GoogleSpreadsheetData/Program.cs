using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ppom
{
    class Program
    {
        // Usage: dotnet run -- SPREADSHEET_ID OUTPUT_FILE
        static void Main(string[] args)
        {
            string spreadsheet_id = args[0];
            string output_file = args[1];

            GoogleSheets.LoadSheet(spreadsheet_id, output_file);
        }
    }
}
