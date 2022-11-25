using System.ComponentModel.DataAnnotations;

namespace LogOtter.HttpPatch;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
public abstract class OptionallyPatchedValidationAttribute : Attribute
{
    protected OptionallyPatchedValidationAttribute(ValidationAttribute validationAttribute)
    {
        ValidationAttribute = validationAttribute;
    }
    
    public ValidationAttribute ValidationAttribute { get; }
}

public sealed class MinLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public MinLengthIfPatchedAttribute(int minLength)
        : base(new MinLengthAttribute(minLength))
    {
        MinLength = minLength;
    }

    public int MinLength { get; }
}

public sealed class MaxLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public MaxLengthIfPatchedAttribute(int maxLength)
        : base(new MaxLengthAttribute(maxLength))
    {
        MaxLength = maxLength;
    }

    public int MaxLength { get; }
}


public sealed class RequiredIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public RequiredIfPatchedAttribute()
        : base(new RequiredAttribute())
    {
    }
}

public sealed class RangeIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public RangeIfPatchedAttribute(double minimum, double maximum)
        : base(new RangeAttribute(minimum, maximum))
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public double Minimum { get; }

    public double Maximum { get; }
}

public sealed class EmailAddressIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public EmailAddressIfPatchedAttribute()
        : base(new EmailAddressAttribute())
    {
    }
}

public sealed class StringLengthIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public StringLengthIfPatchedAttribute(int maximumLength) 
        : base(new StringLengthAttribute(maximumLength))
    {
        MaximumLength = maximumLength;
    }

    public int MaximumLength { get; }
}

public sealed class RegularExpressionIfPatchedAttribute : OptionallyPatchedValidationAttribute
{
    public RegularExpressionIfPatchedAttribute(string pattern) 
        : base(new RegularExpressionAttribute(pattern))
    {
        Pattern = pattern;
    }

    public string Pattern { get; }
}