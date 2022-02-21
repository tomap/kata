using Moq;
using NUnit.Framework;
using sensor.Dtos;
using sensor.Repositories;
using sensor.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sensor.Tests
{
    public class SensorServiceFixture
    {
        private SensorService sensorService;
        private Mock<ITemperatureCaptor> mockedCaptor;
        private Mock<ITemperatureRepository> mockTemperatureRepository;

        [SetUp]
        public void Setup()
        {
            mockedCaptor = new Mock<ITemperatureCaptor>();
            mockTemperatureRepository = new Mock<ITemperatureRepository>();
            sensorService = new SensorService(mockedCaptor.Object, mockTemperatureRepository.Object);
            mockTemperatureRepository.Setup(t => t.GetThresholdsAsync()).ReturnsAsync(new Thresholds { Cold = 22F, Hot = 40F });
        }

        [Test]
        public async Task ReturnATemperature()
        {
            var t = await sensorService.GetTemperatureFromCaptorAsync();
            Assert.True(t < float.MaxValue && t > float.MinValue);
        }

        [Test]
        public async Task ReturnTemperatureFromCaptor()
        {
            const float expectedTemp = 42;
            mockedCaptor.Setup(c => c.ReadTemperature()).Returns(expectedTemp);
            var t = await sensorService.GetTemperatureFromCaptorAsync();
            Assert.AreEqual(expectedTemp, t);
        }

        [Test]
        public async Task ReturnAStatus()
        {
            var status = await sensorService.GetSensorStatusAsync();
            CollectionAssert.Contains(Enum.GetValues(typeof(SensorStatus)), status);
        }

        [TestCase(21.9F, SensorStatus.COLD)]
        [TestCase(22.1F, SensorStatus.WARM)]
        [TestCase(39.9F, SensorStatus.WARM)]
        [TestCase(40.1F, SensorStatus.HOT)]
        public async Task ReturnColdStatusDependingOnTemperature(float temperature, SensorStatus expectedStatus)
        {
            mockedCaptor.Setup(c => c.ReadTemperature()).Returns(temperature);
            var status = await sensorService.GetSensorStatusAsync();
            Assert.AreEqual(expectedStatus, status);
        }


        [Test]
        public async Task ReturnEmptyHistoryWhenNoRequest()
        {
            var history = await sensorService.GetSensorRequestHistoryAsync();
            Assert.IsEmpty(history);
        }


        [TestCase(21.9F, SensorStatus.COLD)]
        [TestCase(22.1F, SensorStatus.WARM)]
        [TestCase(39.9F, SensorStatus.WARM)]
        [TestCase(40.1F, SensorStatus.HOT)]
        public async Task ReturnOneHistoryWhenOneHistoryStored(float temperature, SensorStatus expectedStatus)
        {
            mockTemperatureRepository.Setup(t => t.GetLastTemperaturesAsync())
                .ReturnsAsync(new List<float>() { temperature });
            var history = await sensorService.GetSensorRequestHistoryAsync();
            Assert.IsNotEmpty(history);
            CollectionAssert.AreEquivalent(new List<SensorStatus>() { expectedStatus }, history);
        }

        [TestCase(1F, 2F, true)]
        [TestCase(1F, 1F, false)]
        [TestCase(1F, 0F, false)]
        public async Task CheckConsistencyOfThreshold(float cold, float hot, bool shouldBeAccepted)
        {
            var thresholds = new Thresholds() { Cold = cold, Hot = hot };
            if (shouldBeAccepted)
                await sensorService.SetThresholdsAsync(thresholds);
            else
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await sensorService.SetThresholdsAsync(thresholds));
        }


        [Test]
        public async Task WillStoreThresholds()
        {
            const float cold = 18F;
            const float hot = 42F;
            var thresholds = new Thresholds() { Cold = cold, Hot = hot };
            mockTemperatureRepository.Setup(c => c.StoreThresholdsAsync(thresholds)).Verifiable();
            await sensorService.SetThresholdsAsync(thresholds);

            mockTemperatureRepository.Verify();
        }

        [TestCase(10F, 20F, 30F, SensorStatus.HOT)]
        [TestCase(10F, 20F, 15F, SensorStatus.WARM)]
        [TestCase(10F, 20F, 5F, SensorStatus.COLD)]
        public async Task ReturnExpectedTempAfterThresholdsChanged(float coldThreshold, float hotThreshold, float captor, SensorStatus expectedSensorStatus)
        {
            var thresholds = new Thresholds() { Cold = coldThreshold, Hot = hotThreshold };
            mockTemperatureRepository.Setup(t => t.GetThresholdsAsync()).ReturnsAsync(thresholds);
            mockedCaptor.Setup(c => c.ReadTemperature()).Returns(captor);

            var status = await sensorService.GetSensorStatusAsync();
            Assert.AreEqual(expectedSensorStatus, status);
        }
    }
}