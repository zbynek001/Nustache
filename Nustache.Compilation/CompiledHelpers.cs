﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Nustache.Compilation
{
    public delegate Expression CompiledHelper(CompileContext context, IList<Expression> arguments, IDictionary<string, Expression> options/*, RenderBlock fn, RenderBlock inverse*/);

    //public delegate void HelperProxy(RenderBlock fn, RenderBlock inverse);
    //public delegate void RenderBlock(object data);

    public static class CompiledHelpers
    {
        private static readonly Dictionary<string, CompiledHelper> DefaultHelpers = new Dictionary<string, CompiledHelper>();
        private static readonly Dictionary<string, CompiledHelper> CustomHelpers = new Dictionary<string, CompiledHelper>();

        public static void Register(string name, CompiledHelper helper)
        {
            CustomHelpers.Add(name, helper);
        }

        public static bool Contains(string name)
        {
            return CustomHelpers.ContainsKey(name) || DefaultHelpers.ContainsKey(name);
        }

        public static CompiledHelper Get(string name)
        {
            return CustomHelpers.ContainsKey(name) ? CustomHelpers[name] : DefaultHelpers[name];
        }

        public static void Clear()
        {
            CustomHelpers.Clear();
        }

        static CompiledHelpers()
        {
            //DefaultHelpers["each"] = EachHelper;
            //DefaultHelpers["if"] = IfHelper;
            //DefaultHelpers["unless"] = UnlessHelper;
            //DefaultHelpers["with"] = WithHelper;
        }

        //public static void EachHelper(RenderContext context, IList<object> arguments, IDictionary<string, object> options, RenderBlock fn, RenderBlock inverse)
        //{
        //    foreach (var item in (IEnumerable)arguments[0]) {
        //        fn(item);
        //    }
        //}

        //public static void IfHelper(RenderContext context, IList<object> arguments, IDictionary<string, object> options, RenderBlock fn, RenderBlock inverse)
        //{
        //    var value = arguments[0];

        //    if (context.IsTruthy(value)) {
        //        fn(null);
        //    }
        //    else {
        //        inverse(null);
        //    }
        //}

        //public static void UnlessHelper(RenderContext context, IList<object> arguments, IDictionary<string, object> options, RenderBlock fn, RenderBlock inverse)
        //{
        //    IfHelper(context, arguments, options, inverse, fn);
        //}

        //public static void WithHelper(RenderContext context, IList<object> arguments, IDictionary<string, object> options, RenderBlock fn, RenderBlock inverse)
        //{
        //    fn(arguments[0]);
        //}

        private static IEnumerable<string> ExtractCaptureValues(MatchCollection matches, string groupName)
        {
            foreach (var match in matches) {
                var m = match as Match;
                if (m.Groups[groupName].Captures.Count != 0) {
                    yield return m.Value;
                }
            }
        }

        public static void Parse(CompileContext ctx, string path, out string name, out IList<Expression> arguments, out IDictionary<string, Expression> options)
        {
            name = path;
            arguments = null;
            options = null;

            if (path.Contains(" ")) {
                // unescaped: (?<option>\S+=(?:["'][^"']+["']|\S+))|(?<argument>["'][^"']+["']|\S+)
                const string pattern = "(?<option>\\S+=(?:[\"'][^\"']+[\"']|\\S+))|(?<argument>[\"'][^\"']+[\"']|\\S+)";
                var tokens = Regex.Matches(path, pattern);
                var argTokens = ExtractCaptureValues(tokens, "argument");
                var optTokens = ExtractCaptureValues(tokens, "option");
                name = ParseName(argTokens);
                arguments = ParseArguments(ctx, argTokens);
                options = ParseOptions(ctx, optTokens);
            }
        }

        private static string ParseName(IEnumerable<string> tokens)
        {
            var e = tokens.GetEnumerator();
            e.MoveNext();
            return e.Current;
        }

        private static IList<Expression> ParseArguments(CompileContext ctx, IEnumerable<string> tokens)
        {
            var e = tokens.GetEnumerator();
            e.MoveNext(); //Skip first argument as it's the helper name.

            var arguments = new List<Expression>();

            while (e.MoveNext()) {
                var value = e.Current;
                if (value[0] == '"' || value[0] == '\'') {
                    arguments.Add(Expression.Constant(value.Substring(1, value.Length - 2)));
                    //arguments.Add(value.Substring(1, value.Length - 2));
                }
                else {
                    arguments.Add(ctx.CompiledGetter(value));
                    //arguments.Add(ctx.GetValue(value));
                }
            }

            return arguments;
        }

        private static IDictionary<string, Expression> ParseOptions(CompileContext ctx, IEnumerable<string> tokens)
        {
            var options = new Dictionary<string, Expression>();

            foreach (var token in tokens) {
                if (token.Contains("=")) {
                    var split = token.Split(new[] { '=' }, 2);
                    var key = split[0];
                    var val = split[1];

                    if (val[0] == '"' || val[0] == '\'') {
                        options[key] = Expression.Constant(val.Substring(1, val.Length - 2));
                        //options[key] = val.Substring(1, val.Length - 2);
                    }
                    else {
                        options[key] = ctx.CompiledGetter(val);
                        //options[key] = ctx.GetValue(val);
                    }
                }
            }

            return options;
        }
    }
}
