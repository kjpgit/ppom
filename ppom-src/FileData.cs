using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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
            return MarkdownEngine.MarkdownToHtml(text);
        }

        private string rootPath;
        private StoreData storeData;
        private Dictionary<String, String> productToCategoryMap;
    }
}