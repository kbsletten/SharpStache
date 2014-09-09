using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace SharpStache.Test
{
    [TestClass]
    public class PerfTest
    {
        public const int ALargeNumber = 10000;

        public static void Sweat(string expected, string template, object data)
        {
            for (var i = 0; i < ALargeNumber; i++)
            {
                Assert.AreEqual(expected, SharpStache.Render(template, data));
            }
        }

        [TestMethod]
        public void TestSimple()
        {
            var template = "Hello, {{name}}";
            var data = new { name = "world" };
            var expected = "Hello, world";

            Sweat(expected, template, data);
        }

        [TestMethod]
        public void TestLoop()
        {
            var template = "Hello{{#.}}, {{name}}{{/.}}";
            var data = new[] { new { name = "Joe" }, new { name = "Jill" }, new { name = "Jack" }, new { name = "Janet" } };
            var expected = "Hello, Joe, Jill, Jack, Janet";

            Sweat(expected, template, data);
        }
    }
}
