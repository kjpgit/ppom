using System;
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
    }
}
