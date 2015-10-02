using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Web;

namespace SharpStache
{
    public interface IValue : IEnumerable<IValue>
    {
        bool IsTruthy { get; }
        bool IsIterable { get; }
        IValue this[string property] { get; }
        void Render(TextWriter writer, bool htmlEscape);
    }

    internal class LegacyValue : IValue
    {
        private readonly object _value;

        public LegacyValue(object value)
        {
            _value = value;
        }

        public bool IsTruthy
        {
            get { return _value != null && (!(_value is bool) || (bool) _value) && (!(_value is IEnumerable) || ((IEnumerable)_value).GetEnumerator().MoveNext()); }
        }

        public bool IsIterable
        {
            get { return _value != null && _value is IEnumerable; }
        }

        public IValue this[string property]
        {
            get
            {
                if (property == ".")
                    return this;

                if (_value == null)
                    return null;

                var dict = _value as IDictionary;
                if (dict != null)
                {
                    return new LegacyValue(dict[property]);
                }

                var field = _value.GetType().GetField(property);
                if (field != null)
                {
                    return new LegacyValue(field.GetValue(_value));
                }

                var prop = _value.GetType().GetProperty(property);
                if (prop != null)
                {
                    return new LegacyValue(prop.GetValue(_value, null));
                }

                var meth = _value.GetType().GetMethod(property);
                if (meth != null)
                {
                    return new LegacyValue(meth.Invoke(_value, null));
                }

                return null;
            }
        }

        public void Render(TextWriter writer, bool htmlEscape)
        {
            if (_value != null)
            {
                if (htmlEscape)
                {
                    var html = _value as IHtmlString;
                    writer.Write(html != null ? html.ToHtmlString() : WebUtility.HtmlEncode(_value.ToString()));
                }
                else
                {
                    writer.Write(_value.ToString());
                }
            }
        }

