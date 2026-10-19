using FluentValidation;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Services.Validators
{
    public class ReviewValidator:AbstractValidator<CreateReviewRequest>
    {
        public ReviewValidator()
        {
            RuleFor(x=>x.Rating).InclusiveBetween(1,5).WithMessage("Rating must be between 1 an 5");
            RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");
        }
    }
}