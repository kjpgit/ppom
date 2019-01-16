using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;


namespace ppom
{
    public class MarkdownEngine 
    {
        public static String MarkdownToHtml(String text) {
            return Markdig.Markdown.ToHtml(text);
        }
    }

    public class ImageInfo
    {
        public override string ToString() => $"<ImageInfo {Name}: {Width}x{Height}>";
        public string Name => System.IO.Path.GetFileName(Path);
        public string Path;
        public int Width;
        public int Height;
    }

    public class ImageEngine
    {
        public static (int width, int height) GetImageMetadata(String path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                var info = Image.Identify(stream);
                return (info.Width, info.Height);
            }
        }

        public static void ResizeImage(String srcPath, String dstPath, int maxWidth)
        {
            if (File.Exists(dstPath))
                return;
            using (var image = Image.Load(srcPath)) {
                image.Mutate(x => x.Resize(width: maxWidth, height: 0));
                image.Save(dstPath);
            }
        }
    }

    /// <summary>
    /// Contains product data stored on the filesystem.
    /// (Markdown files, macros, images)
    /// </summary>
    public class FileData
    {
        public FileData(String rootPath, StoreData storeData) {
            this.rootPath = rootPath;
            this.storeData = storeData;
            this.productToCategoryMap = new Dictionary<String, String>();

            foreach (var categoryId in storeData.Categories.CategoryIds) {
                string path = rootPath + "/" + categoryId;
                foreach (var subpath in Directory.GetDirectories(path)) {
                    string productId = Path.GetFileName(subpath);
                    Console.WriteLine($"product {productId} -> {categoryId}");
                    productToCategoryMap[productId] = categoryId;
                }
            }
        }

        public String ExpandMacros(String text) {
            var r = new Regex("{{([-_a-z]+)}}");
            while (true) {
                Match mo = r.Match(text);
                if (!mo.Success)
                    break;
                var macro_name = mo.Groups[1];
                var macro_text = File.ReadAllText(rootPath + "/macros/" + macro_name + ".md");
                text = text.Replace(mo.Value, macro_text);
            }
            return text;
        }

        public bool ProductExists(String productId) {
            return productToCategoryMap.ContainsKey(productId);
        }

        public string GetProductCategoryId(String productId) {
            return productToCategoryMap[productId];
        }

        public string GetProductDirectory(String productId) {
            return rootPath + "/" + GetProductCategoryId(productId) + "/" + productId;
        }

        public string GetProductDescriptionHTML(String productId) {
            string path = GetProductDirectory(productId) + "/description.md";
            string text = File.ReadAllText(path);
            return ProcessMarkdownWithMacros(text);
        }

        public IList<String> GetImagePaths(Product product)
        {
            // Main images - sorted by filename
            var ret = new List<String>();
            string productDir = GetProductDirectory(product.Id);
            foreach (var file in Directory.GetFiles(productDir)) {
                if (IsImageFile(file)) {
                    ret.Add(file);
                }
            }
            ret.Sort();

            // Extra images - sorted by spreadsheet
            foreach (var file in product.ExtraImages) {
                Trace.Assert(IsImageFile(file));
                string path = rootPath + "/macros/images/" + file;
                ret.Add(path);
            }

            return ret;
        }

        public static bool IsImageFile(String path)
        {
            path = path.ToLower();
            if (path.EndsWith(".jpg"))
                return true;
            //if (path.EndsWith(".png"))
             //   return true;
            return false;
        }

        public string ProcessMarkdownWithMacros(string text) {
            text = ExpandMacros(text);
            return MarkdownEngine.MarkdownToHtml(text);
        }

        private string rootPath;
        private StoreData storeData;
        private Dictionary<String, String> productToCategoryMap;
    }
}