using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;


namespace ppom
{
    class Program
    {
        const String STOREDATA = "storedata.json";
        const String SPREADSHEET_ID = "1qfgU2L_ZpC7EnzfOxhyjj2Mliyea3jYyENidsJAXVX4";

        static void Main(string[] args)
        {
            Console.WriteLine(Extensions.GetDecimalPlaces(123m));
            Console.WriteLine(Extensions.GetDecimalPlaces(123.0m));
            Console.WriteLine(Extensions.GetDecimalPlaces(123.00m));
    
            //GoogleSheets.LoadSheet(SPREADSHEET_ID, STOREDATA);
            var storeData = new StoreData(STOREDATA);
        }
    }
}
