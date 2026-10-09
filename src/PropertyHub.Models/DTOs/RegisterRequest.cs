namespace PropertyHub.Models.DTOs;

using PropertyHub.DataAccess.Enums;

public class RegisterRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
}