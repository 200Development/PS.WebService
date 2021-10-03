using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PS.WebService.Library;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PS.WebService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UploadController : Controller
    {

        [HttpPost]
        public async Task<IActionResult> UploadVideoAsync([FromForm] IFormFile file)
        {

            try
            {
                ConfigWrapper config = new ConfigWrapper(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build());

                var azureMediaService = new MediaServicesManagementClient(config);

               

                if (file.Length > 0)
                {
                    var fileName = GetFileName(file);
                    var filePath = Path.Combine(config.UploadFolderPath, file.FileName);
                    using Stream fileStream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(fileStream);

                    var uploadResponse = await azureMediaService.CreateInputAssetAsync(file.FileName, filePath);
                }
                else
                {
                    return NoContent();
                }

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        private string GetFileName(IFormFile file)
        {
            return ContentDispositionHeaderValue
                    .Parse(file.ContentDisposition)
                    .FileName
                    .Trim('"');
        }
    }
}
