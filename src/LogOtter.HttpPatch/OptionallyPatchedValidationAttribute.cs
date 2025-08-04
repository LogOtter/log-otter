using System.ComponentModel.DataAnnotations;

namespace LogOtter.HttpPatch;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = true)]
public abstract class OptionallyPatchedValidationAttribute(ValidationAttribute validationAttribute) : Attribute
{
    public ValidationAttribute ValidationAttribute { get; } = validationAttribute;
}

public sealed class MinLengthIfPatchedAttribute(int minLength) : OptionallyPatchedValidationAttribute(new MinLengthAttribute(minLength))
{
    public int MinLength { get; } = minLength;
}

public sealed class MaxLengthIfPatchedAttribute(int maxLength) : OptionallyPatchedValidationAttribute(new MaxLengthAttribute(maxLength))
{
    public int MaxLength { get; } = maxLength;
}

public sealed class RequiredIfPatchedAttribute() : OptionallyPatchedValidationAttribute(new RequiredAttribute());

public sealed class RangeIfPatchedAttribute(double minimum, double maximum)
    : OptionallyPatchedValidationAttribute(new RangeAttribute(minimum, maximum))
{
    public double Minimum { get; } = minimum;

    public double Maximum { get; } = maximum;
}

public sealed class EmailAddressIfPatchedAttribute() : OptionallyPatchedValidationAttribute(new EmailAddressAttribute());

public sealed class StringLengthIfPatchedAttribute(int maximumLength) : OptionallyPatchedValidationAttribute(new StringLengthAttribute(maximumLength))
{
    public int MaximumLength { get; } = maximumLength;
}

public sealed class RegularExpressionIfPatchedAttribute(string pattern)
    : OptionallyPatchedValidationAttribute(new RegularExpressionAttribute(pattern))
{
    public string Pattern { get; } = pattern;
}
