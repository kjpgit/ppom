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

            var fileData = new FileData("/home/karl/jgit/data");
            var blogData = new BlogData("/home/karl/jgit/data/blog");
            var storeData = new StoreData(STOREDATA, fileData);

            //test_markdown("/tmp/mdtest", fileData);

            var generator = new SiteGenerator(storeData, fileData);
            generator.create_directories();
            generator.generate_front_page();
            generator.generate_misc_pages();
            generator.generate_listings();
            generator.generate_categories();
            generator.generate_blog(blogData);

        }

        static void test_markdown(string rootDir, FileData fileData) {
            foreach (var path in Directory.GetFiles(rootDir)) {
                if (path.EndsWith(".mdorig")) {
                    string output = path.Replace(".mdorig", ".net");
                    string text = File.ReadAllText(path);
                    string html = Markdig.Markdown.ToHtml(text);
                    File.WriteAllText(output, html);
                }
            }
        }
    }
}
