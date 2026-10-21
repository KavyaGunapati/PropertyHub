namespace PropertyHub.Models.DTOs;

using PropertyHub.DataAccess.Enums;

public class AdminPropertyResponse
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string City { get; set; }
    public decimal Price { get; set; }
    public PropertyStatus Status { get; set; }
    public int AgentId { get; set; }
    public string AgentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}