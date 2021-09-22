using Microsoft.AspNetCore.Mvc;
using System;

namespace PS.WebService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UploadController : Controller
    {
        [HttpPost]
        public IActionResult UploadVideo([FromForm] dynamic video)
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
            return Ok();
        }
    }
}
