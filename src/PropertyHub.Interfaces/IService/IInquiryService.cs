using PropertyHub.Models.DTOs;

namespace PropertyHub.Interfaces.IService
{
    public interface IInquiryService
    {
        Task<Result<InquiryResponse>> SubmitAsync(int propertyId, int buyerId, CreateInquiryRequest request, CancellationToken cancellationToken);
        Task<Result<List<InquiryResponse>>> GetForPropertyAsync(int propertyId, int agentId, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<Result<List<InquiryResponse>>> GetMineAsync(int buyerId, int pageNumber, int pageSize, CancellationToken cancellationToken);
        Task<Result<InquiryResponse>> RespondAsync(int inquiryId, int agentId, RespondInquiryRequest request, CancellationToken cancellationToken);
    }
}