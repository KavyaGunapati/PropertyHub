using FluentValidation;
using PropertyHub.Models.DTOs;

namespace PropertyHub.Services.Validators
{
    public class CreateInquiryValidator : AbstractValidator<CreateInquiryRequest>
    {
        public CreateInquiryValidator()
        {
            RuleFor(x=>x.Message).NotEmpty().WithMessage("Message is required").MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters");
        }
    }
    public class ResponseInquiryValidator : AbstractValidator<RespondInquiryRequest>
    {
        public ResponseInquiryValidator()
        {
            RuleFor(x=>x.Response).NotEmpty().WithMessage("Response is required").MaximumLength(2000).WithMessage("Response cannot exceed 2000 characters");
        }
    }
}