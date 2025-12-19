using BotCarniceria.Infrastructure.Services.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Services.Caching;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<MemoryCacheService>> _mockLogger;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<MemoryCacheService>>();
        _cacheService = new MemoryCacheService(_memoryCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingKey_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _cacheService.GetAsync<string>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithValidKeyAndValue_ShouldStoreValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _cacheService.SetAsync(null!, "value");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _cacheService.SetAsync<string>("key", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpireAfterTime()
    {
        // Arrange
        var key = "expiring-key";
        var value = "expiring-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        await Task.Delay(150);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithExistingKey_ShouldRemoveValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _cacheService.RemoveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ShouldRemoveAllMatchingKeys()
    {
        // Arrange
        await _cacheService.SetAsync("user:1", "value1");
        await _cacheService.SetAsync("user:2", "value2");
        await _cacheService.SetAsync("product:1", "value3");

        // Act
        await _cacheService.RemoveByPrefixAsync("user:");

        // Assert
        var user1 = await _cacheService.GetAsync<string>("user:1");
        var user2 = await _cacheService.GetAsync<string>("user:2");
        var product1 = await _cacheService.GetAsync<string>("product:1");

        user1.Should().BeNull();
        user2.Should().BeNull();
        product1.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WithNullPrefix_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _cacheService.RemoveByPrefixAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        await _cacheService.SetAsync(key, value);

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        var key = "non-existing-key";

        // Act
        var result = await _cacheService.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _cacheService.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldStoreAndRetrieve()
    {
        // Arrange
        var key = "complex-key";
        var value = new TestObject { Id = 1, Name = "Test" };

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<TestObject>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
