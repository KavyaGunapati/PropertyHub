namespace PropertyHub.Models.DTOs;

public class FavoriteResponse
{
    public int PropertyId { get; set; }
    public string Title { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public decimal Price { get; set; }
    public DateTime AddedAt { get; set; }
}