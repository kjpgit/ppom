using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using RazorLight;

namespace ppom
{
    class Program
    {
        const String STOREDATA = "storedata.json";
        const String SPREADSHEET_ID = "1qfgU2L_ZpC7EnzfOxhyjj2Mliyea3jYyENidsJAXVX4";

        static void Main(string[] args)
        {
            //GoogleSheets.LoadSheet(SPREADSHEET_ID, STOREDATA);
            //var storeData = new StoreData(STOREDATA);
            var model = new { Firstname = "Bill", Lastname = "Gates" };

            var engine = new RazorLightEngineBuilder()
                        .UseFilesystemProject(Directory.GetCurrentDirectory() + "/templates")
                        .UseMemoryCachingProvider()
                        .Build();

            dynamic viewBag = new System.Dynamic.ExpandoObject();
            viewBag.Title = "Page Title Karl";

            string result = engine.CompileRenderAsync(
                "hello",
                new { Name = "John Doe" },
                viewBag).Result;
            Console.WriteLine(result);

        }
    }
}
