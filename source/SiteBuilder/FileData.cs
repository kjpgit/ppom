using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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
            String tempPath = ".resize.jpg";
            if (File.Exists(dstPath))
                return;
            using (var image = Image.Load(srcPath)) {
                image.Mutate(x => x.Resize(width: maxWidth, height: 0));
                image.Save(tempPath);
                File.Move(tempPath, dstPath);
            }
        }
    }

    /// <summary>
    /// Accesses product data stored on the filesystem.
    /// (Markdown files, macros, images)
    /// </summary>
    public class FileData
    {
        public FileData(String rootPath) {
            this.rootPath = rootPath;
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

        public List<String> GetProductIdsForCategory(String categoryId)
        {
            var path = rootPath + "/" + categoryId;
            var ret = new List<String>();
            foreach (var subpath in Directory.GetDirectories(path)) {
                string productId = Path.GetFileName(subpath);
                ret.Add(productId);
            }
            return ret;
        }

        public string GetProductDirectory(String categoryId, String productId) {
            return rootPath + "/" + categoryId + "/" + productId;
        }

        public string GetProductDescriptionHTML(String productDir) {
            string path = productDir + "/description.md";
            string text = File.ReadAllText(path);
            return ProcessMarkdownWithMacros(text);
        }

        public string GetCategoryDescriptionHTML(String categoryId) {
            string path = rootPath + "/" + categoryId + "/description.md";
            if (!File.Exists(path)) {
                return null;
            }
            string text = File.ReadAllText(path);
            return ProcessMarkdownWithMacros(text);
        }

        // NB: Called by templates
        public string GetMarkdownHTML(String logicalPath) {
            string path = rootPath + "/" + logicalPath;
            string text = File.ReadAllText(path);
            return ProcessMarkdownWithMacros(text);
        }

        // NB: Called by blog template
        public string ProcessMarkdownWithMacros(string text) {
            text = ExpandMacros(text);
            return MarkdownEngine.MarkdownToHtml(text);
        }

        public IList<String> GetImagePaths(Product product)
        {
            // Main images - sorted by filename
            var ret = new List<String>();
            string productDir = GetProductDirectory(product.Category.Id, product.Id);
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

        private string rootPath;
    }
}