        public IEnumerator<IValue> GetEnumerator()
        {
            return ((IEnumerable) _value).Cast<object>().Select(o => new LegacyValue(o)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal interface IValueProvider<in T>
    {
        Func<T, bool> IsTruthyFunc { get; }
        Func<T, bool> IsIterableFunc { get; }
        Func<T, string, IValue> ItemsFunc { get; }
        Func<T, IEnumerator<IValue>> GetEnumeratorFunc { get; }
        Action<T, TextWriter, bool> RenderAction { get; }
    }

    internal class ValueProvider<T> : IValueProvider<T>
    {
        public Func<T, bool> IsTruthyFunc { get; private set; }
        public Func<T, bool> IsIterableFunc { get; private set; }
        public Func<T, string, IValue> ItemsFunc { get; private set; }
        public Func<T, IEnumerator<IValue>> GetEnumeratorFunc { get; private set; }
        public Action<T, TextWriter, bool> RenderAction { get; private set; }

        public static readonly ValueProvider<T> Instance = new ValueProvider<T>(); 

        private ValueProvider()
        {
            var t = Expression.Parameter(typeof (T), "t");
            if (typeof (T) == typeof (bool))
            {
                IsTruthyFunc = Expression.Lambda<Func<T, bool>>(t, t).Compile();
            }
            else if (typeof (IEnumerable).IsAssignableFrom(typeof (T)))
            {
                Expression isIt = Expression.Call(Expression.Call(t, typeof (IEnumerable).GetMethod("GetEnumerator", new Type[0]), new Expression[0]), typeof(IEnumerator).GetMethod("MoveNext", new Type[0]), new Expression[0]);
                if (!typeof (T).IsValueType)
                {
                    isIt = Expression.AndAlso(Expression.NotEqual(t, Expression.Constant(null, typeof (T))), isIt);
                }
                IsTruthyFunc =
                    Expression.Lambda<Func<T, bool>>(isIt, t).Compile();
            }
            else
            {
                var isNaN = typeof (T).GetMethod("IsNaN", new[] {typeof (T)});
                if (isNaN != null && isNaN.IsStatic)
                {
                    IsTruthyFunc =
                        Expression.Lambda<Func<T, bool>>(Expression.Call(typeof (T), "IsNaN", new Type[0], t), t)
                            .Compile();
                }
                else if (typeof (T).IsValueType)
                {
                    IsTruthyFunc = t1 => true;
                }
                else
                {
                    IsTruthyFunc = t1 => t1 != null;
                }
            }
            if (typeof (IEnumerable).IsAssignableFrom(typeof (T)))
            {
                IsIterableFunc = IsTruthyFunc;
            }
            else
            {
                IsIterableFunc = t1 => false;
            }
            var dicttype =
                typeof (T).GetInterfaces()
                    .Where(
                        i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IDictionary<,>) &&
                            i.GetGenericArguments()[0] == typeof (string))
                    .Select(i => i.GetGenericArguments()[1])
                    .FirstOrDefault();
            var p = Expression.Parameter(typeof (string), "p");
            if (dicttype != null)
            {
                var methodInfo = typeof (T).GetProperty("Item", dicttype, new[] {typeof (string)}).GetGetMethod();
                ItemsFunc =
                    Expression.Lambda<Func<T, string, IValue>>(
                        ToRenderer(dicttype, Expression.Call(t, methodInfo, new[] {(Expression) p})),
                        t,
                        p).Compile();
            }
            else
            {
                var ret = Expression.Label(typeof (IValue));
                var cases = new List<SwitchCase>();
                cases.AddRange(typeof (T).GetFields().Where(f => !f.IsStatic).Select(field => Expression.SwitchCase(Expression.Return(ret, ToRenderer(field.FieldType, Expression.Field(t, field))), Expression.Constant(field.Name))));
                cases.AddRange(typeof (T).GetProperties().Where(par => !par.GetGetMethod().IsStatic).Select(property => Expression.SwitchCase(Expression.Return(ret, ToRenderer(property.PropertyType, Expression.Property(t, property))), Expression.Constant(property.Name))));
                cases.AddRange(typeof (T).GetMethods().Where(par => !par.IsStatic && par.GetParameters().Length == 0 && par.ReturnType != typeof(void)).Select(property => Expression.SwitchCase(Expression.Return(ret, ToRenderer(property.ReturnType, Expression.Call(t, property, new Expression[0]))), Expression.Constant(property.Name))));
                if (cases.Count == 0)
                {
                    ItemsFunc = (t1, p1) => null;
                }
                else
                {
                    ItemsFunc = Expression.Lambda<Func<T, string, IValue>>(
                        Expression.Block(typeof (IValue),
                            Expression.Switch(p, cases.ToArray()),
                            Expression.Label(ret, Expression.Constant(null, typeof (IValue)))), t, p).Compile();
                }
            }
            var listtype =
                typeof (T).GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();
            if (listtype != null)
            {
                GetEnumeratorFunc =
                    Expression.Lambda<Func<T, IEnumerator<IValue>>>(
                        Expression.Call(typeof (Value<>).MakeGenericType(new[] {listtype}), "FromEnumerable",
                            new Type[0], t), t).Compile();
            }
            else
            {
                GetEnumeratorFunc = null;
            }
            if (typeof (IHtmlString).IsAssignableFrom(typeof (T)))
            {
                var w = Expression.Parameter(typeof (TextWriter), "w");
                var b = Expression.Parameter(typeof (bool), "b");
                RenderAction =
                    Expression.Lambda<Action<T, TextWriter, bool>>(
                        Expression.Call(w, typeof (TextWriter).GetMethod("Write", new[] {typeof (string)}),
                            new[]
                            {
                                (Expression)
                                    Expression.Call(t, typeof (IHtmlString).GetMethod("ToHtmlString", new Type[0]),
                                        new Expression[0])
                            }), t, w, b).Compile();
            }
            else if (typeof (T).IsValueType)
            {
                RenderAction = (t1, w1, b1) => w1.Write(b1 ? WebUtility.HtmlEncode(t1.ToString()) : t1.ToString());
            }
            else
            {
                RenderAction = (t1, w1, b1) =>
                {
                    if (t1 != null)
                    {
                        w1.Write(b1 ? WebUtility.HtmlEncode(t1.ToString()) : t1.ToString());
                    }
                };
            }
        }

        private static Expression ToRenderer(Type type, Expression value)
        {
            return Expression.Call(typeof(Value<>).MakeGenericType(new []{ type }), "ForItem", new Type[0], new []{ value });
        }
    }

    internal class Value<T> : IValue
    {
        private static readonly Type Type = typeof (T);
        private static readonly ValueProvider<T> Provider = ValueProvider<T>.Instance; 

        public static IEnumerator<IValue> FromEnumerable(IEnumerable<T> enumerable)
        {
            return enumerable.Select(ForItem).GetEnumerator();
        }

        private readonly T _value;

        public static IValue ForItem(T item)
        {
            return item != null && item.GetType() != Type
                ? (IValue)Activator.CreateInstance(typeof (Value<>).MakeGenericType(item.GetType()), item)
                : new Value<T>(item);
        }

        [Obsolete("Consider using `Value<T>.ForItem(T)`. Calling this constructor directly may cause unexpected behavior if the type of `value` is not exactly `T`")]
        public Value(T value)
        {
            _value = value;
        }

        public bool IsTruthy
        {
            get { return ((IValueProvider<T>) Provider).IsTruthyFunc(_value); }
        }

        public bool IsIterable
        {
            get { return ((IValueProvider<T>) Provider).IsIterableFunc(_value); }
        }

        public IValue this[string property]
        {
            get { return property == "." ? this : ((IValueProvider<T>) Provider).ItemsFunc(_value, property); }
        }

        public void Render(TextWriter writer, bool htmlEscape)
        {
            ((IValueProvider<T>) Provider).RenderAction(_value, writer, htmlEscape);
        }

        public IEnumerator<IValue> GetEnumerator()
        {
            return ((IValueProvider<T>) Provider).GetEnumeratorFunc(_value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal interface ITemplate
    {
        void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values);
    }

    internal class LiteralTemplate : ITemplate
    {
        private readonly string _template;
        private readonly int _offset;
        private readonly int _length;

        internal LiteralTemplate(string template, int offset, int length)
        {
            _template = template;
            _offset = offset;
            _length = length;
        }

        void ITemplate.Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            // avoid copy of substring
            for (var i = _offset; i < _offset + _length; i++)
            {
                writer.Write(_template[i]);
            }
        }
    }

    internal class SectionTemplate : MemberTemplate
    {
        private readonly ITemplate[] _inner;

        internal SectionTemplate(string name, ITemplate[] inner)
            : base(name)
        {
            _inner = inner;
        }

        protected override void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            var val = Get(values);

            if (val == null)
                return;

            if (val.IsIterable)
            {

                foreach (var v in val)
                {
                    values.Push(v);
                    foreach (var template in _inner)
                    {
                        template.Render(writer, partials, values);

                    }
                    values.Pop();
                }
            }
            else if (val.IsTruthy)
            {
                values.Push(val);
                foreach (var template in _inner)
                {
                    template.Render(writer, partials, values);
                }
                values.Pop();
            }
        }
    }

    internal class InvertedTemplate : MemberTemplate
    {
        private readonly ITemplate[] _inner;

        internal InvertedTemplate(string name, ITemplate[] inner)
            : base(name)
        {
            _inner = inner;
        }

        protected override void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            var val = Get(values);

            if (val != null && val.IsTruthy)
                return;

            foreach (var template in _inner)
            {
                template.Render(writer, partials, values);
            }
        }
    }

