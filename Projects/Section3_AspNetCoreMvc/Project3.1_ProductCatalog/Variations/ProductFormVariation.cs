using System.ComponentModel.DataAnnotations;

namespace ProductCatalog.Variations;

// A form class with a CROSS-FIELD rule. Attributes like [Range] check ONE property; IValidatableObject.Validate
// runs after them and can compare properties ("expensive products need a description").
public class ProductFormVariation : IValidatableObject
{
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Price > 1000 && string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("Products over 1,000 need a description.", [nameof(Description)]);
        }
    }
}
