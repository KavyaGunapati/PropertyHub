using PropertyHub.Models.DTOs;

namespace PropertyHub.Interfaces.IService
{
    public interface IReviewService
    {
        Task<Result<ReviewResponse>> SubmitReviewAsync(int propertyId, int userId, CreateReviewRequest request, CancellationToken cancellationToken = default);
        Task<Result<List<ReviewResponse>>> GetReviewsAsync(int propertyId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}