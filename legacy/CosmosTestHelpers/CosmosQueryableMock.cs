using System.Collections;
using System.Linq.Expressions;

namespace CosmosTestHelpers;

public class CosmosQueryableMock<T> : IOrderedQueryable<T>, IQueryProvider
{
    private EnumerableQuery<T> _underlying;

    public CosmosQueryableMock(IQueryable<T> partition)
    {
        _underlying = new EnumerableQuery<T>(partition.Expression);
    }

    public IEnumerator<T> GetEnumerator()
    {
        var expression = ValidateExpression(Expression);
        _underlying = new EnumerableQuery<T>(expression);
        return ((IOrderedQueryable<T>)_underlying).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        var expression = ValidateExpression(Expression);
        _underlying = new EnumerableQuery<T>(expression);
        return((IEnumerable)_underlying).GetEnumerator();
    }

    public Type ElementType => ((IQueryable)_underlying).ElementType;

    public Expression Expression => ((IQueryable)_underlying).Expression;

    public IQueryProvider Provider => this;

    public IQueryable CreateQuery(Expression expression)
    {
        return((IQueryProvider)_underlying).CreateQuery(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var queryProvider = ((IQueryProvider)_underlying).CreateQuery<TElement>(expression);
        return new CosmosQueryableMock<TElement>(queryProvider);
    }

    public object Execute(Expression expression)
    {
        expression = ValidateExpression(expression);
        return ((IQueryProvider)_underlying).Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        expression = ValidateExpression(expression);
        return ((IQueryProvider)_underlying).Execute<TResult>(expression);
    }

    private Expression ValidateExpression(Expression expression)
    {
        var validator = new CosmosExpressionValidator<T>();
        var newExpression = validator.Visit(expression);
        return newExpression;
    }
}