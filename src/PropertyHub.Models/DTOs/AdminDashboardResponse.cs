namespace PropertyHub.Models.DTOs;

public class AdminDashboardResponse
{
    public int TotalUsers { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; }
    public int TotalProperties { get; set; }
    public Dictionary<string, int> PropertiesByStatus { get; set; }
    public int InquiriesAwaitingResponse { get; set; }
    public int TotalReviews { get; set; }
}