using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Stripe;

namespace Lexvor.API.Services
{
    public static class BlobService {
        public static async Task<string> DownloadBlobAsText(string blobUrl, OtherSettings settings, bool sensitive = false) {
	        var cloudStorageAccount = new CloudStorageAccount(new StorageCredentials("lexvorassets", settings.BlobKey), true);
	        var blobClient = cloudStorageAccount.CreateCloudBlobClient();
			var containerName = sensitive ? settings.BlobContainerSensitive : settings.BlobContainer;
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
	        CloudBlockBlob blob = container.GetBlockBlobReference(blobUrl);
	        return await blob.DownloadTextAsync();
        }
        public static async Task<byte[]> DownloadBlobBytes(string blobUrl, OtherSettings settings, bool sensitive = false) {
	        var cloudStorageAccount = new CloudStorageAccount(new StorageCredentials("lexvorassets", settings.BlobKey), true);
	        var blobClient = cloudStorageAccount.CreateCloudBlobClient();
	        var containerName = sensitive ? settings.BlobContainerSensitive : settings.BlobContainer;
	        CloudBlobContainer container = blobClient.GetContainerReference(containerName);
	        CloudBlockBlob blob = container.GetBlockBlobReference(blobUrl);
	        var stream = new MemoryStream();
	        await blob.DownloadToStreamAsync(stream);
	        return stream.ToArray();
        }

	    public static async Task<string> GetPrivateBlobUrl(string blobUrl, OtherSettings settings) {
			var blobClient = new CloudBlobClient(new Uri(settings.BlobUri), new StorageCredentials("lexvorassets", settings.BlobKey));
		    CloudBlobContainer container = blobClient.GetContainerReference(settings.BlobContainerSensitive);
		    var blob = container.GetBlockBlobReference(blobUrl);
			var readPolicy = new SharedAccessBlobPolicy() {
				Permissions = SharedAccessBlobPermissions.Read,
				SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromMinutes(1)
			};
		    return $"{blob.Uri.AbsoluteUri}{blob.GetSharedAccessSignature(readPolicy)}";
	    }

        public static async Task UploadBlob(byte[] data, string blobUrl, OtherSettings settings, bool sensitive = false) {
            var blobClient = new CloudBlobClient(new Uri(settings.BlobUri), new StorageCredentials("lexvorassets", settings.BlobKey));
            CloudBlobContainer container = blobClient.GetContainerReference(sensitive ? settings.BlobContainerSensitive : settings.BlobContainer);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobUrl);
            await blob.UploadFromByteArrayAsync(data, 0, data.Length);
        }

        public static async Task DeleteBlob(string blobUrl, OtherSettings settings, bool sensitive = false) {
            var blobClient = new CloudBlobClient(new Uri(settings.BlobUri), new StorageCredentials("lexvorassets", settings.BlobKey));
            CloudBlobContainer container = blobClient.GetContainerReference(sensitive ? settings.BlobContainerSensitive : settings.BlobContainer);
            CloudBlockBlob blob = container.GetBlockBlobReference(blobUrl);
            await blob.DeleteAsync();
        }
    }
}
