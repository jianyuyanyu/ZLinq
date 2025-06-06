// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace ZLinq.Tests
{
    public class AppendPrependTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQueryAppend()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;

            Assert.Equal(q1.Append(42), q1.Append(42));
            Assert.Equal(q1.Append(42), q1.Concat([42]));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQueryPrepend()
        {
            var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                     select x1;

            Assert.Equal(q1.Prepend(42), q1.Prepend(42));
            Assert.Equal(q1.Prepend(42), (new int?[] { 42 }).Concat(q1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryAppend()
        {
            var q1 = from x1 in new[] { "AAA", string.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;

            Assert.Equal(q1.Append("hi"), q1.Append("hi"));
            Assert.Equal(q1.Append("hi"), q1.Concat(["hi"]));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQueryPrepend()
        {
            var q1 = from x1 in new[] { "AAA", string.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                     select x1;

            Assert.Equal(q1.Prepend("hi"), q1.Prepend("hi"));
            Assert.Equal(q1.Prepend("hi"), (new string[] { "hi" }).Concat(q1));
        }

        [Fact(Skip = SkipReason.RefStruct)]
        public void RepeatIteration()
        {
            var q1 = Enumerable.Range(3, 4).Append(12);
            Assert.Equal(q1, q1);
            var q2 = q1.Append(14);
            Assert.Equal(q1, q2); // 14 is not contained on q1.
        }

        [Fact]
        public void EmptyAppend()
        {
            int[] first = [];
            Assert.All(CreateSources(first), first =>
            {
                Assert.Single(first.Append(42), 42);
            });
        }

        [Fact]
        public void EmptyPrepend()
        {
            string[] first = [];
            Assert.All(CreateSources(first), first =>
            {
                Assert.Single(first.Prepend("aa"), "aa");
            });
        }

        [Fact]
        public void PrependNoIteratingSourceBeforeFirstItem()
        {
            var ie = new List<int>();
            var prepended = (from i in ie select i).Prepend(4);

            ie.Add(42);

            Assert.Equal(prepended, ie.Prepend(4));
        }

        [Fact(Skip = SkipReason.EnumeratorBehaviorDifference)]
        public void ForcedToEnumeratorDoesntEnumeratePrepend()
        {
            var valueEnumerable = NumberRangeGuaranteedNotCollectionType(0, 3).Prepend(4);
            // Don't insist on this behaviour, but check it's correct if it happens
            using var en = valueEnumerable.Enumerator;
            Assert.False(en.TryGetNext(out _));
        }

        [Fact(Skip = SkipReason.EnumeratorBehaviorDifference)]
        public void ForcedToEnumeratorDoesntEnumerateAppend()
        {
            var valueEnumerable = NumberRangeGuaranteedNotCollectionType(0, 3).Append(4);
            // Don't insist on this behaviour, but check it's correct if it happens
            using var en = valueEnumerable.Enumerator;
            Assert.False(en.TryGetNext(out _));
        }

        [Fact(Skip = SkipReason.EnumeratorBehaviorDifference)]
        public void ForcedToEnumeratorDoesntEnumerateMultipleAppendsAndPrepends()
        {
            var valueEnumerable = NumberRangeGuaranteedNotCollectionType(0, 3).Append(4).Append(5).Prepend(-1).Prepend(-2);
            // Don't insist on this behaviour, but check it's correct if it happens
            using var en = valueEnumerable.Enumerator;
            Assert.False(en.TryGetNext(out _));
        }

        [Fact]
        public void SourceNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Append(1));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Prepend(1));
        }

        [Fact]
        public void Combined()
        {
            var v = "foo".Append('1').Append('2').Prepend('3').Concat("qq".Append('Q').Prepend('W'));

            Assert.Equal(v.ToArray(), "3foo12WqqQ".ToArray());

            var v1 = "a".Append('b').Append('c').Append('d');

            Assert.Equal(v1.ToArray(), "abcd".ToArray());

            var v2 = "a".Prepend('b').Prepend('c').Prepend('d');

            Assert.Equal(v2.ToArray(), "dcba".ToArray());
        }

        [Fact]
        public void AppendCombinations()
        {
            var source = Enumerable.Range(0, 3).Append(3).Append(4);
            var app0a = source.Append(5);
            var app0b = source.Append(6);
            var app1aa = app0a.Append(7);
            var app1ab = app0a.Append(8);
            var app1ba = app0b.Append(9);
            var app1bb = app0b.Append(10);

            Assert.Equal([0, 1, 2, 3, 4, 5], app0a);
            Assert.Equal([0, 1, 2, 3, 4, 6], app0b);
            Assert.Equal([0, 1, 2, 3, 4, 5, 7], app1aa);
            Assert.Equal([0, 1, 2, 3, 4, 5, 8], app1ab);
            Assert.Equal([0, 1, 2, 3, 4, 6, 9], app1ba);
            Assert.Equal([0, 1, 2, 3, 4, 6, 10], app1bb);
        }

        [Fact]
        public void PrependCombinations()
        {
            var source = Enumerable.Range(2, 2).Prepend(1).Prepend(0);
            var pre0a = source.Prepend(5);
            var pre0b = source.Prepend(6);
            var pre1aa = pre0a.Prepend(7);
            var pre1ab = pre0a.Prepend(8);
            var pre1ba = pre0b.Prepend(9);
            var pre1bb = pre0b.Prepend(10);

            Assert.Equal([5, 0, 1, 2, 3], pre0a);
            Assert.Equal([6, 0, 1, 2, 3], pre0b);
            Assert.Equal([7, 5, 0, 1, 2, 3], pre1aa);
            Assert.Equal([8, 5, 0, 1, 2, 3], pre1ab);
            Assert.Equal([9, 6, 0, 1, 2, 3], pre1ba);
            Assert.Equal([10, 6, 0, 1, 2, 3], pre1bb);
        }

        [Fact]
        public void Append1ToArrayToList()
        {
            Assert.All(CreateSources(Enumerable.Range(0, 2)), source =>
            {
                var valueEnumerable = source.Append(2);
                Assert.Equal(Enumerable.Range(0, 3), valueEnumerable.ToList());
                Assert.Equal(Enumerable.Range(0, 3), valueEnumerable.ToArray());
            });
        }

        [Fact]
        public void Prepend1ToArrayToList()
        {
            Assert.All(CreateSources(Enumerable.Range(1, 2)), source =>
            {
                var valueEnumerable = source.Prepend(0);
                Assert.Equal(Enumerable.Range(0, 3), valueEnumerable.ToList());
                Assert.Equal(Enumerable.Range(0, 3), valueEnumerable.ToArray());
            });
        }

        [Fact]
        public void AppendNToArrayToList()
        {
            Assert.All(CreateSources(Enumerable.Range(0, 2)), source =>
            {
                var valueEnumerable = source.Append(2).Append(3);
                Assert.Equal(Enumerable.Range(0, 4), valueEnumerable.ToList());
                Assert.Equal(Enumerable.Range(0, 4), valueEnumerable.ToArray());
            });
        }

        [Fact]
        public void PrependNToArrayToList()
        {
            Assert.All(CreateSources(Enumerable.Range(2, 2)), source =>
            {
                var valueEnumerable = source.Prepend(1).Prepend(0);
                Assert.Equal(Enumerable.Range(0, 4), valueEnumerable.ToList());
                Assert.Equal(Enumerable.Range(0, 4), valueEnumerable.ToArray());
            });
        }

        [Fact]
        public void AppendPrependToArrayToList()
        {
            Assert.All(CreateSources(Enumerable.Range(2, 2)), source =>
            {
                var valueEnumerable = source.Prepend(1).Append(4).Prepend(0).Append(5);
                Assert.Equal(Enumerable.Range(0, 6), valueEnumerable.ToList());
                Assert.Equal(Enumerable.Range(0, 6), valueEnumerable.ToArray());
            });
        }

        [Fact]
        public void AppendPrependRunOnce()
        {
            Assert.All(CreateSources(Enumerable.Range(2, 2)), source =>
            {
                Assert.Equal(Enumerable.Range(0, 6), source.RunOnce().Prepend(1).RunOnce().Prepend(0).RunOnce().Append(4).RunOnce().Append(5).RunOnce().ToList());
                Assert.Equal(Enumerable.Range(0, 6), source.Prepend(1).Prepend(0).Append(4).Append(5).RunOnce().ToList());
            });
        }

        [Fact]
        public void AppendPrepend_First_Last_ElementAt()
        {
            Assert.Equal(42, new int[] { 42 }.Append(84).First());
            Assert.Equal(42, new int[] { 84 }.Prepend(42).First());
            Assert.Equal(84, new int[] { 42 }.Append(84).Last());
            Assert.Equal(84, new int[] { 84 }.Prepend(42).Last());
            Assert.Equal(42, new int[] { 42 }.Append(84).ElementAt(0));
            Assert.Equal(42, new int[] { 84 }.Prepend(42).ElementAt(0));
            Assert.Equal(84, new int[] { 42 }.Append(84).ElementAt(1));
            Assert.Equal(84, new int[] { 84 }.Prepend(42).ElementAt(1));

            Assert.Equal(42, NumberRangeGuaranteedNotCollectionType(42, 1).Append(84).First());
            Assert.Equal(42, NumberRangeGuaranteedNotCollectionType(84, 1).Prepend(42).First());
            Assert.Equal(84, NumberRangeGuaranteedNotCollectionType(42, 1).Append(84).Last());
            Assert.Equal(84, NumberRangeGuaranteedNotCollectionType(84, 1).Prepend(42).Last());
            Assert.Equal(42, NumberRangeGuaranteedNotCollectionType(42, 1).Append(84).ElementAt(0));
            Assert.Equal(42, NumberRangeGuaranteedNotCollectionType(84, 1).Prepend(42).ElementAt(0));
            Assert.Equal(84, NumberRangeGuaranteedNotCollectionType(42, 1).Append(84).ElementAt(1));
            Assert.Equal(84, NumberRangeGuaranteedNotCollectionType(84, 1).Prepend(42).ElementAt(1));
        }
    }
}
