using Microsoft.AspNetCore.Mvc;
using System;
using CommercialService.Application.Commands;

namespace CommercialService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitsController : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateVisit([FromBody] CreateVisitCommand command)
        {
            return Ok(Guid.NewGuid());
        }
    }
}
