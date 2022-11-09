using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosTestHelpers;

/// <summary>
/// This class can be used to rewrite the in memory queries to ensure the results match those from running ComosDb queries.
/// Please write integration tests for anything you implement.
/// </summary>
public class CosmosExpressionValidator<TModel> : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        var underlyingType = Nullable.GetUnderlyingType(node.Type);
        if (node.NodeType == ExpressionType.MemberAccess && underlyingType is { IsEnum: true })
        {
            return base.VisitMember(node);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        GuardInvalidMethods(node);

        if (HandleIsNullAndIsDefined(node, out var replacementExpression))
        {
            return base.Visit(replacementExpression);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.ExclusiveOr)
        {
            return Expression.Equal(Expression.Constant(true), Expression.Constant(false));
        }

        return base.VisitBinary(HandleNullableEnumEquality(node));
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not && node.Operand.NodeType == ExpressionType.ExclusiveOr)
        {
            return Expression.Equal(Expression.Constant(true), Expression.Constant(false));
        }

        return base.VisitUnary(node);
    }

    private static bool HandleIsNullAndIsDefined(MethodCallExpression methodCallExpression, out Expression replacementExpression)
    {
        if (methodCallExpression.Method == CosmosExpressionValidatorConstants.IsDefined)
        {
            replacementExpression = Expression.Constant(true);
            return true;
        }

        if (methodCallExpression.Method == CosmosExpressionValidatorConstants.IsNull)
        {
            replacementExpression = Expression.Equal(methodCallExpression.Arguments.SingleOrDefault() ?? Expression.Constant(null), Expression.Constant(null));
            return true;
        }

        replacementExpression = null;
        return false;
    }

    /// <summary>
    /// Cosmos does not error if you compare the value of a null enum to null but c# does.
    /// This substitutes a dummy value to avoid the null reference exceptions
    /// </summary>
    private static BinaryExpression HandleNullableEnumEquality(BinaryExpression node)
    {
        BinaryExpression HandleNullableEnumEqualityInner(ExpressionType nodeType, Expression modelSide, Expression constantSide)
        {
            if (modelSide.NodeType == ExpressionType.Convert && modelSide.Type == typeof(int?) && modelSide is UnaryExpression leftConvert && constantSide.NodeType == ExpressionType.Convert && constantSide is UnaryExpression rightConvert)
            {
                var enumType = Nullable.GetUnderlyingType(leftConvert.Operand.Type) ?? leftConvert.Operand.Type;
                if (leftConvert.Operand.NodeType == ExpressionType.MemberAccess && enumType.IsEnum && leftConvert.Operand is MemberExpression leftMember && leftMember.Member.Name == "Value")
                {
                    var nullableEnumType = typeof(Nullable<>).MakeGenericType(enumType);
                    var convertedDefaultValueForNullableEnum = Expression.Convert(Expression.Constant(int.MinValue), nullableEnumType);

                    var coalesce = Expression.Coalesce(leftMember.Expression, convertedDefaultValueForNullableEnum);
                    var coalescedConvert = Expression.Convert(Expression.MakeMemberAccess(coalesce, leftMember.Member), modelSide.Type);

                    var coalescedConstant = Expression.Convert(Expression.Coalesce(rightConvert.Operand, convertedDefaultValueForNullableEnum), typeof(int?));

                    return nodeType == ExpressionType.Equal ? Expression.Equal(coalescedConvert, coalescedConstant) : Expression.NotEqual(coalescedConvert, coalescedConstant);
                }
            }

            return nodeType == ExpressionType.Equal ? Expression.Equal(modelSide, constantSide) : Expression.NotEqual(modelSide, constantSide);
        }

        if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
        {
            node = HandleNullableEnumEqualityInner(node.NodeType, node.Left, node.Right);
            node = HandleNullableEnumEqualityInner(node.NodeType, node.Right, node.Left);
        }

        return node;
    }

    private void GuardInvalidMethods(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(ContainerMock))
        {
            return;
        }

        if (!CosmosExpressionValidatorConstants.AllowList.ContainsKey(node.Method.DeclaringType))
        {
            var nodeObject = node.Object;
            if (node.Method.ReturnType.IsPrimitive && (nodeObject == null || nodeObject.Type != typeof(TModel)))
            {
                return;
            }

            throw new CosmosException($"Methods from {node.Method.DeclaringType} are not supported by Cosmos", HttpStatusCode.BadRequest, 0, string.Empty, 0);
        }

        var allowList = CosmosExpressionValidatorConstants.AllowList[node.Method.DeclaringType];
        if (!allowList.Contains(node.Method.Name))
        {
            throw new CosmosException($"{node.Method.DeclaringType}.{node.Method.Name} is not supported by Cosmos", HttpStatusCode.BadRequest, 0, string.Empty, 0);
        }
    }
}

internal static class CosmosExpressionValidatorConstants
{
    public static readonly Dictionary<Type, List<string>> AllowList = new()
    {
        { typeof(string), new List<string> { "Concat", "Contains", "Count", "EndsWith", "IndexOf", "Replace", "Reverse", "StartsWith", "SubString", "ToLower", "ToUpper", "TrimEnd", "TrimStart" } },
        { typeof(Math), new List<string> { "Abs", "Acos", "Asin", "Atan", "Ceiling", "Cos", "Exp", "Floor", "Log", "Log10", "Pow", "Round", "Sign", "Sin", "Sqrt", "Tan", "Truncate" } },
        { typeof(Array), new List<string> { "Concat", "Contains", "Count" } },
        { typeof(Queryable), new List<string> { "Select", "Contains", "Where", "Single", "SelectMany", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "Count", "Sum", "Min", "Max", "Average", "CountAsync", "SumAsync", "MinAsync", "MaxAsync", "AverageAsync", "Skip", "Take" } },
        // Any is only on enumerable as it is supported as a sub-query but not as an aggregation
        { typeof(Enumerable), new List<string> { "Select", "Contains", "Where", "Single", "SelectMany", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "Count", "Sum", "Min", "Max", "Average", "CountAsync", "SumAsync", "MinAsync", "MaxAsync", "AverageAsync", "Skip", "Take", "Any" } },
        { typeof(object), new List<string> { "ToString" } }
    };

    public static readonly MethodInfo IsNull = typeof(CosmosLinqExtensions).GetMethod(nameof(CosmosLinqExtensions.IsNull));

    public static readonly MethodInfo IsDefined = typeof(CosmosLinqExtensions).GetMethod(nameof(CosmosLinqExtensions.IsDefined));
}