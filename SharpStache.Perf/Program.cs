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

        static void Main(string[] args)
        {
            for (var i = 0; i < 10; i++)
            {
                TestSimple(SharpStache.Render);
                TestLoop(SharpStache.Render);
                TestSimple((t, d) => Render.StringToString(t, d));
                TestLoop((t, d) => Render.StringToString(t, d));
            }
            
            Console.WriteLine("SharpStache Simple {0}ms", TestSimple(SharpStache.Render));
            Console.WriteLine("SharpStache Loop {0}ms", TestLoop(SharpStache.Render));

            Console.WriteLine("Nustache Simple {0}ms", TestSimple((t, d) => Render.StringToString(t, d)));
            Console.WriteLine("Nustache Loop {0}ms", TestLoop((t, d) => Render.StringToString(t, d)));
        }
    }
}
