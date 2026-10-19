namespace PropertyHub.Models.DTOs
{
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string RefreshToken { get; set; }
        public int RefreshTokenExpiration { get; set; }
    }
}