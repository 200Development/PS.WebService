using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;


namespace PS.WebService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UploadController : Controller
    {

        [HttpPost]
        {
            try
            {
                return Ok();
            }
            catch(Exception e)
                    {
                return StatusCode(500, e);
                    }
                }

        [HttpGet]
        public IActionResult Test()
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
