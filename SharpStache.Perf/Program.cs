using System.Threading;
using Nustache.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SharpStache.Perf
{
    public class Program
    {
        public const long IterationCount = 100000;
        public const int ThreadCount = 16;

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
            return time / (double)IterationCount;
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

        private static string StringReplaceRender(string template, object data)
        {
            if (data == null)
            {
                return template;
            }
            return data.GetType()
                .GetProperties()
                .Aggregate(template, (s, p) => s.Replace("{{" + p.Name + "}}", p.GetValue(data).ToString()));
        }

        public static string SharpStacheRender(string template, object data)
        {
            return SharpStache.Render(template, data);
        }

        public static string NustacheRender(string template, object data)
        {
            return Render.StringToString(template, data);
        }

        public static void Main(string[] args)
        {
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("=== Trial {0} ===", i + 1);

                Console.WriteLine("--- StringReplace ---");
                Console.WriteLine("[{0:N6} ms] Empty", TestEmpty(StringReplaceRender));
                Console.WriteLine("[{0:N6} ms] Text", TestText(StringReplaceRender));
                Console.WriteLine("[{0:N6} ms] Simple", TestSimple(StringReplaceRender));
                Console.WriteLine();

                Console.WriteLine("--- SharpStache ---");
                Console.WriteLine("[{0:N6} ms] Empty", TestEmpty(SharpStacheRender));
                Console.WriteLine("[{0:N6} ms] Text", TestText(SharpStacheRender));
                Console.WriteLine("[{0:N6} ms] Simple", TestSimple(SharpStacheRender));
                Console.WriteLine("[{0:N6} ms] Loop", TestLoop(SharpStacheRender));
                Console.WriteLine();
                GC.Collect();

                Console.WriteLine("--- Nustache ---");
                Console.WriteLine("[{0:N6} ms] Empty", TestEmpty(NustacheRender));
                Console.WriteLine("[{0:N6} ms] Text", TestText(NustacheRender));
                Console.WriteLine("[{0:N6} ms] Simple", TestSimple(NustacheRender));
                Console.WriteLine("[{0:N6} ms] Loop", TestLoop(NustacheRender));
                Console.WriteLine();
                GC.Collect();
            }
        }
    }
}
