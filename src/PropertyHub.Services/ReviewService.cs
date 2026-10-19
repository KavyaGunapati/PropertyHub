using PropertyHub.Interfaces.IManager;
using PropertyHub.Interfaces.IService;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewManager _manager;
        public ReviewService(IReviewManager manager)
        {
            _manager=manager;
        }
        public Task<Result<ReviewResponse>> SubmitReviewAsync(int propertyId, int userId, CreateReviewRequest request, CancellationToken cancellationToken)
            =>_manager.SubmitReviewAsync(propertyId,userId,request,cancellationToken);
        public Task<Result<List<ReviewResponse>>> GetReviewsAsync(int propertyId, int pageNumber, int pageSize, CancellationToken cancellationToken)
            =>_manager.GetReviewsAsync(propertyId,pageNumber,pageSize,cancellationToken);
    }
}