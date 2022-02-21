using sensor.Dtos;

namespace sensor.Repositories
{
    public interface ITemperatureRepository
    {
        Task StoreTemperatureAsync(float temperature);
        Task<IEnumerable<float>> GetLastTemperaturesAsync();
        Task StoreThresholdsAsync(Thresholds thresholds);
        Task<Thresholds> GetThresholdsAsync();
    }
}