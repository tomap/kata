namespace sensor.Repositories
{
    public class TemperatureCaptor : ITemperatureCaptor
    {
        public float ReadTemperature()
        {
            // temps between -30° and 70°
            return Random.Shared.NextSingle() * 100 - 30;
        }
    }
}
