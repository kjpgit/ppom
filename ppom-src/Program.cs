using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Mvc.RenderViewToString;

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

            RazorHelper.run();

            /*
            var engine = new RazorEngine("templates");

            String[] templates = {"hello.cshtml", "test2.cshtml"};
            foreach (var template in templates) {
                engine.LoadTemplate(template);
            }

            foreach (var template in templates) {
                var obj = engine.CreateTemplate(template);
                obj.run();
            }
            foreach (var template in templates) {
                var obj = engine.CreateTemplate(template);
                obj.run();
            }
            */

        }
    }
}
