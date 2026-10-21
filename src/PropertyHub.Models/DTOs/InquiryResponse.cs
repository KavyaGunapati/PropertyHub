namespace PropertyHub.Models.DTOs;

using PropertyHub.DataAccess.Enums;

public class InquiryResponse
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; }
    public string BuyerName { get; set; }
    public string Message { get; set; }
    public string Response { get; set; }
    public InquiryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}