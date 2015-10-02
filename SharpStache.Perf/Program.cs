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
        Mustache,
        Nustache
    }

    public class Program
    {
        public const long IterationCount = 100000;
        public const int ThreadCount = 16;
        public const int Trials = 10;

        public struct TestCase<T>
        {
            public string Template;
            public string Expected;
            public T Data;
        }

        public static TestCase<T> Case<T>(string template, string expected, T data)
        {
            return new TestCase<T>
            {
                Template = template,
                Expected = expected,
                Data = data
            };
        }

        public static double Sweat<T>(Func<string, T, string> render, TestCase<T> testCase)
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
                        var actual = render(testCase.Template, testCase.Data);
                        if (testCase.Expected != actual)
                        {
                            throw new Exception("Expected: <" + testCase.Expected + "> Actual <" + actual + ">");
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

        private static string StringReplaceRender<T>(string template, T data)
        {
            if (data == null)
            {
                return template;
            }
            return data.GetType()
                .GetProperties()
                .Aggregate(template, (s, p) => s.Replace("{{" + p.Name + "}}", p.GetValue(data).ToString()));
        }

        public static string SharpStacheRender<T>(string template, T data)
        {
            return SharpStache.Render(template, data);
        }

        public static string MustacheRender<T>(string template, T data)
        {
            return Mustache.Render(template, data);
        }

        public static string NustacheRender<T>(string template, T data)
        {
            return Render.StringToString(template, data);
        }

        public static void Main(string[] args)
        {
            var empty = Case("", "", default(object));
            var text = Case("Hello, world", "Hello, world", default(object));
            var simple = Case("Hello, {{name}}", "Hello, world", new {name = "world"});
            var loop = Case("Hello{{#people}}, {{name}}{{/people}}", "Hello, Joe, Jill, Jack, Janet",
                new
                {
                    people = new[] {new {name = "Joe"}, new {name = "Jill"}, new {name = "Jack"}, new {name = "Janet"}}
                });

            var trials = new double[Enum.GetValues(typeof (Alternative)).Length, Enum.GetValues(typeof (Test)).Length, Trials];
            for (var i = 0; i < Trials; i++)
            {
                if (true)
                {
                    trials[(int) Alternative.StringReplace, (int) Test.Empty, i] = Sweat(StringReplaceRender, empty); GC.Collect();
                    trials[(int) Alternative.StringReplace, (int) Test.Text, i] = Sweat(StringReplaceRender, text); GC.Collect();
                    trials[(int) Alternative.StringReplace, (int) Test.Simple, i] = Sweat(StringReplaceRender, simple); GC.Collect();
                    
                }
                if (true)
                {
                    trials[(int) Alternative.SharpStache, (int) Test.Empty, i] = Sweat(SharpStacheRender, empty); GC.Collect();
                    trials[(int) Alternative.SharpStache, (int) Test.Text, i] = Sweat(SharpStacheRender, text); GC.Collect();
                    trials[(int) Alternative.SharpStache, (int) Test.Simple, i] = Sweat(SharpStacheRender, simple); GC.Collect();
                    trials[(int) Alternative.SharpStache, (int) Test.Loop, i] = Sweat(SharpStacheRender, loop); GC.Collect();
                    
                }
                if (true)
                {
                    trials[(int) Alternative.Mustache, (int) Test.Empty, i] = Sweat(MustacheRender, empty); GC.Collect();
                    trials[(int) Alternative.Mustache, (int) Test.Text, i] = Sweat(MustacheRender, text); GC.Collect();
                    trials[(int) Alternative.Mustache, (int) Test.Simple, i] = Sweat(MustacheRender, simple); GC.Collect();
                    trials[(int) Alternative.Mustache, (int) Test.Loop, i] = Sweat(MustacheRender, loop); GC.Collect();
                    
                }
                if (true)
                {
                    trials[(int) Alternative.Nustache, (int) Test.Empty, i] = Sweat(NustacheRender, empty); GC.Collect();
                    trials[(int) Alternative.Nustache, (int) Test.Text, i] = Sweat(NustacheRender, text); GC.Collect();
                    trials[(int) Alternative.Nustache, (int) Test.Simple, i] = Sweat(NustacheRender, simple); GC.Collect();
                    trials[(int) Alternative.Nustache, (int) Test.Loop, i] = Sweat(NustacheRender, loop); GC.Collect();
                    
                }
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
                    writer.WriteLine("{0},{1},{2}", i, j, string.Join(", ", times));
                    Console.WriteLine("{0} {1}: avg={2} dev={3}", i, j, avg, dev);
                }
            }
        }
    }
}
