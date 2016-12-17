using System;
using System.Linq;
using System.Collections.Generic;

namespace CSCC.Parser
{
    public class StringPos
    {
        private readonly string s;
        private readonly int pos;
        private StringPos(string s, int pos) { this.s = s;this.pos = pos; }
        public static StringPos Create(string s)
        {
            return new StringPos(s, 0);
        }
        public int Count { get { return s.Length - pos; } }

        public static Parser<StringPos, char> AnyChar()
        {
            return input =>
            {
                if (input.s.Length > input.pos)
                    return new List<Result<StringPos, char>> {
                        Parsers.ResultOf(input.s[input.pos], new StringPos(input.s, input.pos + 1)) };
                else return new List<Result<StringPos, char>>();
            };
        }
    }

    public class Result<T, V>
    {
        public readonly T Rest;
        public readonly V Value;
        private Result(T t, V v) { Value = v; Rest = t; }
        internal static Result<T,V> Create(V v, T t) { return new Result<T, V>(t,v); }
    }

    public delegate List<Result<T, V>> Parser<T,V>(T input);

    public static class Parsers
    {

        public static V ParseExact<V>(this Parser<StringPos, V> parser, StringPos input)
        {
            return (from r in parser(input)
                    where r.Rest.Count == 0
                    select r.Value).FirstOrDefault();
        }

        public static Result<T,V> ResultOf<T,V>(V v, T t) { return Result<T, V>.Create(v, t); }
        public static Parser<T, V> OR<T, V>(this Parser<T, V> p1, Parser<T, V> p2)
        {
            return input => p1(input).Concat(p2(input)).ToList();
        }

        public static Parser<T, V> Where<T, V>(this Parser<T, V> parser, Func<V, bool> pred)
        {
            return input => (from res in parser(input)
                            where pred(res.Value)
                            select res).ToList();
        }
        public static Parser<T, R> Select<T, V, R>(this Parser<T, V> p, Func<V, R> f)
        {
            return input => (from t in p(input)
                            select ResultOf(f(t.Value), t.Rest)).ToList();
        }
        public static Parser<T, R> SelectMany<T, V1, V2, R>(this Parser<T, V1> p1, Func<V1, Parser<T, V2>> f1, Func<V1, V2, R> f2)
        {
            return input => (from t1 in p1(input)
                             from t2 in f1(t1.Value)(t1.Rest)
                             select ResultOf(f2(t1.Value, t2.Value), t2.Rest)).ToList();
        }
        public static Parser<T, V> Succeed<T, V>(V v)
        {
            return input => new List<Result<T,V>> { Result<T, V>.Create(v, input) };
        }
        public static Parser<T, List<V>> Many<T, V>(this Parser<T, V> parser)
        {
            return Some(parser).OR(Succeed<T, List<V>>(new List<V>()));
        }
        public static Parser<T, List<V>> Some<T, V>(this Parser<T, V> parser)
        {
            return from x in parser
                   from xs in parser.Many()
                   select ((new[] { x }).Concat(xs)).ToList();
        }
        public static Parser<T, IEnumerable<V1>> SepBy1<T, V1, V2>(this Parser<T, V1> main, Parser<T, V2> sep)
        {
            return (from t1 in main
                    from ts in (from _ in sep
                                from t2 in main
                                select t2).Many()
                    select new[] { t1 }.Concat(ts));
        }
        public static Parser<T, IEnumerable<V1>> SepBy<T,V1,V2>(this Parser<T,V1> main, Parser<T,V2> sep)
        {
            return SepBy1(main,sep).OR(Succeed<T,IEnumerable<V1>>(Enumerable.Empty<V1>()));
        }
        
        public static Parser<StringPos, char> Char(Predicate<char> pred)
        {
            return from c in StringPos.AnyChar() where pred(c) select c;
        }
        public static Parser<StringPos, char> Char(char ch)
        {
            return Char(c => c == ch);
        }
    }

}
