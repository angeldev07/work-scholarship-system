using FluentAssertions;
using NSubstitute;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Application.Features.Auth.Queries.GetCurrentUser;
using WorkScholarship.Application.Tests.TestHelpers;
using WorkScholarship.Domain.Enums;
using WorkScholarship.Infrastructure.Data;

namespace WorkScholarship.Application.Tests.Features.Auth.Queries;

[Trait("Category", "Application")]
[Trait("Feature", "Auth")]
[Trait("Component", "GetCurrentUserQueryHandler")]
public class GetCurrentUserQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _dbContext = DbContextFactory.CreateInMemoryContext();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _handler = new GetCurrentUserQueryHandler(_dbContext, _currentUserService);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // =====================================================================
    // Happy path
    // =====================================================================

    [Fact]
    public async Task Handle_WithAuthenticatedActiveUser_ReturnsUserDto()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .WithFirstName("Juan")
            .WithLastName("Perez")
            .WithRole(UserRole.Admin)
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("test@univ.edu");
        result.Value.FirstName.Should().Be("Juan");
        result.Value.LastName.Should().Be("Perez");
        result.Value.Role.Should().Be("ADMIN");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithBecaUser_ReturnsCorrectRole()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("beca@univ.edu")
            .WithRole(UserRole.Beca)
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("BECA");
    }

    [Fact]
    public async Task Handle_WithSupervisorUser_ReturnsCorrectRole()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("super@univ.edu")
            .WithRole(UserRole.Supervisor)
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("SUPERVISOR");
    }

    [Fact]
    public async Task Handle_WithGoogleUser_ReturnsCorrectAuthProvider()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("google@univ.edu")
            .AsGoogle("google-id-123", "https://photo.url")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AuthProvider.Should().Be("Google");
    }

    // =====================================================================
    // Error paths
    // =====================================================================

    [Fact]
    public async Task Handle_WithNoAuthenticatedUser_ReturnsUnauthorizedFailure()
    {
        // Arrange
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.UNAUTHORIZED);
    }

    [Fact]
    public async Task Handle_WithUserNotFoundInDatabase_ReturnsUserNotFoundFailure()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns(nonExistentUserId);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.USER_NOT_FOUND);
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsInactiveAccountFailure()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsInactive()
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(AuthErrorCodes.INACTIVE_ACCOUNT);
    }

    // =====================================================================
    // UserDto mapping
    // =====================================================================

    [Fact]
    public async Task Handle_UserDtoHasCorrectFullName()
    {
        // Arrange
        var user = new UserBuilder()
            .WithFirstName("Maria")
            .WithLastName("Garcia")
            .WithEmail("maria@univ.edu")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.FullName.Should().Be("Maria Garcia");
    }

    [Fact]
    public async Task Handle_UserDtoHasPhotoUrl()
    {
        // Arrange
        var user = new UserBuilder()
            .WithEmail("test@univ.edu")
            .AsGoogle("google-id", "https://photo.url/pic.jpg")
            .Build();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _currentUserService.UserId.Returns(user.Id);

        var query = new GetCurrentUserQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value.PhotoUrl.Should().Be("https://photo.url/pic.jpg");
    }
}
