using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;
using System.IdentityModel.Tokens.Jwt;

namespace MyMediaVerse.UnitTests.Application
{
    public class AuthServiceTests : InMemoryDbTestBase
    {
        private readonly IConfiguration _mockConfiguration;
        private readonly IConfigurationSection _mockJwtSection;
        private readonly AuthService _authService;
        private const string TestJwtSecret = "ThisIsAVerySecureTestSecretKey12345678901234567890";

        public AuthServiceTests()
        {
            _mockConfiguration = Substitute.For<IConfiguration>();
            _mockJwtSection = Substitute.For<IConfigurationSection>();

            // Setup JWT configuration
            _mockJwtSection["Secret"].Returns(TestJwtSecret);
            _mockJwtSection["Issuer"].Returns("TestIssuer");
            _mockJwtSection["Audience"].Returns("TestAudience");
            _mockConfiguration.GetSection("JwtSettings").Returns(_mockJwtSection);

            _authService = new AuthService(Context, _mockConfiguration);
        }

        #region GenerateAccessToken Tests

        [Fact]
        public void GenerateAccessToken_ShouldReturnValidJwtToken()
        {
            // Arrange
            var username = "testuser";
            var userId = "user123";
            var expirationMinutes = 15;

            // Act
            var token = _authService.GenerateAccessToken(username, userId, expirationMinutes);

            // Assert
            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == username);
            jwtToken.Claims.Should().Contain(c => c.Type == "userId" && c.Value == userId);
        }

        [Fact]
        public void GenerateAccessToken_ShouldIncludeCorrectExpiration()
        {
            // Arrange
            var username = "testuser";
            var userId = "user123";
            var expirationMinutes = 30;

            // Act
            var token = _authService.GenerateAccessToken(username, userId, expirationMinutes);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var expectedExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
            jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateAccessToken_ShouldIncludeUniqueJti()
        {
            // Arrange
            var username = "testuser";
            var userId = "user123";

            // Act
            var token1 = _authService.GenerateAccessToken(username, userId, 15);
            var token2 = _authService.GenerateAccessToken(username, userId, 15);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == "jti").Value;
            var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == "jti").Value;

