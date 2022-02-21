using System.Data.Common;

namespace sensor.Helpers
{
    public interface IDatabaseProvider
    {
        DbConnection GetConnection();
    }
}
