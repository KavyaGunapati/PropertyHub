using PropertyHub.Models.DTOs;

namespace PropertyHub.Interfaces.IManager
{
    public interface IInquiryManager
    {
        Task<Result<InquiryResponse>> SubmitAsync(int propertyId, int buyerId, CreateInquiryRequest request,CancellationToken cancellationToken=default);
        Task<Result<List<InquiryResponse>>> GetForPropertyAsync(int propertyId,int agentId,int pageNumber,int pageSize,CancellationToken cancellationToken=default);
        Task<Result<List<InquiryResponse>>> GetMineAsync(int agentId,int pageNumber, int pageSize, CancellationToken cancellationToken=default);
        Task<Result<InquiryResponse>> RespondAsync(int inquiryId,int agentId, RespondInquiryRequest request,CancellationToken cancellationToken=default);
    }
}