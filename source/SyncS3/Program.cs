using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime.CredentialManagement;

namespace SyncS3
{
    // This class is immutable.  Aren't autoproperties nice!
    class LocalFileInfo {
        public LocalFileInfo(string path, string rootPath) {
            Trace.Assert(path.StartsWith(rootPath));
            RelativePath = path.Substring(rootPath.Length);
            RelativePath = RelativePath.TrimStart('/');
            Trace.Assert(!RelativePath.StartsWith("/"));
            Trace.Assert(!String.IsNullOrWhiteSpace(RelativePath));
            FullLocalPath = path;
            ContentType = GetContentType(path);
        }

        // NB: Does not start with /
        public string RelativePath { get; }

        public string ContentType { get; }

        public string FullLocalPath { get; }

        public string CacheControl { 
            get {
                switch (ContentType) {
                    case "text/html": return "public, max-age=60";
                    default: return "public, max-age=86400";
                }
            }
        }

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


            // Step 1: Upload new files
            foreach (var localFile in localFiles.Values) {
                if (!remoteFiles.ContainsKey(localFile.RelativePath)) {
                    Console.WriteLine("New file needs upload: {0}", localFile.RelativePath);
                    upload_file(client, localFile, bucketName);
                }
            }

            // Step 2: Overwrite changed files
            foreach (var remoteFile in remoteFiles.Values) {
                LocalFileInfo localFile;
                var key = remoteFile.Key;
                localFiles.TryGetValue(key, out localFile);
                if (localFile != null) {
                    Console.WriteLine("Key needs update: {0}", key);
                    upload_file(client, localFile, bucketName);
                }
            }

            // Step 3: Delete files that have been removed
            foreach (var remoteFile in remoteFiles.Values) {
                LocalFileInfo localFile;
                var key = remoteFile.Key;
                localFiles.TryGetValue(key, out localFile);
                if (localFile == null) {
                    Console.WriteLine("Key needs delete: {0}", key);
                }
            }
        }

        static void upload_file(AmazonS3Client client, LocalFileInfo localFile, String bucketName)
        {
            var putRequest = new PutObjectRequest {
                BucketName = bucketName,
                Key = localFile.RelativePath,
                FilePath = localFile.FullLocalPath,
                ContentType = localFile.ContentType
            };
            putRequest.Headers.CacheControl = localFile.CacheControl;

            PutObjectResponse response = client.PutObjectAsync(putRequest).Result;
        }

        // Scan all files in a local directory.
        // Return: path (relative to rootPath) -> object info
        static Dictionary<string, LocalFileInfo> ListLocalDirectory(string rootPath) 
        {
            var ret = new Dictionary<string, LocalFileInfo>();
            foreach (var f in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)) {
                var info = new LocalFileInfo(f, rootPath);
                Console.WriteLine("File {0}", info.RelativePath);
                ret.Add(info.RelativePath, info);
            }
            return ret;
        }

        // Scan all objects in the S3 bucket.
        // Return: key (string) -> object info
        static Dictionary<String, S3Object> ScanBucket(AmazonS3Client client, string bucketName)
        {
            ListObjectsV2Request request = new ListObjectsV2Request {
                BucketName = bucketName,
                MaxKeys = 1000
            };
            ListObjectsV2Response response;
            var ret = new Dictionary<String, S3Object>();

            do {
                response = client.ListObjectsV2Async(request).Result;

                foreach (S3Object entry in response.S3Objects) {
                    Trace.Assert(!entry.Key.StartsWith("/"));
                    Console.WriteLine("key = {0} size = {1}", entry.Key, entry.Size);
                    ret.Add(entry.Key, entry);
                }

                Console.WriteLine("Next Continuation Token: {0}", response.NextContinuationToken);
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return ret;
        }
    }
}
