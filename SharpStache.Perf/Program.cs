using System.IO;
using System.Threading;
using Nustache.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SharpStache.Perf
{
    public enum Test
    {
        Empty,
        Text,
        Simple,
        Loop
    }

    public enum Alternative
    {
        StringReplace,
        SharpStache,
        Nustache
    }

    public class Program
    {
        public const long IterationCount = 100000;
        public const int ThreadCount = 16;
        public const int Trials = 10;

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
            var trials = new double[Enum.GetValues(typeof (Alternative)).Length, Enum.GetValues(typeof (Test)).Length, Trials];
            for (var i = 0; i < Trials; i++)
            {
                trials[(int) Alternative.StringReplace, (int) Test.Empty, i] = TestEmpty(StringReplaceRender); GC.Collect();
                trials[(int) Alternative.StringReplace, (int) Test.Text, i] = TestText(StringReplaceRender); GC.Collect();
                trials[(int) Alternative.StringReplace, (int) Test.Simple, i] = TestSimple(StringReplaceRender); GC.Collect();

                trials[(int) Alternative.SharpStache, (int) Test.Empty, i] = TestEmpty(SharpStacheRender); GC.Collect();
                trials[(int) Alternative.SharpStache, (int) Test.Text, i] = TestText(SharpStacheRender); GC.Collect();
                trials[(int) Alternative.SharpStache, (int) Test.Simple, i] = TestSimple(SharpStacheRender); GC.Collect();
                trials[(int) Alternative.SharpStache, (int) Test.Loop, i] = TestLoop(SharpStacheRender); GC.Collect();

                trials[(int) Alternative.Nustache, (int) Test.Empty, i] = TestEmpty(NustacheRender); GC.Collect();
                trials[(int) Alternative.Nustache, (int) Test.Text, i] = TestText(NustacheRender); GC.Collect();
                trials[(int) Alternative.Nustache, (int) Test.Simple, i] = TestSimple(NustacheRender); GC.Collect();
                trials[(int) Alternative.Nustache, (int) Test.Loop, i] = TestLoop(NustacheRender); GC.Collect();
            }
            using (var file = new FileStream("performance.csv", FileMode.Create))
            using (var writer = new StreamWriter(file))
            foreach (var i in Enum.GetValues(typeof (Alternative)).Cast<Alternative>())
            {
                foreach (var j in Enum.GetValues(typeof (Test)).Cast<Test>())
                {
                    var times = Enumerable.Range(0, Trials).Select(k => trials[(int)i, (int)j, k]).ToList();
                    var avg = times.Average();
                    var dev = times.Select(t => t - avg).Select(t => t*t).Select(t => t/times.Count).Sum();
                    writer.WriteLine("{0},{1},{2}", i, j, string.Join(", ", trials));
                    Console.WriteLine("{0} {1}: avg={2} dev={3}", i, j, avg, dev);
                }
            }
        }
    }
}
