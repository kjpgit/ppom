using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ppom
{
    class Program
    {
        // Usage: dotnet run -- /path/to/storedata
        static void Main(string[] args)
        {
            string data_path = args[0];
            string blog_path = data_path + "/blog";
            string store_data_json = data_path + "/storedata.json";

            //test_markdown("/tmp/mdtest", fileData);

            var fileData = new FileData(data_path);
            var blogData = new BlogData(blog_path);
            var storeData = new StoreData(store_data_json, fileData);
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
