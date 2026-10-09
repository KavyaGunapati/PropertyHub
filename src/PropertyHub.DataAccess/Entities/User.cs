namespace PropertyHub.DataAccess.Entities;

using Microsoft.AspNetCore.Identity;
using PropertyHub.DataAccess.Enums;

public class User : IdentityUser<int>
{
    public string PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public ICollection<Property> Properties { get; set; } = new List<Property>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}