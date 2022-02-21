using System.Data.Common;

namespace sensor.Helpers
{
    public interface IDatabaseBootstrap
    {
        void Setup();
    }
}
