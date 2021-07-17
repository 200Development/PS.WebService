using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;


namespace PS.WebService.Controllers
{
    [Route("[controller]/[action]")]
    [EnableCors("MyPolicy")]
    public class UploadController : Controller
    {

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Upload([FromForm] object form)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

                // 
                if (Request.Form.Files.Count < 1) return BadRequest();
                var file = Request.Form.Files[0];

                // Return bad request if no file was uploaded
                if (file.Length <= 0) return BadRequest();
               
                var localPath = "./data/";
                var fileName = "Test" + Guid.NewGuid() + ".mp4";
                var localFilePath = Path.Combine(localPath, fileName);
                Directory.CreateDirectory(localPath);

                if (!System.IO.File.Exists(Path.GetFullPath(localFilePath)))
                {
                    await using (var stream = System.IO.File.Create(localFilePath))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                // Write media to file

                // Get Azure blob
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                var containerClient = blobServiceClient.GetBlobContainerClient("videos");
                var blobClient = containerClient.GetBlobClient(fileName);

                // Upload media to Azure blob
                await using (FileStream uploadFileStream =
                    new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await blobClient.UploadAsync(uploadFileStream, true);
                    uploadFileStream.Close();
                }


                return Ok();


            }
            catch (Exception e)
            {
                return StatusCode(500, $"Internal Server Error: {e}");
            }
        }
    }
}
