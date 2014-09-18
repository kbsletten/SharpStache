using System.Threading;
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
        public const long IterationCount = 100000;
        public const int ThreadCount = 10;

        public static double Sweat(Func<string, object, string> render, string expected, string template, object data)
        {
            var threads = new Task[ThreadCount];
            var index = 0;
            var time = 0L;

            for (var i = 0; i < ThreadCount; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var j = Interlocked.Increment(ref index);
                    while (j < IterationCount)
                    {
                        var actual = render(template, data);
                        if (expected != actual)
                        {
                            throw new Exception("Expected: <" + expected + "> Actual <" + actual + ">");
                        }
                        j = Interlocked.Increment(ref index);
                    }
                    stopwatch.Stop();
                    Interlocked.Add(ref time, stopwatch.ElapsedMilliseconds);
                });
            }

            Task.WaitAll(threads);

            return 1000 * time / (double)IterationCount;
        }

        public static double TestEmpty(Func<string, object, string> render)
        {
            var template = "";
            var expected = "";

            return Sweat(render, expected, template, null);
        }

        public static double TestText(Func<string, object, string> render)
        {
            var template = "Hello, world";
            var expected = "Hello, world";

            return Sweat(render, expected, template, null);
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
                Console.WriteLine("[{0}µs] Empty", TestEmpty(SharpStacheRender));
                Console.WriteLine("[{0}µs] Text", TestText(SharpStacheRender));
                Console.WriteLine("[{0}µs] Simple", TestSimple(SharpStacheRender));
                Console.WriteLine("[{0}µs] Loop", TestLoop(SharpStacheRender));
                Console.WriteLine();
                GC.Collect();

                Console.WriteLine("--- Nustache ---");
                Console.WriteLine("[{0}µs] Empty", TestEmpty(NustacheRender));
                Console.WriteLine("[{0}µs] Text", TestText(NustacheRender));
                Console.WriteLine("[{0}µs] Simple", TestSimple(NustacheRender));
                Console.WriteLine("[{0}µs] Loop", TestLoop(NustacheRender));
                Console.WriteLine();
                GC.Collect();
            }
        }
    }
}
