using FluentValidation;
using PropertyHub.Models.DTOs;
namespace PropertyHub.Services.Validators
{
    public class PropertyValidator : AbstractValidator<CreatePropertyRequest>
    {
        public PropertyValidator()
        {
            RuleFor(x=> x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x=> x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x=> x.Address).NotEmpty().WithMessage("Address is required.");
            RuleFor(x=> x.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x=> x.State).NotEmpty().WithMessage("State is required.");
            RuleFor(x=> x.ZipCode).NotEmpty().WithMessage("ZipCode is required.");
            RuleFor(x=> x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
            RuleFor(x=> x.Bedrooms).GreaterThanOrEqualTo(0).WithMessage("Bedrooms must be greater than or equal to 0.");
            RuleFor(x=> x.Bathrooms).GreaterThanOrEqualTo(0).WithMessage("Bathrooms must be greater than or equal to 0.");
            RuleFor(x=> x.SquareFeet).GreaterThan(0).WithMessage("SquareFeet must be greater than 0.");
            RuleFor(x=> x.YearBuilt).GreaterThan(0).WithMessage("YearBuilt must be greater than 0.");
        }
    }
}