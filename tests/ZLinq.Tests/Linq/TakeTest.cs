﻿using System;
using System.Linq;

namespace ZLinq.Tests.Linq;

public class TakeTest
{
    [Fact]
    public void Take_Empty()
    {
        var empty = Array.Empty<int>();

        var expected = empty.Take(5).ToArray();
        var actual1 = empty.AsValueEnumerable().Take(5).ToArray();
        var actual2 = empty.ToValueEnumerable().Take(5).ToArray();

        actual1.ShouldBe(expected);
        actual2.ShouldBe(expected);
    }

    [Fact]
    public void Take_Zero()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();

        var expected = sequence.Take(0).ToArray();
        var actual1 = sequence.AsValueEnumerable().Take(0).ToArray();
        var actual2 = sequence.ToValueEnumerable().Take(0).ToArray();

        actual1.ShouldBe(expected); // Should be empty
        actual2.ShouldBe(expected); // Should be empty
    }

    [Fact]
    public void Take_Negative()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();

        var expected = sequence.Take(-5).ToArray();
        var actual1 = sequence.AsValueEnumerable().Take(-5).ToArray();
        var actual2 = sequence.ToValueEnumerable().Take(-5).ToArray();

        actual1.ShouldBe(expected); // Should be empty
        actual2.ShouldBe(expected); // Should be empty
    }

    [Fact]
    public void Take_PartialCollection()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();

        var expected = sequence.Take(5).ToArray();
        var actual1 = sequence.AsValueEnumerable().Take(5).ToArray();
        var actual2 = sequence.ToValueEnumerable().Take(5).ToArray();

        actual1.ShouldBe(expected); // Should be [1,2,3,4,5]
        actual2.ShouldBe(expected); // Should be [1,2,3,4,5]
    }

    [Fact]
    public void Take_ExceedingSize()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();

        var expected = sequence.Take(20).ToArray();
        var actual1 = sequence.AsValueEnumerable().Take(20).ToArray();
        var actual2 = sequence.ToValueEnumerable().Take(20).ToArray();

        actual1.ShouldBe(expected); // Should return all elements
        actual2.ShouldBe(expected); // Should return all elements
    }

    [Fact]
    public void Take_ExactSize()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();

        var expected = sequence.Take(10).ToArray();
        var actual1 = sequence.AsValueEnumerable().Take(10).ToArray();
        var actual2 = sequence.ToValueEnumerable().Take(10).ToArray();

        actual1.ShouldBe(expected); // Should return all elements
        actual2.ShouldBe(expected); // Should return all elements
    }

    [Fact]
    public void Take_TryGetNonEnumeratedCount()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        bool result = takeOperation.TryGetNonEnumeratedCount(out int count);

        result.ShouldBeTrue();
        count.ShouldBe(5);
    }

    [Fact]
    public void Take_TryGetNonEnumeratedCount_LargerThanCollection()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(20);

        bool result = takeOperation.TryGetNonEnumeratedCount(out int count);

        result.ShouldBeTrue();
        count.ShouldBe(10); // Should return the smaller of takCount and collection size
    }

    [Fact]
    public void Take_TryGetSpan()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        bool result = takeOperation.TryGetSpan(out var span);

        result.ShouldBeTrue();
        span.Length.ShouldBe(5);
        span.ToArray().ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void Take_TryCopyTo()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        Span<int> destination = new int[5];
        bool result = takeOperation.TryCopyTo(destination);

        result.ShouldBeTrue();
        destination.ToArray().ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void TryCopyTo2()
    {
        var take = ValueEnumerable.Range(1, 5).Take(3); // 1, 2, 3

        var dest = new int[5];

        Array.Clear(dest);
        take.TryCopyTo(dest, 0).ShouldBeTrue();
        dest.ShouldBe([1, 2, 3, 0, 0]);

        Array.Clear(dest);
        take.TryCopyTo(dest, 1).ShouldBeTrue();
        dest.ShouldBe([2, 3, 0, 0, 0]);

        Array.Clear(dest);
        take.TryCopyTo(dest, 2).ShouldBeTrue();
        dest.ShouldBe([3, 0, 0, 0, 0]);
    }

    [Fact]
    public void Take_TryCopyTo_SmallDestination()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        Span<int> smallDestination = new int[3];
        bool result = takeOperation.TryCopyTo(smallDestination);

        result.ShouldBeTrue();
        smallDestination.ToArray().ShouldBe(new[] { 1, 2, 3 }); // Should only copy what fits
    }

    [Fact]
    public void Take_TryCopyTo_LargeDestination()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        Span<int> largeDestination = new int[10];
        Array.Fill(largeDestination.ToArray(), -1); // Fill with placeholder values
        bool result = takeOperation.TryCopyTo(largeDestination);

        result.ShouldBeTrue();
        // Only the first 5 elements should be copied, rest should remain unchanged
        largeDestination.Slice(0, 5).ToArray().ShouldBe(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void Take_TryCopyTo_WithOffset()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        Span<int> destination = new int[5];
        bool result = takeOperation.TryCopyTo(destination, 2); // Start from the third element

        result.ShouldBeTrue();
        destination.ToArray().ShouldBe(new[] { 3, 4, 5, 0, 0 });
    }

    [Fact]
    public void Take_TryCopyTo_EmptyDestination()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(5);

        Span<int> emptyDestination = Span<int>.Empty;
        bool result = takeOperation.TryCopyTo(emptyDestination);

        result.ShouldBeTrue(); // Should succeed since we're copying 0 elements
    }

    [Fact]
    public void Take_TryCopyTo_ZeroTake()
    {
        var sequence = Enumerable.Range(1, 10).ToArray();
        var takeOperation = sequence.AsValueEnumerable().Take(0);

        Span<int> destination = new int[5];
        destination.Fill(-1);
        bool result = takeOperation.TryCopyTo(destination);

        result.ShouldBeFalse();
    }

    [Fact]
    public void Take_TryCopyTo_SourceDoesNotSupportNonEnumeratedCount()
    {
        // Create a source that doesn't support TryGetNonEnumeratedCount
        var nonCountableSource = new[] { 1, 2, 3, 4, 5 }
            .AsValueEnumerable()
            .Where(x => true); // Where operation doesn't support non-enumerated count

        var takeOperation = nonCountableSource.Take(3);

        Span<int> destination = new int[5];
        bool result = takeOperation.TryCopyTo(destination);

        result.ShouldBeFalse(); // Should fail as we need count information
    }

    [Fact]
    public void TakeAnd()
    {
        int[] source = [1, 2, 3, 4, 5];
        {
            var expected = source.Take(1).Last();
            var actual = source.AsValueEnumerable().Take(1).Last();
            actual.ShouldBe(expected);
        }
        {
            var expected = source.Take(3).ElementAt(1);
            var actual = source.AsValueEnumerable().Take(3).ElementAt(1);
            actual.ShouldBe(expected);
        }
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => source.Take(3).ElementAt(9999)); // expected
            Assert.Throws<ArgumentOutOfRangeException>(() => source.AsValueEnumerable().Take(3).ElementAt(9999)); // actual
        }
    }

