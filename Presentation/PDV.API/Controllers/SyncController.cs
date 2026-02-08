using Dotmim.Sync.Web.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PDV.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController(WebServerAgent webServerAgent, ILogger<SyncController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task Post()
    {
        try
        {
            logger.LogInformation("Sync request received from {User}",
                User.Identity?.Name ?? "Unknown");

            await webServerAgent.HandleRequestAsync(HttpContext);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during sync operation");
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(new { Message = "Sync failed", Error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Sync endpoint ready",
            Tables = new[]
            {
                new { Name = "Products", Direction = "DownloadOnly" },
                new { Name = "Sales", Direction = "Bidirectional" },
                new { Name = "SaleItems", Direction = "Bidirectional" },
                new { Name = "Payments", Direction = "Bidirectional" },
                new { Name = "Operators", Direction = "DownloadOnly" },
                new { Name = "FiscalTransactions", Direction = "Bidirectional" },
                new { Name = "FiscalConfigurations", Direction = "Bidirectional" },
            },
            Timestamp = DateTime.UtcNow

        });
    }
}
