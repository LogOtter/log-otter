using System.Reflection;
using Microsoft.Azure.Cosmos.Linq;

namespace LogOtter.CosmosDb.ContainerMock;

//TODO: check nullability

internal static class CosmosExpressionValidatorConstants
{
    public static readonly Dictionary<Type, List<string>> AllowList =
        new()
        {
            {
                typeof(string),
                new List<string>
                {
                    "Concat",
                    "Contains",
                    "Count",
                    "EndsWith",
                    "IndexOf",
                    "Replace",
                    "Reverse",
                    "StartsWith",
                    "SubString",
                    "ToLower",
                    "ToUpper",
                    "TrimEnd",
                    "TrimStart",
                    "Equals",
                }
            },
            {
                typeof(Math),
                new List<string>
                {
                    "Abs",
                    "Acos",
                    "Asin",
                    "Atan",
                    "Ceiling",
                    "Cos",
                    "Exp",
                    "Floor",
                    "Log",
                    "Log10",
                    "Pow",
                    "Round",
                    "Sign",
                    "Sin",
                    "Sqrt",
                    "Tan",
                    "Truncate",
                }
            },
            {
                typeof(Array),
                new List<string> { "Concat", "Contains", "Count" }
            },
            {
                typeof(Queryable),
                new List<string>
                {
                    "Select",
                    "Contains",
                    "Where",
                    "Single",
                    "SelectMany",
                    "OrderBy",
                    "OrderByDescending",
                    "ThenBy",
                    "ThenByDescending",
                    "Count",
                    "Sum",
                    "Min",
                    "Max",
                    "Average",
                    "CountAsync",
                    "SumAsync",
                    "MinAsync",
                    "MaxAsync",
                    "AverageAsync",
                    "Skip",
                    "Take",
                }
            },
            // Any is only on enumerable as it is supported as a sub-query but not as an aggregation
            {
                typeof(Enumerable),
                new List<string>
                {
                    "Select",
                    "Contains",
                    "Where",
                    "Single",
                    "SelectMany",
                    "OrderBy",
                    "OrderByDescending",
                    "ThenBy",
                    "ThenByDescending",
                    "Count",
                    "Sum",
                    "Min",
                    "Max",
                    "Average",
                    "CountAsync",
                    "SumAsync",
                    "MinAsync",
                    "MaxAsync",
                    "AverageAsync",
                    "Skip",
                    "Take",
                    "Any",
                }
            },
            {
                typeof(object),
                new List<string> { "ToString" }
            },
        };

    public static readonly MethodInfo IsNull = typeof(CosmosLinqExtensions).GetMethod(nameof(CosmosLinqExtensions.IsNull))!;

    public static readonly MethodInfo IsDefined = typeof(CosmosLinqExtensions).GetMethod(nameof(CosmosLinqExtensions.IsDefined))!;
}
