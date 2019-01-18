using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

namespace ppom
{
    public class BlogPost {
        public BlogPost(string year, string path) {
            this.year = year;

            string data = File.ReadAllText(path);
            var data_lines = new List<String>();
            bool in_data = false;
            foreach (var line in data.Split('\n')) {
                if (!in_data && line.Contains(":")) {
                    var vals = line.Split(":", 2);
                    this.metadata[vals[0].Trim()] = vals[1].Trim();
                } else {
                    in_data = true;
                    data_lines.Add(line);
                }
            }

            this.markdownText = String.Join("\n", data_lines);

            this.path = "/blog/" + year + "/" + GetFSTitle() + ".html";
        }

        public String Title => metadata["Title"];

        public bool IsDraft() {
            string status = this.metadata.GetValueOrDefault("Status", "ok");
            return (status.ToLower() == "draft");
        }

        public string GetFSTitle() {
            string title = this.metadata["Title"].ToLowerInvariant();
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

        public Dictionary<string, string> metadata = new Dictionary<string, string>();
        public string path;
        public string year;
        public string markdownText;
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
                        blogArchive.years.Last().posts.Add(post);
                    }
                }
            }
        }

        public BlogArchive BlogArchive => blogArchive;

        private BlogArchive blogArchive;
        private string rootPath;
    }

}