            jti1.Should().NotBe(jti2);
        }

        #endregion

        #region GenerateRefreshToken Tests

        [Fact]
        public void GenerateRefreshToken_ShouldReturnNonEmptyToken()
        {
            // Act
            var token = _authService.GenerateRefreshToken();

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Length.Should().BeGreaterThan(20);
        }

        [Fact]
        public void GenerateRefreshToken_ShouldReturnUniqueTokens()
        {
            // Act
            var tokens = Enumerable.Range(0, 10)
                .Select(_ => _authService.GenerateRefreshToken())
                .ToList();

            // Assert
            tokens.Distinct().Should().HaveCount(10);
        }

        #endregion

        #region SaveRefreshTokenAsync Tests

        [Fact]
        public async Task SaveRefreshTokenAsync_ShouldSaveTokenToDatabase()
        {
            // Arrange
            var userId = "user123";
            var token = "test-refresh-token";
            var expirationDays = 7;

            // Act
            await _authService.SaveRefreshTokenAsync(userId, token, expirationDays);

            // Assert
            var savedToken = Context.RefreshTokens.FirstOrDefault(rt => rt.Token == token);
            savedToken.Should().NotBeNull();
            savedToken!.UserId.Should().Be(userId);
            savedToken.IsRevoked.Should().BeFalse();
            savedToken.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(expirationDays), TimeSpan.FromMinutes(1));
        }

        #endregion

        #region ValidateRefreshTokenAsync Tests

        [Fact]
        public async Task ValidateRefreshTokenAsync_ShouldReturnUserId_WhenTokenIsValid()
        {
            // Arrange
            var userId = "user123";
            var token = "valid-refresh-token";
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            Context.RefreshTokens.Add(refreshToken);
            await Context.SaveChangesAsync();

            // Act
            var result = await _authService.ValidateRefreshTokenAsync(token);

            // Assert
            result.Should().Be(userId);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist()
        {
            // Arrange
            var nonExistentToken = "non-existent-token";

            // Act
            var result = await _authService.ValidateRefreshTokenAsync(nonExistentToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ShouldReturnNull_WhenTokenIsExpired()
        {
            // Arrange
            var token = "expired-token";
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = "user123",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(-3), // Expired 3 days ago
                IsRevoked = false
            };
            Context.RefreshTokens.Add(refreshToken);
            await Context.SaveChangesAsync();

            // Act
            var result = await _authService.ValidateRefreshTokenAsync(token);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ShouldReturnNull_WhenTokenIsRevoked()
        {
            // Arrange
            var token = "revoked-token";
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = "user123",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow.AddHours(-1)
            };
            Context.RefreshTokens.Add(refreshToken);
            await Context.SaveChangesAsync();

            // Act
            var result = await _authService.ValidateRefreshTokenAsync(token);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region RevokeRefreshTokenAsync Tests

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldRevokeToken_WhenTokenExists()
        {
            // Arrange
            var token = "token-to-revoke";
            var refreshToken = new RefreshToken
            {
                Token = token,
                UserId = "user123",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            Context.RefreshTokens.Add(refreshToken);
            await Context.SaveChangesAsync();

            // Act
            await _authService.RevokeRefreshTokenAsync(token, "replacement-token");

            // Assert
            Context.ChangeTracker.Clear();
            var revokedToken = Context.RefreshTokens.First(rt => rt.Token == token);
            revokedToken.IsRevoked.Should().BeTrue();
            revokedToken.RevokedAt.Should().NotBeNull();
            revokedToken.ReplacedByToken.Should().Be("replacement-token");
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldDoNothing_WhenTokenDoesNotExist()
        {
            // Arrange
            var nonExistentToken = "non-existent-token";

            // Act - should not throw
            await _authService.RevokeRefreshTokenAsync(nonExistentToken);

            // Assert - no exception means success
        }

        #endregion

        #region RevokeAllUserTokensAsync Tests

        [Fact]
        public async Task RevokeAllUserTokensAsync_ShouldRevokeAllUserTokens()
        {
            // Arrange
            var userId = "user123";
            var tokens = new[]
            {
                new RefreshToken { Token = "token1", UserId = userId, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false },
                new RefreshToken { Token = "token2", UserId = userId, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false },
                new RefreshToken { Token = "token3", UserId = "other-user", CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false }
            };
            Context.RefreshTokens.AddRange(tokens);
            await Context.SaveChangesAsync();

            // Act
            await _authService.RevokeAllUserTokensAsync(userId);

            // Assert
            Context.ChangeTracker.Clear();
            var userTokens = Context.RefreshTokens.Where(rt => rt.UserId == userId).ToList();
            userTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());

            var otherUserToken = Context.RefreshTokens.First(rt => rt.UserId == "other-user");
            otherUserToken.IsRevoked.Should().BeFalse();
        }

        #endregion

        #region CleanupExpiredTokensAsync Tests

        [Fact]
        public async Task CleanupExpiredTokensAsync_ShouldRemoveExpiredTokens()
        {
            // Arrange
            var tokens = new[]
            {
                new RefreshToken { Token = "expired1", UserId = "user1", CreatedAt = DateTime.UtcNow.AddDays(-10), ExpiresAt = DateTime.UtcNow.AddDays(-3), IsRevoked = false },
                new RefreshToken { Token = "expired2", UserId = "user2", CreatedAt = DateTime.UtcNow.AddDays(-10), ExpiresAt = DateTime.UtcNow.AddDays(-1), IsRevoked = false },
                new RefreshToken { Token = "valid", UserId = "user3", CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false }
            };
            Context.RefreshTokens.AddRange(tokens);
            await Context.SaveChangesAsync();

            // Act
            await _authService.CleanupExpiredTokensAsync();

            // Assert
            Context.ChangeTracker.Clear();
            var remainingTokens = Context.RefreshTokens.ToList();
            remainingTokens.Should().HaveCount(1);
            remainingTokens.First().Token.Should().Be("valid");
        }

        [Fact]
        public async Task CleanupExpiredTokensAsync_ShouldDoNothing_WhenNoExpiredTokensExist()
        {
            // Arrange
            var validToken = new RefreshToken
            {
                Token = "valid-token",
                UserId = "user123",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            Context.RefreshTokens.Add(validToken);
            await Context.SaveChangesAsync();

            // Act
            await _authService.CleanupExpiredTokensAsync();

            // Assert
            Context.ChangeTracker.Clear();
            var remainingTokens = Context.RefreshTokens.ToList();
            remainingTokens.Should().HaveCount(1);
        }

        #endregion
    }
}
