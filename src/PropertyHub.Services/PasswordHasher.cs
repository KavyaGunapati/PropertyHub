namespace PropertyHub.Services;
using PropertyHub.Interfaces.IService;
using BCrypt.Net;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hashsedPassword)
    {
        return BCrypt.Verify(password, hashsedPassword);
    }
}