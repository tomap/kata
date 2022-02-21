using sensor.Dtos;

namespace sensor.Services;

public interface ISensorService
{
    Task<IEnumerable<SensorStatus>> GetSensorRequestHistoryAsync();
    Task<SensorStatus> GetSensorStatusAsync();
    Task SetThresholdsAsync(Thresholds thresholds);
}
