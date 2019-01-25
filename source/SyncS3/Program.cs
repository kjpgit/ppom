using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
            MD5Checksum = GetMd5Hash(path);
        }

        // NB: Does not start with /
        public string RelativePath { get; }

        public string ContentType { get; }

        public string MD5Checksum { get; }

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

        static string GetMd5Hash(string filePath)
        {
            var md5Hash = System.Security.Cryptography.MD5.Create();
            byte[] data = md5Hash.ComputeHash(File.ReadAllBytes(filePath));

            var sBuilder = new System.Text.StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }

    public class Program
    {
        // Usage: dotnet run -- /root/path BucketName [--dry-run]
        //
        // Simple S3 sync tool. 
        // We don't even have to gzip compress because cloudfront does it
        // automatically for the correct content types (e.g. html, css, js).
        // 
        static async Task Main(string[] args)
        {
            var rootPath = args[0];
            var bucketName = args[1];
            var extra_args = args.Skip(2).ToList();
            bool dry_run = false;

            foreach (var arg in extra_args) {
                if (arg == "--dry-run") {
                    dry_run = true;
                } else {
                    throw new Exception($"Unknown option: ${arg}");
                }
            }

            // NB: Region must be in the *credentials* file, not the config.
            // The .NET SDK only looks at one file, apparently.
            var client = new AmazonS3Client();

            var localFiles = ListLocalDirectory(rootPath);
            var remoteFiles = await ScanBucket(client, bucketName);


            // Step 1: Upload new files
            foreach (var localFile in localFiles.Values) {
                if (!remoteFiles.ContainsKey(localFile.RelativePath)) {
                    Console.WriteLine("New file needs upload: {0}", localFile.RelativePath);
                    if (!dry_run) {
                        await upload_file(client, localFile, bucketName);
                    }
                }
            }

            // Step 2: Overwrite changed files
            foreach (var remoteFile in remoteFiles.Values) {
                var key = remoteFile.Key;
                localFiles.TryGetValue(key, out LocalFileInfo localFile);
                if (localFile != null) {
                    if (localFile.MD5Checksum.ToLower() != remoteFile.ETag.ToLower().Trim('"')) {
                        Console.WriteLine("Key needs update: {0}", key);
                        if (!dry_run) {
                            await upload_file(client, localFile, bucketName);
                        }
                    } else {
                        Console.WriteLine("Key unchanged: {0}", key);
                    }
                }
            }

            // Step 3: Delete files that have been removed
            foreach (var remoteFile in remoteFiles.Values) {
                var key = remoteFile.Key;
                localFiles.TryGetValue(key, out LocalFileInfo localFile);
                if (localFile == null) {
                    Console.WriteLine("Key needs delete: {0}", key);
                    if (!dry_run) {
                        await delete_file(client, key, bucketName);
                    }
                }
            }
        }

        static async Task upload_file(AmazonS3Client client, LocalFileInfo localFile, String bucketName)
        {
            var putRequest = new PutObjectRequest {
                BucketName = bucketName,
                Key = localFile.RelativePath,
                FilePath = localFile.FullLocalPath,
                ContentType = localFile.ContentType
            };
            putRequest.Headers.CacheControl = localFile.CacheControl;

            PutObjectResponse response = await client.PutObjectAsync(putRequest);
        }

        static async Task delete_file(AmazonS3Client client, String key, String bucketName)
        {
             var deleteObjectRequest = new DeleteObjectRequest {
                    BucketName = bucketName,
                    Key = key
                };

            DeleteObjectResponse response = await client.DeleteObjectAsync(deleteObjectRequest);
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
        static async Task<Dictionary<String, S3Object>> ScanBucket(
            AmazonS3Client client, string bucketName)
        {
            ListObjectsV2Request request = new ListObjectsV2Request {
                BucketName = bucketName,
                MaxKeys = 1000
            };
            ListObjectsV2Response response;
            var ret = new Dictionary<String, S3Object>();

            do {
                response = await client.ListObjectsV2Async(request);

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
