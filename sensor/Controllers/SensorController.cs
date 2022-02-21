using Microsoft.AspNetCore.Mvc;
using sensor.Dtos;
using sensor.Services;

namespace sensor.Controllers;

[ApiController]
[Route("[controller]")]
public class SensorController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ISensorService _sensorService;

    public SensorController(ILogger<SensorController> logger, ISensorService sensorService)
    {
        _logger = logger;
        _sensorService = sensorService;
    }

    [HttpGet("ping")]
    public string Ping()
    {
        return "pong";
    }

    [HttpGet]
    public async Task<ContentResult> GetAsync()
    {
        return Content((await _sensorService.GetSensorStatusAsync()).ToString());
    }

    [HttpGet("history")]
    public async Task<IEnumerable<SensorStatus>> GetHistoryAsync()
    {
        return await _sensorService.GetSensorRequestHistoryAsync();
    }

    [HttpPut("thresholds")]
    public async Task SetThresholdsAsync([FromBody] Thresholds thresholds)
    {
        await _sensorService.SetThresholdsAsync(thresholds);
    }
}
