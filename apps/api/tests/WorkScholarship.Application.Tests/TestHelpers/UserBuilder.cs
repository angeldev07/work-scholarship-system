using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Application.Tests.TestHelpers;

/// <summary>
/// Test Data Builder for User entity. Provides a fluent API to create test users
/// with sensible defaults that can be overridden as needed.
/// </summary>
public class UserBuilder
{
    private string _email = "test.user@univ.edu";
    private string _firstName = "Test";
    private string _lastName = "User";
    private string _passwordHash = "AQAAAAIAAYagAAAAEHashed_Password_For_Tests==";
    private UserRole _role = UserRole.Beca;
    private string _createdBy = "system";
    private bool _isGoogle = false;
    private string? _googleId = null;
    private string? _photoUrl = null;
    private bool _deactivate = false;
    private int _failedLoginAttempts = 0;
    private bool _lockout = false;

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UserBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UserBuilder WithPasswordHash(string passwordHash)
    {
        _passwordHash = passwordHash;
        return this;
    }

    public UserBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }

    public UserBuilder AsGoogle(string googleId = "google-123", string? photoUrl = null)
    {
        _isGoogle = true;
        _googleId = googleId;
        _photoUrl = photoUrl;
        return this;
    }

    public UserBuilder AsInactive()
    {
        _deactivate = true;
        return this;
    }

    public UserBuilder WithFailedLoginAttempts(int attempts)
    {
        _failedLoginAttempts = attempts;
        return this;
    }

    public UserBuilder AsLockedOut()
    {
        _lockout = true;
        return this;
    }

    public User Build()
    {
        User user;

        if (_isGoogle)
        {
            user = User.CreateFromGoogle(_email, _firstName, _lastName, _googleId, _photoUrl, _createdBy);
        }
        else
        {
            user = User.Create(_email, _firstName, _lastName, _passwordHash, _role, _createdBy);
        }

        if (_deactivate)
        {
            user.Deactivate("system");
        }

        for (int i = 0; i < _failedLoginAttempts; i++)
        {
            user.RecordFailedLogin();
        }

        if (_lockout)
        {
            // Record 5 failed logins to trigger lockout
            for (int i = 0; i < User.MAX_FAILED_LOGIN_ATTEMPTS; i++)
            {
                user.RecordFailedLogin();
            }
        }

        return user;
    }
}
