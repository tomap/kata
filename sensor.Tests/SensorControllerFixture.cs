using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using sensor.Dtos;
using sensor.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace sensor.Tests
{
    [TestFixture]
    public class SensorControllerFixture 
    {
        private IDatabaseProvider dbProv;
        private WebApplicationFactory<Program> application;
        private HttpClient client;

        [Test]
        public async Task BasicTest()
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
        public async Task TestGetSensorData()
        {
            var resp = await client.GetAsync("Sensor");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            // assert on query result
            Assert.Contains(content, new List<string> { "HOT", "COLD", "WARM" });

        }

        [Test]
        public async Task TestNoDataWhenNoCall()
        {
                using var cnn = dbProv.GetConnection();
                // assert on db
                var cnt = await cnn.QuerySingleAsync<int>("SELECT COUNT(*) FROM Temperatures");
                Assert.Zero(cnt);            
        }

        [Test]
        public async Task TestSingleDataWhenOneCall()
        {
            await client.GetAsync("Sensor");

            using var cnn = dbProv.GetConnection();
            // assert on db
            var cnt = await cnn.QuerySingleAsync<int>("SELECT COUNT(*) FROM Temperatures");
            Assert.AreEqual(1, cnt);
        }

        [Test]
        public async Task TestXDataWhenXCalls()
        {
            for (var i = 1; i <= 15; i++)
            {
                await client.GetAsync("Sensor");

                using var cnn = dbProv.GetConnection();
                // assert on db
                var cnt = await cnn.QuerySingleAsync<int>("SELECT COUNT(*) FROM Temperatures");
                Assert.AreEqual(i, cnt);
            }
        }

        [Test]
        public async Task TestNoHistoryWhenNoCalls()
        {
            var resp = await client.GetAsync("Sensor/history");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            // assert on query result
            Assert.AreEqual("[]", content);
        }

        [Test]
        public async Task TestOneHistoryWhenOneCall()
        {
            await client.GetAsync("Sensor");

            var resp = await client.GetAsync("Sensor/history");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            var obj = Deserialize< IEnumerable<SensorStatus>>(content);// assert on query result
            CollectionAssert.IsNotEmpty(obj);
            Assert.AreEqual(1, obj.Count());
            Assert.Contains(obj.First(), new List<SensorStatus> { SensorStatus.HOT, SensorStatus.COLD, SensorStatus.WARM });
        }

        [Test]
        public async Task TestOneHistoryMatchesOneCall()
        {
            var resp = await client.GetAsync("Sensor");
            resp.EnsureSuccessStatusCode();
            var content = await resp.Content.ReadAsStringAsync();
            var value = Enum.Parse<SensorStatus>(content);

            resp = await client.GetAsync("Sensor/history");
            resp.EnsureSuccessStatusCode();
            content = await resp.Content.ReadAsStringAsync();
            var obj = Deserialize<IEnumerable<SensorStatus>>(content);// assert on query result
            CollectionAssert.IsNotEmpty(obj);
            Assert.AreEqual(1, obj.Count());
            Assert.AreEqual(obj.First(), value);
        }

        [Test]
        public async Task TestXHistoryMatchesXCall()
        {
            var results = new List<SensorStatus>();
            for (var i = 1; i <= 15; i++) {
                var resp = await client.GetAsync("Sensor");
                resp.EnsureSuccessStatusCode();
                var content = await resp.Content.ReadAsStringAsync();
                
                results.Add(Enum.Parse<SensorStatus>(content));
                resp = await client.GetAsync("Sensor/history");
                resp.EnsureSuccessStatusCode();
                content = await resp.Content.ReadAsStringAsync();
                var obj = Deserialize<IEnumerable<SensorStatus>>(content);
                CollectionAssert.IsNotEmpty(obj);
                Assert.AreEqual(i, obj.Count());
                CollectionAssert.AreEqual(obj, results);
            }
        }

        [Test]
        public async Task TestHistoryLimitedTo15()
        {
            var results = new SizedQueue<SensorStatus>(15);

            for (var i = 1; i <= 15; i++)
            {
                var resp = await client.GetAsync("Sensor");
                resp.EnsureSuccessStatusCode();
                var content = await resp.Content.ReadAsStringAsync();

                results.Enqueue(Enum.Parse<SensorStatus>(content));
                resp = await client.GetAsync("Sensor/history");
                resp.EnsureSuccessStatusCode();
                content = await resp.Content.ReadAsStringAsync();
                var obj = Deserialize<IEnumerable<SensorStatus>>(content);
                CollectionAssert.IsNotEmpty(obj);
                Assert.AreEqual(i, obj.Count());
                CollectionAssert.AreEqual(obj, results);
            }

            for (var i = 1; i <= 15; i++)
            {
                var resp = await client.GetAsync("Sensor");
                resp.EnsureSuccessStatusCode();
                var content = await resp.Content.ReadAsStringAsync();

                results.Enqueue(Enum.Parse<SensorStatus>(content));
                resp = await client.GetAsync("Sensor/history");
                resp.EnsureSuccessStatusCode();
                content = await resp.Content.ReadAsStringAsync();
                var obj = Deserialize<IEnumerable<SensorStatus>>(content);
                CollectionAssert.IsNotEmpty(obj);
                Assert.AreEqual(15, obj.Count());
                CollectionAssert.AreEqual(obj, results);
            }

        }

        public sealed class SizedQueue<T> : Queue<T>
        {
            private int _capacity { get; }
            public SizedQueue(int fixedCapacity)
            {
                _capacity = fixedCapacity;
            }

            /// <summary>
            /// If the total number of item exceed the capacity, the oldest ones automatically dequeues.
            /// </summary>
            public new void Enqueue(T item)
            {
                base.Enqueue(item);
                if (Count > _capacity)
                {
                    Dequeue();
                }
            }
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
            application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
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
