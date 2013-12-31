using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpStache.Test
{
    [TestClass]
    public class SharpStacheTest
    {
        public const string Template = @"{{! Ignore me. }}{{#Greeting}}{{Greeting}}{{/Greeting}}{{^Greeting}}Hello{{/Greeting}}{{#Names}}, {{.}}{{/Names}}{{^Names}} World{{/Names}}.";

        public static readonly object ObjGreeting = new
        {
            Greeting = "Domo Arigato",
            Names = new []
            {
                "Mr. Roboto",
                "whoever else"
            }
        };

        public static readonly IDictionary<string, object> DictGreeting = new Dictionary<string, object>
        {
            {"Greeting", "Domo Arigato"},
            {"Names", new List<string>()
            {
                "Mr. Roboto",
                "whoever else"
            }}
        };

        public const string Expected = @"Domo Arigato, Mr. Roboto, whoever else.";
        public const string Default = @"Hello World.";

        [TestMethod]
        public void TestRender_Null()
        {
            Assert.AreEqual(Default, SharpStache.Render(Template, null));
        }

        [TestMethod]
        public void TestRender_Empty_Obj()
        {
            Assert.AreEqual(Default, SharpStache.Render(Template, new object()));
        }

        [TestMethod]
        public void TestRender_Empty_Dict()
        {
            Assert.AreEqual(Default, SharpStache.Render(Template, new Dictionary<string, object>()));
        }

        [TestMethod]
        public void TestRender_Obj()
        {
            Assert.AreEqual(Expected, SharpStache.Render(Template, ObjGreeting));
        }

        [TestMethod]
        public void TestRender_Dict()
        {
            Assert.AreEqual(Expected, SharpStache.Render(Template, DictGreeting));
        }
    }
}
