using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlob_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public FileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost(nameof(UploadFile))]
        public async Task<IActionResult> UploadFile(List<IFormFile> files, String FolderName)
        {
            //string systemFileName = files.FileName;
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
            // This also does not make a service call; it only creates a local object.
            //CloudBlockBlob blockBlob = container.GetBlockBlobReference(systemFileName);

            var urls = new List<string>();

            foreach (var file in files)
            {
                String filename = file.FileName;
                String complete_name = FolderName + "/" + filename;
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(complete_name);
                await using (var data = file.OpenReadStream())
                {
                    await blockBlob.UploadFromStreamAsync(data);
                }
            }

            return Ok("Files Uploaded Successfully");


            /*await using (var data = files.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(data);
            }
           */
        }



        [HttpPost(nameof(ListFiles))]
        public List<String> ListFiles(String FolderName)
        {

            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));

            /*String prefix = FolderName + "/";*/

            CloudBlobDirectory Dir = container.GetDirectoryReference(FolderName);

            var res = new List<string>();

            BlobContinuationToken continuationToken = null;

            do
            {

                var listingResult = Dir.ListBlobsSegmentedAsync(continuationToken).Result;
                continuationToken = listingResult.ContinuationToken;
                res.AddRange(listingResult.Results.Select(x => System.IO.Path.GetFileName(x.Uri.AbsolutePath)).ToList());
            }
            while (continuationToken != null);

            return res;

      }


    


        [HttpPost(nameof(DownloadFile))]
        public async Task<IActionResult> DownloadFile(String FolderName, String fileName)
        /*{
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            // Retrieve storage account from connection string.
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            // Create the blob client.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();
            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));

            CloudBlobDirectory dir = container.GetDirectoryReference(dir_name);

            List<IListBlobItem> blobs = dir.ListBlobsSegmentedAsync().ToList();

            foreach (IListBlobItem item in blobs)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                await using MemoryStream memoryStream = new();
                await blob.DownloadToStreamAsync(memoryStream);
            }





        }
*/        
        {
            CloudBlockBlob blockBlob;
            await using (MemoryStream memoryStream = new MemoryStream())
            {
                string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(_configuration.GetValue<string>("BlobContainerName"));
                String complete_name = FolderName + "/" + fileName;
                blockBlob = cloudBlobContainer.GetBlockBlobReference(complete_name);
                await blockBlob.DownloadToStreamAsync(memoryStream);
            }

            Stream blobStream = blockBlob.OpenReadAsync().Result;
            return File(blobStream, blockBlob.Properties.ContentType, blockBlob.Name);
        }
        [HttpDelete(nameof(DeleteFile))]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            string blobstorageconnection = _configuration.GetValue<string>("BlobConnectionString");
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(blobstorageconnection);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            string strContainerName = _configuration.GetValue<string>("BlobContainerName");
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(strContainerName);
            var blob = cloudBlobContainer.GetBlobReference(fileName);
            await blob.DeleteIfExistsAsync();
            return Ok("File Deleted");
        }
    }
}

