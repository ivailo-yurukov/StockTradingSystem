using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PriceService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("PriceService is running");
        }
    }
}
