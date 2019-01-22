using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ppom
{
    // This class is immutable.
    public class BlogPost {
        public BlogPost(string year, string path) {
            this.Year = year;

            string data = File.ReadAllText(path);
            var data_lines = new List<String>();
            bool in_data = false;
            var metadata = new Dictionary<String, String>();
            foreach (var line in data.Split('\n')) {
                if (!in_data && line.Contains(":")) {
                    var vals = line.Split(":", 2);
                    metadata[vals[0].Trim()] = vals[1].Trim();
                } else {
                    in_data = true;
                    data_lines.Add(line);
                }
            }

            this.Metadata = new ReadOnlyDictionary<string, string>(metadata);
            this.MarkdownText = String.Join("\n", data_lines);
            this.Path = "/blog/" + year + "/" + GetFSTitle() + ".html";
        }

        public String Title => Metadata["Title"];

        public bool IsDraft() {
            string status = this.Metadata.GetValueOrDefault("Status", "ok");
            return (status.ToLower() == "draft");
        }

        public string GetFSTitle() {
            string title = this.Title.ToLowerInvariant();
            title = title.Replace(" ", "-");
            title = title.Replace(".", "");
            title = title.Replace("!", "");
            title = title.Replace("&", "");
            title = title.Replace("'", "");
            title = title.Replace(",", "");
            while (title.Contains("--")) {
                title = title.Replace("--", "-");
            }
            return title;
        }

        public string Path { get; }
        public string Year { get; }
        public string MarkdownText { get; }
        public ReadOnlyDictionary<string, string> Metadata { get; }
    }

    // A collection of blog posts.
    // This class is immutable.
    public class BlogData 
    {
        public BlogData(string rootPath) {
            this.rootPath = rootPath;
            blogPosts = new List<BlogPost>();

            foreach (var year_dir in Directory.GetDirectories(rootPath)) {
                string year = Path.GetFileName(year_dir);
                foreach (var blog_file in Directory.GetFiles(year_dir)) {
                    if (blog_file.ToLower().EndsWith(".md")) {
                        var post = new BlogPost(year, blog_file);
                        if (!post.IsDraft()) {
                            blogPosts.Add(post);
                        }
                    }
                }
            }
        }

        public IList<BlogPost> Posts => blogPosts.AsReadOnly();

        private List<BlogPost> blogPosts;
        private string rootPath;
    }
}