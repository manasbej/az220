using Microsoft.AspNetCore.Mvc;
namespace TodoApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AzureDeviceController : ControllerBase
{
    private readonly ILogger<AzureDeviceController> _logger;
    private readonly IDeviceManagerService _deviceManagerService;
    public AzureDeviceController(IDeviceManagerService deviceManagerService, ILogger<AzureDeviceController> logger)
    {
        _logger = logger;
        _deviceManagerService = deviceManagerService;
    }

    [HttpPost("CreateDeviceAsync")]
    public async Task<IActionResult> CreateDeviceAsync(string deviceName)
    {        
        return Ok(await _deviceManagerService.CreateDeviceAsync(deviceName));
    }
    [HttpGet("GetAllDeviceListAsync")]
    public async Task<IActionResult> GetDeviceListAsync()
    {
        return Ok(await _deviceManagerService.GetDeviceListAsync() );
    }
    [HttpDelete("DeleteDeviceAsync")]
    public async Task<IActionResult> DeleteDeviceAsync(string deviceId)
    {
        return Ok(await _deviceManagerService.DeleteDeviceAsync(deviceId));
    }

    [HttpPut("UpdateDesiredPropertiesAsync")]
    public async Task<IActionResult> UpdateDesiredPropertiesAsync(string deviceId)
    {
        return Ok(await _deviceManagerService.UpdateDesiredPropertiesAsync(deviceId));
    }
    [HttpPut("UpdateReportedPropertiesAsync")]
    public async Task<IActionResult> UpdateReportedPropertiesAsync(string deviceId)
    {
        return Ok(await _deviceManagerService.UpdateReportedPropertiesAsync(deviceId));
    }
    [HttpPost("SendDeviceTelemetryMessagesAsync")]
    public async Task<IActionResult> SendDeviceTelemetryMessagesAsync(string deviceId)
    {
        return Ok(await _deviceManagerService.SendDeviceTelemetryMessagesAsync(deviceId));
    }
}