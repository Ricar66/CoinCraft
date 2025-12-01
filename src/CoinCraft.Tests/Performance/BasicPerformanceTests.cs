using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Moq;
using CoinCraft.Services.Licensing;

namespace CoinCraft.Tests.Performance
{
    public class BasicPerformanceTests
    {
        [Fact]
        public async Task LicensingService_Validate_ShouldBeFast_WithMock()
        {
            var api = new Mock<ILicenseApiClient>();
            api.Setup(x => x.ValidateLicenseAsync(It.IsAny<string?>(), It.IsAny<string?>()))
               .ReturnsAsync(new LicenseValidationResult { IsValid = true, Message = "OK" });

            var svc = new LicensingService(api.Object);

            var sw = Stopwatch.StartNew();
            var result = await svc.EnsureLicensedAsync(() => Task.FromResult<string?>("FAKE-KEY"));
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds < 2000);
        }
    }
}
