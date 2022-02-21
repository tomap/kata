using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using sensor.Dtos;
using sensor.Helpers;
using sensor.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sensor.Tests
{
    [TestFixture]
    public class SensorControllerCaptorFixture 
    {
        private IDatabaseProvider dbProv;
        private Mock<ITemperatureCaptor> _mockCaptorRepository;
        private WebApplicationFactory<Program> application;
        private HttpClient client;

        [Test]
        public async Task BasicTestCaptor()
        {
            {
                using var cnn = dbProv.GetConnection();
                // prepare DB
                await cnn.ExecuteAsync("UPDATE Thresholds SET Cold=1, Hot=2;");
            }

            var resp = await client.GetAsync("Sensor/ping");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            // assert on query result
            Assert.AreEqual("pong", content);

            {
                using var cnn = dbProv.GetConnection();
                // assert on db
                var (cold, hot) = await cnn.QuerySingleAsync<(float cold, float hot)>("SELECT Cold, Hot FROM Thresholds");
                Assert.AreEqual(1F, cold);
                Assert.AreEqual(2F, hot);
            }
        }

        [Test]
        public async Task TestCaptorMocked()
        {
            // To test thresholds, we are going to mock the captor. otherwise, we get random values
            _mockCaptorRepository.Setup(c => c.ReadTemperature()).Returns(10F);
            var resp = await client.GetAsync("Sensor");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            var value = Enum.Parse<SensorStatus>(content);
            Assert.AreEqual(SensorStatus.COLD, value);
        }

        [Test]
        public async Task TestChangeThresholds()
        {
            var thresholds = new Thresholds { Cold = 5F, Hot = 15F };
            var resp = await client.PutAsync("Sensor/thresholds", new StringContent(JsonSerializer.Serialize(thresholds), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            // To test thresholds, we are going to mock the captor. otherwise, we get random values
            _mockCaptorRepository.Setup(c => c.ReadTemperature()).Returns(10F);
            resp = await client.GetAsync("Sensor");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            var value = Enum.Parse<SensorStatus>(content);
            Assert.AreEqual(SensorStatus.WARM, value);
        }

        private static T? Deserialize<T>(string content)
        {
            var opt = new JsonSerializerOptions();
            opt.Converters.Add(new JsonStringEnumConverter());
            var obj = JsonSerializer.Deserialize<T>(content, opt);
            return obj;
        }

        [SetUp]
        public void InitApp()
        {
            _mockCaptorRepository = new Mock<ITemperatureCaptor>();
            application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var descriptor = services.SingleOrDefault(
                                            d => d.ServiceType ==
                                                typeof(ITemperatureCaptor));

                        services.Remove(descriptor);
                        services.AddTransient(t => _mockCaptorRepository.Object);

                        services.Configure<ConnectionStrings>(opts =>
                        {
                            opts.Sqlite = $"Data Source={Guid.NewGuid()}.sqlite";
                        });
                    });
                });
            dbProv = application.Services.GetRequiredService<IDatabaseProvider>();
            client = application.CreateClient();
            Assert.IsNotNull(dbProv);
        }

        [TearDown]
        public void CloseApp()
        {
            client.Dispose();
            application.Dispose();
        }
    }
}
