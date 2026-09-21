using Microsoft.AspNetCore.Mvc;

namespace CommercialService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NegotiationsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