    internal class PartialTemplate : ITemplate
    {
        private readonly string _name;

        internal PartialTemplate(string name)
        {
            _name = name;
        }

        void ITemplate.Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            string partial;

            if (partials.TryGetValue(_name, out partial))
            {
                var templates = Parser.GetTemplates(partial);
                foreach (var template in templates)
                {
                    template.Render(writer, partials, values);
                }
            }
        }
    }

    internal class EscapeTemplate : MemberTemplate
    {
        internal EscapeTemplate(string name)
            : base(name)
        {

        }

        protected override void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            var renderer = Get(values);
            if (renderer != null)
            {
                renderer.Render(writer, false);
            }
        }
    }

    internal abstract class MemberTemplate : ITemplate
    {
        private readonly string[] _name;

        internal MemberTemplate(string name)
        {
            _name = name == "." ? new[] { name } : name.Split('.');
        }

        internal IValue Get(IEnumerable<IValue> values)
        {
            return (
                from value in values
                select GetVal(value, _name.First()) into val
                where val != null
                select _name.Skip(1).Aggregate(val, GetVal)
            ).FirstOrDefault();
        }

        private static IValue GetVal(IValue val, string name)
        {
            if (val == null) return null;
            return val[name];
        }

        void ITemplate.Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            Render(writer, partials, values);
        }

        protected abstract void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values);
    }

    internal class AttrTemplate : MemberTemplate
    {
        internal AttrTemplate(string name)
            : base(name)
        {
        }

        protected override void Render(TextWriter writer, IDictionary<string, string> partials, Stack<IValue> values)
        {
            var value = Get(values);
            if (value != null)
            {
                value.Render(writer, true);
            }
        }
    }

    internal static class Parser
    {
        internal static IEnumerable<ITemplate> GetTemplates(string template)
        {
            return GetTemplates(template, Lexer.GetTokens(template).GetEnumerator());
        }

        private static IEnumerable<ITemplate> GetTemplates(string template, IEnumerator<Token> tokens, string context = null)
        {
            while (tokens.MoveNext())
            {
                var token = tokens.Current;

                if (token.Type == TagType.Text)
                {
                    yield return new LiteralTemplate(template, token.Offset, token.Length);
                    continue;
                }

                var text = template.Substring(token.Offset, token.Length);

                switch (token.Type)
                {
                    case TagType.Escaped:
                        yield return new EscapeTemplate(text);
                        break;
                    case TagType.Attribute:
                        yield return new AttrTemplate(text);
                        break;
                    case TagType.Partial:
                        yield return new PartialTemplate(text);
                        break;
                    case TagType.Loop:
                        yield return new SectionTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.Inverted:
                        yield return new InvertedTemplate(text, GetTemplates(template, tokens, text).ToArray());
                        break;
                    case TagType.End:
                        if (context != text)
                            throw new Exception("Unmatched end tag. Got " + text + " expecting " + (context ?? "nothing"));
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}