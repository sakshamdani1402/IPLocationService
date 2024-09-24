using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using IPLocationService.Models;
using Location.Models;
using Location.Interfaces.Services;
using Location.Utilities;
using System.Net;

namespace IPLocationService.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IProviderSelectorLogic _providerSelectorLogic;
    private readonly IProviderCallerLogic _providerCallerLogic;


    public HomeController(ILogger<HomeController> logger, IProviderSelectorLogic providerSelectorLogic, IProviderCallerLogic providerCallerLogic)
    {
        _logger = logger;
        _providerSelectorLogic = providerSelectorLogic;
        _providerCallerLogic = providerCallerLogic;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Resume()
    {
        return View();
    }

    /// <summary>
    /// Retrieves location information based on the provided IP address.
    /// </summary>
    /// <param name="ip">The IP address to look up.</param>
    /// <returns>The JSON response from the IP location provider.</returns>
    [HttpGet]
    [Route("api/location")]
    [RateLimitAttribute(200, 12000)] //  200 per minute and 12000 per hour
    public async Task<IActionResult> GetLocation(string ip)
    {
        // Check if the IP address is null or whitespace
        if (string.IsNullOrWhiteSpace(ip))
        {
            return BadRequest("IP address cannot be null or empty.");
        }
        if(!UtilityFunctions.IsValidIpAddress(ip))
        {
            return BadRequest("Invalid IP address");
        }
        try
        {
            Provider provider = await _providerSelectorLogic.GetBestProviderAsync();
            var response = await _providerCallerLogic.CallProviderApiAsync(provider, ip);
            return Content(response, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving location information.");
            return StatusCode((int)HttpStatusCode.InternalServerError, "An error occurred while processing your request.");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
