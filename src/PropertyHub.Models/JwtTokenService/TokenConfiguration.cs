namespace PropertyHub.Models.JwtTokenService
{
    public class TokenConfiguration
    {
        public string AccessTokenSecret { get; set; }
        public string RefreshTokenSecret { get; set; }
        public int AccessTokenExpirationInMinutes { get; set; }= 900;
        public int RefreshTokenExpirationInMinutes { get; set; }= 1440;
        public string Issuer { get; set; }="PropertyHub";
        public string Audience { get; set; }="PropertyHubUsers";
    }
}