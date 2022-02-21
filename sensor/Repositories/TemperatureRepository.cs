using Dapper;
using Microsoft.Data.Sqlite;
using sensor.Dtos;
using sensor.Helpers;

namespace sensor.Repositories
{
    public class TemperatureRepository : ITemperatureRepository
    {
        public readonly IDatabaseProvider _databaseProvider;

        public TemperatureRepository(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
        }
        /// <summary>
        /// return the last 15 temperatures stored
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<float>> GetLastTemperaturesAsync()
        {
            using var connection = _databaseProvider.GetConnection();
            return await connection.QueryAsync<float>("SELECT temperature FROM (SELECT * FROM Temperatures ORDER BY ID DESC LIMIT 15) ORDER BY ID");
        }

        /// <summary>
        /// store the temperature requested
        /// </summary>
        /// <param name="temperature"></param>
        public async Task StoreTemperatureAsync(float temperature)
        {
            using var connection = _databaseProvider.GetConnection();
            await connection.ExecuteAsync("INSERT INTO Temperatures (Temperature) VALUES (@temperature)", new { temperature = temperature });
        }

        /// <summary>
        /// Will store the thresholds
        /// </summary>
        public async Task StoreThresholdsAsync(Thresholds thresholds)
        {
            using var connection = _databaseProvider.GetConnection();
            await connection.ExecuteAsync("UPDATE Thresholds SET Cold=@cold, Hot=@hot", new { cold = thresholds.Cold, hot = thresholds.Hot });
        }

        public async Task<Thresholds> GetThresholdsAsync()
        {
            using var connection = _databaseProvider.GetConnection();
            return await connection.QuerySingleAsync<Thresholds>("SELECT cold, hot FROM thresholds LIMIT 1;");
        }
    }
}
