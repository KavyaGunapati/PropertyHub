namespace PropertyHub.DataAccess.Entities;

using PropertyHub.DataAccess.Enums;

public class Inquiry
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public Property Property { get; set; }

    public int BuyerId { get; set; }
    public User Buyer { get; set; }

    public string Message { get; set; }
    public string Response { get; set; }

    public InquiryStatus Status { get; set; } = InquiryStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}