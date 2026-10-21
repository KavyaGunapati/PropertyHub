using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

namespace  PropertyHub.Services
{
    public class InquiryService : IInquiryService
    {
        private readonly IInquiryManager _manager;
        public InquiryService(IInquiryManager manager)
        {
            _manager=manager;
        }
        public Task<Result<InquiryResponse>> SubmitAsync(int propertyId, int buyerId, CreateInquiryRequest request, CancellationToken cancellationToken)
            =>_manager.SubmitAsync(propertyId,buyerId,request,cancellationToken);
        public Task<Result<List<InquiryResponse>>> GetForPropertyAsync(int propertyId, int agentId, int pageNumber, int pageSize, CancellationToken cancellationToken)
            =>_manager.GetForPropertyAsync(propertyId,agentId,pageNumber,pageSize,cancellationToken);
        public Task<Result<List<InquiryResponse>>> GetMineAsync(int buyerId, int pageNumber, int pageSize, CancellationToken cancellationToken)
            =>_manager.GetMineAsync(buyerId,pageNumber,pageSize,cancellationToken);
        public Task<Result<InquiryResponse>> RespondAsync(int inquiryId, int agentId, RespondInquiryRequest request, CancellationToken cancellationToken)
            =>_manager.RespondAsync(inquiryId,agentId,request,cancellationToken);
    }
}