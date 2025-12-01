using System;
using System.IO;
using System.Threading.Tasks;
using CoinCraft.Services.Licensing;
using FluentAssertions;
using Moq;
using Xunit;

namespace CoinCraft.Tests.Services;

public class LicensingServiceTests
{
    private readonly Mock<ILicenseApiClient> _apiClientMock;
    private readonly LicensingService _service;

    public LicensingServiceTests()
    {
        // Clean up any existing license file to ensure clean state
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, "CoinCraft", "license.dat");
        if (File.Exists(path)) File.Delete(path);

        _apiClientMock = new Mock<ILicenseApiClient>();
        
        // Default behavior: Invalid for any unknown key (including nulls)
        _apiClientMock.Setup(x => x.ValidateLicenseAsync(It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new LicenseValidationResult { IsValid = false, Message = "Mock Default Invalid" });

        _service = new LicensingService(_apiClientMock.Object);
    }

    [Fact]
    public async Task EnsureLicensedAsync_ShouldReturnInvalid_WhenNoKeyProvided()
    {
        // Arrange
        var provider = new Func<Task<string?>>(() => Task.FromResult<string?>(null));

        // Act
        var result = await _service.EnsureLicensedAsync(provider);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Nenhuma licença fornecida");
    }

    [Fact]
    public async Task EnsureLicensedAsync_ShouldReturnValid_WhenApiReturnsValid()
    {
        // Arrange
        var key = "VALID-KEY";
        var provider = new Func<Task<string?>>(() => Task.FromResult<string?>(key));
        
        // Use IsAny<string?> to match any fingerprint including null
        _apiClientMock.Setup(x => x.ValidateLicenseAsync(key, It.IsAny<string?>()))
            .ReturnsAsync(new LicenseValidationResult { IsValid = true, License = new CoinCraft.Services.Licensing.License() });

        _apiClientMock.Setup(x => x.RegisterInstallationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.EnsureLicensedAsync(provider);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue($"API should return valid for key {key}");
    }
}