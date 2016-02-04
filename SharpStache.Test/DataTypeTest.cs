using System;
using System.Collections.Generic;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpStache.Test
{
    [TestClass]
    public class DataTypeTest
    {
        [TestMethod]
        public void TestProperty()
        {
            Assert.AreEqual("123", Mustache.Render("{{Test}}", new { Test = "123" }));
        }

        [TestMethod]
        public void TestDictionary()
        {
            Assert.AreEqual("123", Mustache.Render("{{Test}}", new Dictionary<string, string> { { "Test", "123" } }));
        }

        struct TestStruct
        {
            public string Test;
        }

        [TestMethod]
        public void TestField()
        {
            Assert.AreEqual("123", Mustache.Render("{{Test}}", new TestStruct { Test = "123" }));
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
            Assert.AreEqual("123", Mustache.Render("{{Test}}", new TestClass()));
        }

        [TestMethod]
        public void TestHtmlString()
        {
            Assert.AreEqual("<", Mustache.Render("{{.}}", new HtmlString("<")));
        }

        [TestMethod]
        public void TestDownCast()
        {
            Assert.AreEqual("123", Mustache.Render("{{#.}}{{.}}{{/.}}", new object[] { 1, 2.0, "3" }));
        }

        [TestMethod]
        public void TestUnderscore()
        {
            Assert.AreEqual("123", SharpStache.Render("{{_Test}}", new { _Test = 123 }));
            Assert.AreEqual("123", Mustache.Render("{{_Test}}", new { _Test = 123 }));
        }
    }
}
