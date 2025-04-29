using Microsoft.AspNetCore.Mvc;

namespace GameDeliveryPaaS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloWorldController : ControllerBase
    {
        [HttpGet("GetHelloWorld")]
        public IActionResult GetHelloWorld()
        {
            return Ok("Hello World!");
        }
    }
}