#if !NET48

    [Fact]
    public void T()
    {
        int[] source = [1, 2, 3, 4, 5];
        source.AsValueEnumerable().Take(3).ElementAt(0).ShouldBe(1);
        source.AsValueEnumerable().Take(3).ElementAt(1).ShouldBe(2);
        source.AsValueEnumerable().Take(3).ElementAt(2).ShouldBe(3);
        source.AsValueEnumerable().Take(3).ElementAtOrDefault(3).ShouldBe(0);
    }

    [Fact]
    public void T2()
    {
        int[] source = [1, 2, 3, 4, 5];
        source.AsValueEnumerable().Take(3).Skip(1).ToArray().ShouldBe([2, 3]);
        source.AsValueEnumerable().Skip(1).Take(3).ToArray().ShouldBe([2, 3, 4]);
    }

    [Fact]
    public void T3()
    {
        IEnumerable<int> source = ForceNotCollection([1, 2, 3, 4, 5]);

        // LINQ
        {
            var values = source.Take(^5..3); // 1,2,3
            Assert.Equal(values.ToArray(), values.ToArray());
        }

        // ZLinq
        {
            var values = source.AsValueEnumerable().Take(^5..3); // 1,2,3
            Assert.Equal(values.ToArray(), values.ToArray());
        }

        static IEnumerable<T> ForceNotCollection<T>(IEnumerable<T> source)
        {
            foreach (T item in source)
                yield return item;
        }

    }

    [Fact]
    public void T4()
    {
        int[] source = [1, 2, 3, 4, 5];

        // LINQ
        Assert.Equal(0, source.Take(0..0).LastOrDefault());

        // ZLinq
        Assert.Equal(0, source.AsValueEnumerable().Take(0..0).LastOrDefault());
    }

    [Fact]
    public void T5()
    {
        int[] source = [1, 2, 3, 4, 5];

        // System.Linq
        Assert.Empty(source.Take(int.MaxValue..^int.MaxValue).ToArray());

        // ZLinq
        Assert.Empty(source.AsValueEnumerable().Take(int.MaxValue..^int.MaxValue).ToArray());
    }

    [Fact]
    public void DisposeSource_StartIndexFromEnd_ShouldDisposeOnFirstElement()
    {
        const int count = 5;
        int state = 0;
        var source = new DelegateIterator<int>(
            moveNext: () => ++state <= count,
            current: () => state,
            dispose: () => state = -1);

        using var e = source.Take(^3..).GetEnumerator();
        Assert.True(e.MoveNext());
        Assert.Equal(3, e.Current);

        Assert.Equal(-1, state);
        Assert.True(e.MoveNext());
    }

    [Fact]
    public void DisposeSource_EndIndexFromEnd_ShouldDisposeOnCompletedEnumeration()
    {
        const int count = 5;
        int state = 0;
        var source = new DelegateIterator<int>(
            moveNext: () => ++state <= count,
            current: () => state,
            dispose: () => state = -1);

        using var e = source.Take(..^3).GetEnumerator();

        Assert.True(e.MoveNext());
        Assert.Equal(4, state);
        Assert.Equal(1, e.Current);

        Assert.True(e.MoveNext());
        Assert.Equal(5, state);
        Assert.Equal(2, e.Current);

        Assert.False(e.MoveNext());
        Assert.Equal(-1, state);
    }

    // [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))]
    [Theory]
    [InlineData(1000)]
    [InlineData(1000000)]
    [InlineData(int.MaxValue)]
    public void LazySkipAllTakenForLargeNumbers(int largeNumber)
    {
        Assert.Empty(new FastInfiniteEnumerator<int>().AsValueEnumerable().Take(largeNumber).Skip(largeNumber).Skip(42).ToArray());
        Assert.Empty(new FastInfiniteEnumerator<int>().AsValueEnumerable().Take(largeNumber).Skip(largeNumber / 2).Skip(largeNumber / 2 + 1).ToArray());

        //Assert.Empty(new FastInfiniteEnumerator<int>().AsValueEnumerable().Take(0..largeNumber).Skip(largeNumber).ToArray());
        //Assert.Empty(new FastInfiniteEnumerator<int>().AsValueEnumerable().Take(0..largeNumber).Skip(largeNumber).Skip(42).ToArray());
        //Assert.Empty(new FastInfiniteEnumerator<int>().AsValueEnumerable().Take(0..largeNumber).Skip(largeNumber / 2).Skip(largeNumber / 2 + 1).ToArray());
    }

#endif
}
