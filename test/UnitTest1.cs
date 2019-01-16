using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using ppom;

namespace ppomtest
{
    public class ExtensionsTest
    {
        [Fact]
        public void TestGetDecimalPlaces()
        {
            Assert.Equal(0, Extensions.GetDecimalPlaces(0));
            Assert.Equal(0, Extensions.GetDecimalPlaces(0m));
            Assert.Equal(0, Extensions.GetDecimalPlaces(1m));
            Assert.Equal(0, Extensions.GetDecimalPlaces(123m));
            Assert.Equal(1, Extensions.GetDecimalPlaces(123.0m));
            Assert.Equal(2, Extensions.GetDecimalPlaces(123.00m));
            Assert.Equal(3, Extensions.GetDecimalPlaces(123.000m));
        }

        [Fact]
        public void TestEnumerate()
        {
            string[] myList = {"Hi", "There", "World"};
            var o = Extensions.Enumerate(myList).GetEnumerator();
            Assert.True(o.MoveNext());
            Assert.Equal((0, "Hi"), o.Current);
            Assert.True(o.MoveNext());
            Assert.Equal((1, "There"), o.Current);
            Assert.True(o.MoveNext());
            Assert.Equal((2, "World"), o.Current);
            Assert.False(o.MoveNext());
        }

        [Fact]
        public void TestSplitChunks()
        {
            int[] myList = {1, 2, 3, 4, 5};
            var expected = new List<List<int>>();
            expected.Add(new List<int>{ 1, 2, 3});
            expected.Add(new List<int>{ 4, 5});
            var actual = Extensions.SplitChunks(myList, 3).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestSplitChunks2()
        {
            int[] myList = {1, 2, 3, 4};
            var expected = new List<List<int>>();
            expected.Add(new List<int>{ 1, 2});
            expected.Add(new List<int>{ 3, 4});
            var actual = Extensions.SplitChunks(myList, 2).ToList();
            Assert.Equal(expected, actual);
        }
    }
}
