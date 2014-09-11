using Nustache.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpStache.Perf
{
    class Program
    {
        public const int ALargeNumber = 10000;

        public static double Sweat(Func<string, object, string> render, string expected, string template, object data)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < ALargeNumber; i++)
            {
                var actual = render(template, data);
                if (expected != actual)
                {
                    throw new Exception("Expected: <" + expected + "> Actual <" + actual + ">");
                }
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds / (double)ALargeNumber;
        }

        public static double TestEmpty(Func<string, object, string> render)
        {
            var template = "";
            object data = null;
            var expected = "";

            return Sweat(render, expected, template, data);
        }

        public static double TestText(Func<string, object, string> render)
        {
            var template = "Hello, world";
            object data = null;
            var expected = "Hello, world";

            return Sweat(render, expected, template, data);
        }

        public static double TestSimple(Func<string, object, string> render)
        {
            var template = "Hello, {{name}}";
            var data = new { name = "world" };
            var expected = "Hello, world";

            return Sweat(render, expected, template, data);
        }

        public static double TestLoop(Func<string, object, string> render)
        {
            var template = "Hello{{#people}}, {{name}}{{/people}}";
            var data = new { people = new[] { new { name = "Joe" }, new { name = "Jill" }, new { name = "Jack" }, new { name = "Janet" } } };
            var expected = "Hello, Joe, Jill, Jack, Janet";

            return Sweat(render, expected, template, data);
        }

        public static string SharpStacheRender(string template, object data)
        {
            return SharpStache.Render(template, data);
        }

        public static string NustacheRender(string template, object data)
        {
            return Render.StringToString(template, data);
        }

        static void Main(string[] args)
        {
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("=== Trial {0} ===", i);
                
                Console.WriteLine("--- SharpStache ---");
                Console.WriteLine("[{0}ms] Empty", TestEmpty(SharpStacheRender));
                Console.WriteLine("[{0}ms] Text", TestText(SharpStacheRender));
                Console.WriteLine("[{0}ms] Simple", TestSimple(SharpStacheRender));
                Console.WriteLine("[{0}ms] Loop", TestLoop(SharpStacheRender));
                Console.WriteLine();
                GC.Collect();

                Console.WriteLine("--- Nustache ---");
                Console.WriteLine("[{0}ms] Empty", TestEmpty(NustacheRender));
                Console.WriteLine("[{0}ms] Text", TestText(NustacheRender));
                Console.WriteLine("[{0}ms] Simple", TestSimple(NustacheRender));
                Console.WriteLine("[{0}ms] Loop", TestLoop(NustacheRender));
                Console.WriteLine();
                GC.Collect();
            }
        }
    }
}
