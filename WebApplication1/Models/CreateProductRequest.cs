using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class CreateProductRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, StringLength(20)]
    public string Sku { get; set; } = "";

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Range(0.01, 1000000)]
    public decimal? SalePrice { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (SalePrice.HasValue && SalePrice.Value >= Price)
        {
            yield return new ValidationResult(
                "SalePrice must be less than Price.",
                new[] { nameof(SalePrice) });
        }
    }
}