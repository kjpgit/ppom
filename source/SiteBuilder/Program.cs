using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ppom
{
    class Program
    {
        // Usage: dotnet run -- DATA_DIR BUILD_DIR
        static void Main(string[] args)
        {
            string data_dir = args[0];
            string build_dir = args[1];
            string blog_path = data_dir + "/blog";
            string store_data_json = data_dir + "/storedata.json";

            //test_markdown("/tmp/mdtest", fileData);

            var fileData = new FileData(data_dir);
            var blogData = new BlogData(blog_path);
            var storeData = new StoreData(store_data_json, fileData);
            var generator = new SiteGenerator(build_dir, storeData, fileData);

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
