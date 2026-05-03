using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.UnitTests.Domain
{
    public class RefreshTokenTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var token = new RefreshToken();

            // Assert
            token.Id.Should().Be(0);
            token.Token.Should().Be(string.Empty);
            token.UserId.Should().Be(string.Empty);
            token.IsRevoked.Should().BeFalse();
            token.RevokedAt.Should().BeNull();
            token.ReplacedByToken.Should().BeNull();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var testDate = DateTime.UtcNow;

            // Act
            var token = new RefreshToken
            {
                Id = 1,
                Token = "abc123def456",
                UserId = "admin",
                CreatedAt = testDate,
                ExpiresAt = testDate.AddDays(7),
                IsRevoked = false,
                ReplacedByToken = null
            };

            // Assert
            token.Id.Should().Be(1);
            token.Token.Should().Be("abc123def456");
            token.UserId.Should().Be("admin");
            token.CreatedAt.Should().Be(testDate);
            token.ExpiresAt.Should().Be(testDate.AddDays(7));
            token.IsRevoked.Should().BeFalse();
            token.ReplacedByToken.Should().BeNull();
        }

        #endregion

        #region IsActive Tests

        [Fact]
        public void IsActive_WhenNotRevokedAndNotExpired_ShouldReturnTrue()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "valid-token",
                UserId = "admin",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            // Act & Assert
            token.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_WhenRevoked_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "revoked-token",
                UserId = "admin",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow
            };

            // Act & Assert
            token.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenExpired_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "expired-token",
                UserId = "admin",
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                ExpiresAt = DateTime.UtcNow.AddDays(-7),
                IsRevoked = false
            };

            // Act & Assert
            token.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_WhenRevokedAndExpired_ShouldReturnFalse()
        {
            // Arrange
            var token = new RefreshToken
            {
                Token = "revoked-expired",
                UserId = "admin",
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                ExpiresAt = DateTime.UtcNow.AddDays(-7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow.AddDays(-10)
            };

            // Act & Assert
            token.IsActive.Should().BeFalse();
        }

        #endregion

        #region Token Rotation Tests

        [Fact]
        public void ReplacedByToken_TracksTokenRotation()
        {
            // Arrange
            var oldToken = new RefreshToken
            {
                Id = 1,
                Token = "old-token",
                UserId = "admin",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(6),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow,
                ReplacedByToken = "new-token"
            };

            // Assert
            oldToken.IsRevoked.Should().BeTrue();
            oldToken.ReplacedByToken.Should().Be("new-token");
            oldToken.IsActive.Should().BeFalse();
        }

        #endregion
    }
}
