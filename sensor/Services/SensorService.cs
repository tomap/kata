using sensor.Dtos;
using sensor.Repositories;

namespace sensor.Services
{
    public class SensorService : ISensorService
    {
        private readonly ITemperatureCaptor _temperatureCaptor;
        private readonly ITemperatureRepository _temperatureRepository;
        
        public SensorService(ITemperatureCaptor temperatureCaptor , ITemperatureRepository temperatureRepository)
        {
            _temperatureCaptor = temperatureCaptor;
            _temperatureRepository = temperatureRepository;
        }

        public async Task<float> GetTemperatureFromCaptorAsync()
        {
            var temp = _temperatureCaptor.ReadTemperature();
            await _temperatureRepository.StoreTemperatureAsync(temp);
            return temp;
        }


        /// <summary>
        /// Return a status (hot/cold/warm) based upon the temperature
        /// </summary>
        /// <returns></returns>
        public async Task<SensorStatus> GetSensorStatusAsync()
        {
            var temp = await GetTemperatureFromCaptorAsync();
            return await ConvertTemperatureToStatusAsync(temp);
        }

        private async Task<SensorStatus> ConvertTemperatureToStatusAsync(float temp)
        {
            var thresholds = await _temperatureRepository.GetThresholdsAsync();
            if (temp > thresholds.Hot)
            {
                return SensorStatus.HOT;
            }
            if (temp < thresholds.Cold)
            {
                return SensorStatus.COLD;
            }
            return SensorStatus.WARM;
        }

        /// <summary>
        /// Returns the last 15 requests
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<SensorStatus>> GetSensorRequestHistoryAsync()
        {
            return await Task.WhenAll((await _temperatureRepository.GetLastTemperaturesAsync())
                .Select(ConvertTemperatureToStatusAsync));
        }

        /// <summary>
        /// Set the cold & hot threshold
        /// Will thow an exception is value are not cold below hot
        /// </summary>
        /// <param name="coldThreshold"></param>
        /// <param name="hotThreshold"></param>
        public async Task SetThresholdsAsync(Thresholds thresholds)
        {
            if(thresholds.Cold >= thresholds.Hot)
            {
                throw new ArgumentOutOfRangeException($"{nameof(thresholds)}.Cold should be lower than {nameof(thresholds)}.Hot");
            }
            await _temperatureRepository.StoreThresholdsAsync(thresholds);
        }
    }
}
