using System;
using System.Dynamic;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
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

    public class SubCategory
    {
        public String Name;   // may be null
        public List<Product> Listings;
    }

    public class CategoryModel
    {
        public Category Category;
        public List<SubCategory> SubCategories;
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
            Directory.CreateDirectory(getOutputDir("/about"));
            Directory.CreateDirectory(getOutputDir("/blog"));
            foreach (var category in storeData.Categories) {
                Directory.CreateDirectory(getOutputDir(getCategoryDir(category.Id)));
            }
        }

        public void generate_front_page() {
            dynamic viewBag = GetViewBag();
            var model = storeData;
            string result = runTemplate("front", model, viewBag);
            string path = getOutputDir("index.html");
            File.WriteAllText(path, result);
        }

        public void generate_misc_pages() {
            dynamic viewBag = GetViewBag();
            string model = null;

            string result = runTemplate("cart", model, viewBag);
            string path = getOutputDir("shop/cart.html");
            File.WriteAllText(path, result);

            result = runTemplate("about", model, viewBag);
            path = getOutputDir("about/index.html");
            File.WriteAllText(path, result);

            result = runTemplate("receipt", model, viewBag);
            path = getOutputDir("receipt.html");
            File.WriteAllText(path, result);

            result = runTemplate("error", model, viewBag);
            path = getOutputDir("error.html");
            File.WriteAllText(path, result);
        }

        public void generate_blog(BlogData blogData) {
            dynamic viewBag = GetViewBag();

            string result = runTemplate("blog-archive", blogData.BlogArchive, viewBag);
            string path = getOutputDir("blog/index.html");
            File.WriteAllText(path, result);

            foreach (var yearList in blogData.BlogArchive.years) {
                Directory.CreateDirectory(getOutputDir("/blog/" + yearList.year));
                foreach (var post in yearList.posts) {
                    result = runTemplate("blog", post, viewBag);
                    path = getOutputDir(post.path);
                    File.WriteAllText(path, result);
                }
            }
        }

        public void generate_categories() {
            foreach (var category in storeData.Categories) {
                Console.WriteLine($"Processing category {category.Id}");

                var subcat_map = new OrderedDictionary();

                foreach (var product in storeData.Products) {
                    if (product.Category.Id != category.Id) {
                        continue;
                    }

                    string subcatName = product.SubCategory ?? "";
                    if (!subcat_map.Contains(subcatName)) {
                        subcat_map[subcatName] = new List<Product>();
                    }

                    var subcat_list = (List<Product>)subcat_map[subcatName];
                    subcat_list.Add(product);
                }

                var subcats = new List<SubCategory>();
                foreach (string name in subcat_map.Keys) {
                    var subcat = new SubCategory() {
                        Name = name,
                        Listings = (List<Product>)subcat_map[name]
                    };
                    subcats.Add(subcat);
                }

                dynamic viewBag = GetViewBag();

                // Build the model
                var model = new CategoryModel() {
                    Category = category,
                    SubCategories = subcats
                };

                // Call the template
                string result = runTemplate("category", model, viewBag);
                string path = getOutputDir(getCategoryDir(category.Id) + "/index.html");
                File.WriteAllText(path, result);
            }
        }

        public void generate_listings() {
            foreach (Product product in storeData.Products) {
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id)));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/large"));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/medium"));
                Directory.CreateDirectory(getOutputDir(getProductDir(product.Id) + "/thumb"));

                generate_listing_images(product);

                dynamic viewBag = GetViewBag();
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

        private ExpandoObject GetViewBag() {
            dynamic viewBag = new ExpandoObject();
            viewBag.CacheBust = GetCacheBust();
            viewBag.FileData = fileData;
            return viewBag;
        }

        private StoreData storeData;
        private FileData fileData;
        private String buildDirectory;
        private RazorLightEngine engine;
    }
}