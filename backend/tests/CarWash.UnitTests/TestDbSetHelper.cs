using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarWash.UnitTests;

public static class TestDbSetHelper
{
    public static DbSet<T> CreateMockDbSet<T>(List<T> sourceList) where T : class
    {
        var queryable = sourceList.AsQueryable();

        var dbSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        ((IQueryable<T>)dbSet).Provider.Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        ((IQueryable<T>)dbSet).Expression.Returns(queryable.Expression);
        ((IQueryable<T>)dbSet).ElementType.Returns(queryable.ElementType);
        ((IQueryable<T>)dbSet).GetEnumerator().Returns(queryable.GetEnumerator());

        ((IAsyncEnumerable<T>)dbSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return dbSet;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethods()
            .First(method => method.Name == nameof(IQueryProvider.Execute) && method.IsGenericMethod)
            .MakeGenericMethod(expectedResultType)
            .Invoke(this, new object[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression) : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }
}
