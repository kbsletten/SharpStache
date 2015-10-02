using System.Collections.Generic;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpStache.Test
{
    [TestClass]
    public class LegacyDataTypeTest
    {
        [TestMethod]
        public void TestProperty()
        {
            Assert.AreEqual("123", SharpStache.Render("{{Test}}", new { Test = "123" }));
        }

        [TestMethod]
        public void TestDictionary()
        {
            Assert.AreEqual("123", SharpStache.Render("{{Test}}", new Dictionary<string, string> { { "Test", "123" } }));
        }

        struct TestStruct
        {
            public string Test;
        }

        [TestMethod]
        public void TestField()
        {
            Assert.AreEqual("123", SharpStache.Render("{{Test}}", new TestStruct { Test = "123" }));
        }

        class TestClass
        {
            public string Test()
            {
                return "123";
            }
        }

        [TestMethod]
        public void TestMethod()
        {
            Assert.AreEqual("123", SharpStache.Render("{{Test}}", new TestClass()));
        }

        [TestMethod]
        public void TestHtmlString()
        {
            Assert.AreEqual("<", SharpStache.Render("{{.}}", new HtmlString("<")));
        }
    }
}