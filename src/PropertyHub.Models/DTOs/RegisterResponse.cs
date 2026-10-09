namespace PropertyHub.Models.DTOs;

public class RegisterResponse
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Message { get; set; } = "Registration successful. Please log in.";
}