using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace ppom
{
    public class MarkdownEngine 
    {
        public static String MarkdownToHtml(String text) {
            return Markdig.Markdown.ToHtml(text);
        }
    }

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

        public string GetProductFilePath(String productId) {
            return rootPath + "/" + GetProductCategoryId(productId) + "/" + productId;
        }

        public string GetProductDescriptionHTML(String productId) {
            string path = GetProductFilePath(productId) + "/description.md";
            string text = File.ReadAllText(path);
            return ProcessMarkdownWithMacros(text);
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