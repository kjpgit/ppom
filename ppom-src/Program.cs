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
            //GoogleSheets.LoadSheet(SPREADSHEET_ID, STOREDATA);
            var storeData = new StoreData(STOREDATA);

            var fileData = new FileData("/home/karl/jgit/data", storeData);

            var generator = new SiteGenerator(storeData, fileData);
            generator.create_directories();
            generator.generate_listings();
            generator.generate_categories();

        }
    }
}
