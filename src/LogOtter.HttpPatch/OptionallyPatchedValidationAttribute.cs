using System.ComponentModel.DataAnnotations;

namespace LogOtter.HttpPatch;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public abstract class OptionallyPatchedValidationAttribute : Attribute
{
    public ValidationAttribute ValidationAttribute { get; }

    protected OptionallyPatchedValidationAttribute(ValidationAttribute validationAttribute)
    {
        ValidationAttribute = validationAttribute;
    }
}

public sealed class MinLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public int MinLength { get; }

    public MinLengthIfPatchedAttribute(int minLength) : base(new MinLengthAttribute(minLength))
    {
        MinLength = minLength;
    }
}

public sealed class MaxLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public int MaxLength { get; }

    public MaxLengthIfPatchedAttribute(int maxLength) : base(new MaxLengthAttribute(maxLength))
    {
        MaxLength = maxLength;
    }
}

public sealed class RequiredIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public RequiredIfPatchedAttribute() : base(new RequiredAttribute())
    {
    }
}

public sealed class RangeIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public double Minimum { get; }

    public double Maximum { get; }

    public RangeIfPatchedAttribute(double minimum, double maximum) : base(new RangeAttribute(minimum, maximum))
    {
        Minimum = minimum;
        Maximum = maximum;
    }
}

public sealed class EmailAddressIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public EmailAddressIfPatchedAttribute() : base(new EmailAddressAttribute())
    {
    }
}

public sealed class StringLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public int MaximumLength { get; }

    public StringLengthIfPatchedAttribute(int maximumLength) : base(new StringLengthAttribute(maximumLength))
    {
        MaximumLength = maximumLength;
    }
}

public sealed class RegularExpressionIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public string Pattern { get; }

    public RegularExpressionIfPatchedAttribute(string pattern) : base(new RegularExpressionAttribute(pattern))
    {
        Pattern = pattern;
    }
}
