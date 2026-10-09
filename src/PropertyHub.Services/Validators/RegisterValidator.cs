namespace PropertyHub.Services.Validators;
using FluentValidation;
using PropertyHub.Models.DTOs;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress().WithMessage("Invalid email format.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("Confirm Password is required.").Equal(x => x.Password).WithMessage("Passwords do not match.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.").Matches(@"^\d{10}$").WithMessage("Invalid phone number format.");
    }
}
