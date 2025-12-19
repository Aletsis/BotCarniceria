using BotCarniceria.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2");
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword("", hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("VeryLongPasswordWithManyCharacters123456789!@#$%")]
    [InlineData("Pass123!")]
    [InlineData("Contraseña123!")]
    public void HashPassword_WithVariousPasswords_ShouldHashCorrectly(string password)
    {
        // Act
        var hashedPassword = _passwordHasher.HashPassword(password);
        var isValid = _passwordHasher.VerifyPassword(password, hashedPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        isValid.Should().BeTrue();
    }
}
