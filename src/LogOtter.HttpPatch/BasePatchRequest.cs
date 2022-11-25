using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LogOtter.HttpPatch;

/// <summary>
/// Helper to enable the use of validation attributes on the thing that is optionally being patched
/// You will need to use (or create if it doesn't exist) a wrapper that inherits from <see cref="OptionallyPatchedValidationAttribute"/>
/// Such as: <see cref="MinLengthIfPatchedAttribute"/> or <see cref="RequiredIfPatchedAttribute"/>
/// Note: This is only required on the immediate child of the <see cref="OptionallyPatched{T}"/> parameter, nested children can use attributes as usual 
/// </summary>
public record BasePatchRequest : IValidatableObject
{
    private static readonly ConcurrentDictionary<Type, ImmutableList<(PropertyInfo PropertyInfo, ImmutableList<ValidationAttribute> ValidationAttributes)>> PropertiesWithValidationAttributesCache = new();

    private static readonly ConcurrentDictionary<Type, ImmutableList<ParameterInfo>> ConstructorParamsCache = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        var properties = GetPropertiesWithValidationAttributes(GetType());
        foreach (var property in properties)
        {
            var value = (IOptionallyPatched)property.PropertyInfo.GetValue(this)!;

            if (!value.IsIncludedInPatch)
            {
                continue;
            }

            var customValidationContext = new ValidationContext(value, validationContext.Items)
            {
                DisplayName = property.PropertyInfo.Name
            };

            Validator.TryValidateValue(value.Value!, customValidationContext, validationResults, property.ValidationAttributes);
        }

        return validationResults;
    }

    /// <summary>
    /// Gets all the properties on a type with any validation attributes either directly attached to the property or on the constructor with a parameter of the same name
    /// </summary>
    private static ImmutableList<(PropertyInfo PropertyInfo, ImmutableList<ValidationAttribute> ValidationAttributes)> GetPropertiesWithValidationAttributes(Type type)
    {
        ImmutableList<ParameterInfo> GetConstructorParams(Type t)
        {
            var constructors = t.GetConstructors();

            if (constructors.Length != 1)
            {
                throw new InvalidOperationException("BasePatchRequest can only be used on records with a single constructor");
            }

            return constructors.Single().GetParameters().ToImmutableList();
        }

        ImmutableList<(PropertyInfo PropertyInfo, ImmutableList<ValidationAttribute> ValidationAttributes)> GetValidationAttributesFromPropertiesOrConstructorParams(Type t)
        {
            var constructorParameters = ConstructorParamsCache.GetOrAdd(type, GetConstructorParams);
            var properties = new List<(PropertyInfo PropertyInfo, ImmutableList<ValidationAttribute> ValidationAttributes)>();

            foreach (var property in t.GetProperties())
            {
                if (!property.PropertyType.GetInterfaces().Contains(typeof(IOptionallyPatched))) continue;

                var validationAttributesFromProperty = property
                    .GetCustomAttributes<OptionallyPatchedValidationAttribute>()
                    .Select(ve => ve.ValidationAttribute);

                var validationAttributesFromConstructor = constructorParameters
                    .SingleOrDefault(cp => string.Equals(cp.Name, property.Name, StringComparison.Ordinal))
                    ?.GetCustomAttributes<OptionallyPatchedValidationAttribute>()
                    .Select(ve => ve.ValidationAttribute);

                var validationAttributes = validationAttributesFromProperty.Union(validationAttributesFromConstructor ?? Enumerable.Empty<ValidationAttribute>());

                properties.Add((property, validationAttributes.ToImmutableList()));
            }

            return properties.ToImmutableList();
        }

        return PropertiesWithValidationAttributesCache.GetOrAdd(type, GetValidationAttributesFromPropertiesOrConstructorParams);
    }
}