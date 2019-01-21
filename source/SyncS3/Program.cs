using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime.CredentialManagement;

namespace SyncS3
{
    class LocalFileInfo {
        public LocalFileInfo(string path, string rootPath) {
            Trace.Assert(path.StartsWith(rootPath));
            relativePath = path.Substring(rootPath.Length);
            relativePath = relativePath.TrimStart('/');
            fullPath = path;
            contentType = GetContentType(path);
        }

        // NB: Does not start with /
        public string relativePath;

        public string contentType;

        public string fullPath;

        // All file types on our site.  Simple and consistent - no MIME sniffing.
        static string GetContentType(string path) {
            string extension = Path.GetExtension(path);
            switch (extension.ToLower()) {
                case ".html": return "text/html";
                case ".js":  return "application/javascript";
                case ".css": return "text/css";
                case ".jpg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".ico": return "image/vnd.microsoft.icon";
                case ".xml": return "application/xml";
                case ".pdf": return "application/pdf";
                default:
                    throw new Exception($"Unknown file extension: {path}");
            }
        }
    }

    class RemoteFileInfo {
        public S3Object obj;
        public bool needs_delete = false;
        public bool needs_upload = false;
    }

    class Program
    {
        // Usage: dotnet run -- /root/path BucketName
        static void Main(string[] args)
        {
            var rootPath = args[0];
            var bucketName = args[1];

            // NB: Region must be in the *credentials* file, not the config.
            // The .NET SDK only looks at one file, apparently.
            var client = new AmazonS3Client();
            var localFiles = ListLocalDirectory(rootPath);
            var remoteFiles = ScanBucket(client, bucketName);

            var remoteKeysProcessed = new HashSet<String>();

            foreach (var remoteFile in remoteFiles) {
                LocalFileInfo localFile;
                var key = remoteFile.obj.Key;
                localFiles.TryGetValue(key, out localFile);
                if (localFile == null) {
                    Console.WriteLine("Key needs delete: {0}", key);
                    remoteFile.needs_delete = true;
                } else {
                    Console.WriteLine("Key needs update: {0}", key);
                    remoteFile.needs_upload = true;
                }
                remoteKeysProcessed.Add(key);
            }

            foreach (var localFile in localFiles.Values) {
                if (!remoteKeysProcessed.Contains(localFile.relativePath)) {
                    Console.WriteLine("New file needs upload: {0}", localFile.relativePath);
                }
            }
        }

        static Dictionary<string, LocalFileInfo> ListLocalDirectory(string rootPath) 
        {
            var ret = new Dictionary<string, LocalFileInfo>();
            foreach (var f in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)) {
                var info = new LocalFileInfo(f, rootPath);
                Console.WriteLine("File {0}", info.relativePath);
                ret.Add(info.relativePath, info);
            }
            return ret;
        }

        // Iterate all objects in the S3 bucket.
        // If an object is identical, mark it as not needing upload.
        // If an object is no longer needed, mark it for deletion.
        static List<RemoteFileInfo> ScanBucket(AmazonS3Client client, string bucketName)
        {
            ListObjectsV2Request request = new ListObjectsV2Request {
                BucketName = bucketName,
                MaxKeys = 1000
            };
            ListObjectsV2Response response;
            var ret = new List<RemoteFileInfo>();

            do {
                response = client.ListObjectsV2Async(request).Result;

                // Process the response.
                foreach (S3Object entry in response.S3Objects) {
                    Trace.Assert(!entry.Key.StartsWith("/"));
                    Console.WriteLine("key = {0} size = {1}",
                        entry.Key, entry.Size);
                    
                    var remoteInfo = new RemoteFileInfo { obj = entry };
                    ret.Add(remoteInfo);
                }
                Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return ret;
        }
    }
}
