using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OperationsBff.Api.Controllers;
using RealEstateCrm.Contracts.Opportunities;
using Xunit;

namespace OperationsBff.Api.Tests.Controllers;

public class OpportunitiesControllerTests
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var controller = new OpportunitiesController();
        var result = await controller.Get(null, null, null, null);
        Assert.IsType<OkObjectResult>(result);
    }
}
