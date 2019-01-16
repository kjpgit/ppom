using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using RazorLight;

namespace ppom 
{
    public class SiteGenerator
    {
        public SiteGenerator(StoreData storeData) {
            this.storeData = storeData;
            this.buildDirectory = "build";
            this.engine = new RazorLightEngineBuilder()
                        .UseFilesystemProject(Directory.GetCurrentDirectory() + "/templates")
                        .UseMemoryCachingProvider()
                        .Build();

        }

        public String getCategoryDir(String category_id) {
            return "shop/" + category_id;
        }

        public String getProductDir(String productId) {
            return "shop/listing/" + productId;
        }

        public String getOutputDir(String path) {
            return this.buildDirectory + "/" + path;
        }

        public void create_directories() {
            foreach (String categoryId in storeData.Categories.CategoryIds) {
                Directory.CreateDirectory(getOutputDir(getCategoryDir(categoryId)));
            }
        }

        public void generate_listings() {
            foreach (Product product in storeData.Products) {
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id)));

                dynamic viewBag = new System.Dynamic.ExpandoObject();
                viewBag.Title = "Page Title Karl";
                viewBag.CacheBust = "123afc";
                viewBag.PageId = "TestPage";

                var model = product;

                string path = getOutputDir(getProductDir(product.Id) + "/index.html");
                string result = engine.CompileRenderAsync("listing", model, viewBag).Result;
                File.WriteAllText(path, result);
            }
        }


        private StoreData storeData;
        private String buildDirectory;
        private RazorLightEngine engine;
    }
}