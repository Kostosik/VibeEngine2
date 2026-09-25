using System.ComponentModel.DataAnnotations;

namespace Engine.Tooling.Validation;

public interface IValidator
{
    ValidationResult Validate(
        ValidationContext context);
}