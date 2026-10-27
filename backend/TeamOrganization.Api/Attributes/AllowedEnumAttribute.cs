using System.ComponentModel.DataAnnotations;
using TeamOrganization.Domain.Utils;

namespace TeamOrganization.Api.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AllowedEnumValuesAttribute : ValidationAttribute
{
    private readonly Type _enumType;

    public AllowedEnumValuesAttribute(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException(
                "Type must be an enum.",
                nameof(enumType));
        }

        _enumType = enumType;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null || value is string { Length: 0 })
        {
            return ValidationResult.Success;
        }

        if (value is not string input)
        {
            return new ValidationResult(
                FormatErrorMessage(validationContext.DisplayName));
        }

        var allowedValues = Enum.GetValues(_enumType)
            .Cast<Enum>()
            .Select(EnumUtils.ToString);

        if (allowedValues.Contains(input, StringComparer.Ordinal))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            FormatErrorMessage(validationContext.DisplayName));
    }

    public override string FormatErrorMessage(string name)
    {
        return string.IsNullOrWhiteSpace(ErrorMessage)
            ? $"{name} contains an invalid enum value."
            : base.FormatErrorMessage(name);
    }
}