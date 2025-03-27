using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeaveTracker.UnitTests
{
    public static class DbContextMockExtensions
    {
        public static Mock<DbSet<T>> CreateMockDbSet<T>(this List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            mockSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            mockSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(entity => data.Add(entity));
            mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Returns((T entity, CancellationToken _) =>
                {
                    data.Add(entity);
                    return new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>>(Task.FromResult((Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<T>)null));
                });

            return mockSet;
        }

        public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
            public ValueTask DisposeAsync() => new ValueTask(Task.Run(() => _inner.Dispose()));
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
            public T Current => _inner.Current;
        }
    }
}