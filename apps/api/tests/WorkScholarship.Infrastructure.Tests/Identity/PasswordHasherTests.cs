using FluentAssertions;
using WorkScholarship.Infrastructure.Identity;

namespace WorkScholarship.Infrastructure.Tests.Identity;

[Trait("Category", "Infrastructure")]
[Trait("Component", "PasswordHasher")]
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    // =====================================================================
    // Hash()
    // =====================================================================

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        // Act
        var hash = _hasher.Hash("my_password");

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Hash_ReturnsDifferentHashEachTime()
    {
        // Act - same password should produce different hashes (due to salt)
        var hash1 = _hasher.Hash("same_password");
        var hash2 = _hasher.Hash("same_password");

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Hash_ProducesHashDifferentFromOriginalPassword()
    {
        // Arrange
        var password = "my_secret_password";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBe(password);
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("Complex_P@ssw0rd!")]
    [InlineData("password with spaces")]
    [InlineData("123456789")]
    public void Hash_DifferentPasswords_ProduceDifferentHashes(string password)
    {
        // Act
        var hash = _hasher.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
    }

    // =====================================================================
    // Verify()
    // =====================================================================

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "correct_password";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "correct_password";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify("wrong_password", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.Hash("original_password");

        // Act
        var result = _hasher.Verify("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_HashAndVerifyMultiplePasswords_AllVerifyCorrectly()
    {
        // Arrange
        var passwords = new[] { "password1", "password2", "password3" };
        var hashes = passwords.Select(p => _hasher.Hash(p)).ToArray();

        // Act & Assert
        for (int i = 0; i < passwords.Length; i++)
        {
            _hasher.Verify(passwords[i], hashes[i]).Should().BeTrue($"Password '{passwords[i]}' should verify against its hash");
            for (int j = 0; j < passwords.Length; j++)
            {
                if (i != j)
                {
                    _hasher.Verify(passwords[i], hashes[j]).Should().BeFalse($"Password '{passwords[i]}' should NOT verify against hash of '{passwords[j]}'");
                }
            }
        }
    }

    [Fact]
    public void Verify_CaseSensitivePasswordCheck()
    {
        // Arrange
        var hash = _hasher.Hash("CaseSensitive");

        // Act & Assert
        _hasher.Verify("CaseSensitive", hash).Should().BeTrue();
        _hasher.Verify("casesensitive", hash).Should().BeFalse();
        _hasher.Verify("CASESENSITIVE", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithComplexPassword_WorksCorrectly()
    {
        // Arrange
        var complexPassword = "P@ssw0rd!#$%^&*()_+-=[]{}|;':\",./<>?";
        var hash = _hasher.Hash(complexPassword);

        // Act
        var result = _hasher.Verify(complexPassword, hash);

        // Assert
        result.Should().BeTrue();
    }
}
