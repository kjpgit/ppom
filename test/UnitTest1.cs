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
            Assert.Equal(Extensions.GetDecimalPlaces(0), 0);
            Assert.Equal(Extensions.GetDecimalPlaces(0m), 0);
            Assert.Equal(Extensions.GetDecimalPlaces(1m), 0);
            Assert.Equal(Extensions.GetDecimalPlaces(123m), 0);
            Assert.Equal(Extensions.GetDecimalPlaces(123.0m), 1);
            Assert.Equal(Extensions.GetDecimalPlaces(123.00m), 2);
            Assert.Equal(Extensions.GetDecimalPlaces(123.000m), 3);
        }

        [Fact]
        public void TestEnumerate()
        {
            string[] myList = {"Hi", "There", "World"};
            var o = Extensions.Enumerate(myList).GetEnumerator();
            Assert.Equal(true, o.MoveNext());
            Assert.Equal((0, "Hi"), o.Current);
            Assert.Equal(true, o.MoveNext());
            Assert.Equal((1, "There"), o.Current);
            Assert.Equal(true, o.MoveNext());
            Assert.Equal((2, "World"), o.Current);
            Assert.Equal(false, o.MoveNext());
        }
    }
}
