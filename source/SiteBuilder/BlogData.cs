using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

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
            string title = this.Metadata["Title"].ToLowerInvariant();
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

    public class BlogArchiveYear {
        public BlogArchiveYear(string year) {
            this.year = year;
        }

        public string year;
        public List<BlogPost> posts = new List<BlogPost>();
    }

    public class BlogArchive {
        public List<BlogArchiveYear> years = new List<BlogArchiveYear>();
    }

    public class BlogData {
        public BlogData(string rootPath) {
            this.rootPath = rootPath;
            blogArchive = new BlogArchive();

            foreach (var year_dir in Directory.GetDirectories(rootPath).OrderBy(p => p)) {
                string year = Path.GetFileName(year_dir);
                blogArchive.years.Add(new BlogArchiveYear(year));
                foreach (var blog_file in Directory.GetFiles(year_dir)) {
                    if (blog_file.ToLower().EndsWith(".md")) {
                        var post = new BlogPost(year, blog_file);
                        if (!post.IsDraft()) {
                            blogArchive.years.Last().posts.Add(post);
                        }
                    }
                }
            }
        }

        public BlogArchive BlogArchive => blogArchive;

        private BlogArchive blogArchive;
        private string rootPath;
    }

}