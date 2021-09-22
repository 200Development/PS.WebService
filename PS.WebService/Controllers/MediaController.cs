using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PS.WebService.Library;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PS.WebService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MediaController : Controller
    {
      //  private static MediaServicesManagementClient MediaServicesManagementClient;
        private const long MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10GB


        public MediaController()
        {
           // System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }


        [HttpGet]
        public ActionResult Test()
        {
            return Ok();
        }
        
        
        [HttpPost]
        //[RequestSizeLimit(MaxFileSize)] 
        //[RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        public IActionResult UploadSingleFileAsync()
        {
            try
            {
            //    return Ok(await MediaServicesManagementClient.CreateInputAssetAsync(fileName, filePath));
            return Ok();
            }
            catch (Exception e)
            {
                return BadRequest();
            }
        }

        //private async Task CreateClientAndConnectAsync()
        //{
        //    ConfigWrapper config = new ConfigWrapper(new ConfigurationBuilder()
        //                 .SetBasePath(Directory.GetCurrentDirectory())
        //                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        //                 .AddEnvironmentVariables()
        //                 .Build());

        //    MediaServicesManagementClient = new MediaServicesManagementClient(config);
        //    await MediaServicesManagementClient.Connect();
        //}
    }

    public class Asset
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
