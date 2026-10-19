namespace PropertyHub.Models.DTOs
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int Expiration { get; set; }=900;
    }
}