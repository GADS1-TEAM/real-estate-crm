using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SupplyService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts(CancellationToken ct)
    {
        var sampleProducts = new[]
        {
            new
            {
                productId = "prod-1",
                listingId = "lst-101",
                propertyId = "prop-001",
                title = "Departamento 3 Ambientes en Palermo",
                operationTypeCode = "VENTA",
                price = new { amount = 145000, currency = "USD" },
                status = "ACTIVE"
            },
            new
            {
                productId = "prod-2",
                listingId = "lst-102",
                propertyId = "prop-002",
                title = "Casa 4 Ambientes con Jardín en Belgrano",
                operationTypeCode = "VENTA",
                price = new { amount = 280000, currency = "USD" },
                status = "ACTIVE"
            }
        };

        return Ok(sampleProducts);
    }
}
