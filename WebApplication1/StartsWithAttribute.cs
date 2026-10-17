using System.ComponentModel.DataAnnotations;
namespace WebApplication1
{
    public class StartsWithAttribute : ValidationAttribute
    {
        public readonly string _requiredPrefix;
        public StartsWithAttribute(string requiredPrefix)
        {
            _requiredPrefix = requiredPrefix;
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string stringValue)
            {
                if (stringValue.StartsWith(_requiredPrefix))
                {
                    return ValidationResult.Success;
                }
            }
            return new ValidationResult(ErrorMessage ?? $"The field {validationContext.MemberName} must start with '{_requiredPrefix}'.");
        }
    }
}
