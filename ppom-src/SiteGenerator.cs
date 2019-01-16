using System;
using System.Dynamic;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using RazorLight;

namespace ppom 
{
    // Passed to the templates
    public class ImageDisplayInfo
    {
        public override string ToString() => $"<ImageDisplayInfo {Name}: {Width}x{Height}>";
        public string Name => System.IO.Path.GetFileName(Path);
        public string Path;
        public int Width;
        public int Height;
    }


    public class SiteGenerator
    {
        public SiteGenerator(StoreData storeData, FileData fileData) {
            this.storeData = storeData;
            this.fileData = fileData;
            this.buildDirectory = "/tmp/build";
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

        public String GetCacheBust() {
            return "ABCDEFG";
        }

        public void create_directories() {
            foreach (String categoryId in storeData.Categories.CategoryIds) {
                Directory.CreateDirectory(getOutputDir(getCategoryDir(categoryId)));
            }
        }

        public void generate_categories() {
            foreach (String categoryId in storeData.Categories.CategoryIds) {
                Category category = storeData.Categories.GetCategory(categoryId);

            }

        }

        public void generate_listings() {
            foreach (Product product in storeData.Products) {
                if (!fileData.ProductExists(product.Id)) {
                    Console.WriteLine($"Warning: product {product.Id} does not exist");
                    continue;
                } else {
                    Console.WriteLine($"Processing product {product.Id}");
                }

                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id)));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/large"));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/medium"));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/thumb"));

                generate_listing_images(product);

                string categoryId = fileData.GetProductCategoryId(product.Id);
                string categoryName = storeData.Categories.GetCategory(categoryId).Name;

                dynamic viewBag = new ExpandoObject();
                viewBag.CacheBust = GetCacheBust();
                viewBag.CategoryId = categoryId;
                viewBag.CategoryName = categoryName;
                viewBag.ProductDescription = fileData.GetProductDescriptionHTML(product.Id);
                viewBag.Images = get_images_for_display(product);

                var model = product;

                string result = runTemplate("listing", model, viewBag);
                string path = getOutputDir(getProductDir(product.Id) + "/index.html");
                File.WriteAllText(path, result);
            }
        }

        private void generate_listing_images(Product product)
        {
            var outputDir = getOutputDir(getProductDir(product.Id));
            var sourceImages = fileData.GetImagePaths(product);

            foreach (var srcImage in sourceImages) {
                Console.WriteLine($"Processing {srcImage}");

                ImageEngine.ResizeImage(srcImage,
                    outputDir + "/large/" + Path.GetFileName(srcImage),
                    maxWidth: 1024);

                ImageEngine.ResizeImage(srcImage,
                    outputDir + "/medium/" + Path.GetFileName(srcImage),
                    maxWidth: 640);
            }

            // Main product image
            var mainImage = sourceImages[0];

            ImageEngine.ResizeImage(mainImage,
                outputDir + "/thumb/thumb.jpg",
                maxWidth: 200);

            ImageEngine.ResizeImage(mainImage,
                outputDir + "/thumb/product.jpg",
                maxWidth: 1024);
        }

        private IList<ImageDisplayInfo> get_images_for_display(Product product)
        {
            var outputDir = getOutputDir(getProductDir(product.Id));
            var ret = new List<ImageDisplayInfo>();
            foreach (var path in fileData.GetImagePaths(product)) {
                var largeImage = outputDir + "/large/" + Path.GetFileName(path);
                var info = ImageEngine.GetImageMetadata(largeImage);
                ret.Add(new ImageDisplayInfo {
                    Path = largeImage,
                    Width = info.width,
                    Height = info.height
                });
            }
            return ret;
        }

        private String runTemplate<T>(string key, T model, ExpandoObject viewBag = null) {
            return engine.CompileRenderAsync(key, model, viewBag).Result;
        }

        private StoreData storeData;
        private FileData fileData;
        private String buildDirectory;
        private RazorLightEngine engine;
    }
}