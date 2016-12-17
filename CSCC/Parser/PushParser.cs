using System;
using System.Collections.Generic;
using System.Linq;

namespace CSCC.Parser
{
    public delegate Tuple<IEnumerable<R>,Func<T, PushParser<T,R>>> PushParser<T, R>();

    public static class PushParsers
    {
        public static IEnumerable<R> Parse<T,R>(PushParser<T,R> parser, IEnumerable<T> items)
        {
            var tr = parser();
            foreach (var item in items)
            {
                if (tr.Item2 == null) return Enumerable.Empty<R>();
                else tr = tr.Item2(item)();
            }
            return tr.Item1;
        }

        public static PushParser<T,R2> Select<T,R1,R2>(this PushParser<T,R1> parser, Func<R1,R2> f)
        {
            var tr = parser();
            return () => Tuple.Create(tr.Item1.Select(f), 
                tr.Item2 == null ? null : (Func<T,PushParser<T,R2>>)(t => tr.Item2(t).Select(f)));
        }
        public static PushParser<T,R> Of<T,R>(R result)
        {
            return () => Tuple.Create<IEnumerable<R>,Func<T,PushParser<T,R>>>(new[] { result }, null);
        }
        public static PushParser<T,R> Empty<T, R>()
        {
            return () => Tuple.Create<IEnumerable<R>, Func<T, PushParser<T, R>>>(Enumerable.Empty<R>(), null);
        }
        public static PushParser<T, V> Where<T, V>(this PushParser<T, V> parser, Func<V, bool> pred)
        {
            var tr = parser();
            var result = tr.Item1.Where(pred);
            Func<T,PushParser<T,V>> f = t => tr.Item2(t).Where(pred);
            return () => Tuple.Create(result, f);
        }
        public static PushParser<T,R> Or<T, R>(this PushParser<T,R> parser1, PushParser<T, R> parser2)
        {
            var p1 = parser1();
            var p2 = parser2();
            var r = p1.Item1.Concat(p2.Item1);
            var f = p1.Item2 == null ? p2.Item2 : (p2.Item2 == null ? p1.Item2 : t => p1.Item2(t).Or(p2.Item2(t)));
            return () => Tuple.Create(r, f);
        }
        public static PushParser<T, R> OrElse<T, R>(this PushParser<T, R> parser1, PushParser<T, R> parser2)
        {
            var p1 = parser1();
            var p2 = parser2();
            var r = p1.Item1.Concat(p2.Item1);
            var f = p1.Item2 == null ? p2.Item2 : (p2.Item2 == null ? p1.Item2 : t => p1.Item2(t).Or(p2.Item2(t)));
            return () => Tuple.Create(r, f);
        }
        public static PushParser<T,R> Unwrap<T,R>(this PushParser<T,PushParser<T,R>> wrapped)
        {
            var p = wrapped();
            var f = p.Item2 == null ? null : (Func<T, PushParser<T, R>>)(t => Unwrap(p.Item2(t)));
            return p.Item1.Aggregate((PushParser<T,R>)(() => Tuple.Create(Enumerable.Empty<R>(), f)), Or);
        }
        public static PushParser<T,R> SelectMany<T,R1,R2,R>(this PushParser<T,R1> parser1, Func<R1,PushParser<T,R2>> f1, Func<R1,R2,R> f2)
        {
            return parser1.Select(r1 => f1(r1).Select(r2 => f2(r1, r2))).Unwrap();
        }
        public static PushParser<T, List<V>> Many<T, V>(this PushParser<T, V> parser)
        {
            var p1 = Some(parser)();
            return () => Tuple.Create(p1.Item1.Concat(new[] { new List<V>() }), p1.Item2);
        }
        public static PushParser<T, List<V>> Some<T, V>(this PushParser<T, V> parser)
        {
            return from x in parser
                   from xs in parser.Many()
                   select ((new[] { x }).Concat(xs)).ToList();
        }
        public static PushParser<T, IEnumerable<V1>> SepBy1<T, V1, V2>(this PushParser<T, V1> main, PushParser<T, V2> sep)
        {
            return (from t1 in main
                    from ts in (from _ in sep
                                from t2 in main
                                select t2).Many()
                    select new[] { t1 }.Concat(ts));
        }
        public static PushParser<T, IEnumerable<V1>> SepBy<T, V1, V2>(this PushParser<T, V1> main, PushParser<T, V2> sep)
        {
            return SepBy1(main, sep).Or(Of<T, IEnumerable<V1>>(Enumerable.Empty<V1>()));
        }
        public static PushParser<char, char> Char(Predicate<char> pred)
        {
            return () => Tuple.Create<IEnumerable<char>,Func<char,PushParser<char,char>>>
                (Enumerable.Empty<char>(), c => pred(c) ? Of<char,char>(c) : Empty<char,char>());
        }
        public static PushParser<char, char> Char(char ch)
        {
            return Char(c => c == ch);
        }
        public static PushParser<T,T> Item<T>()
        {
            return () => Tuple.Create<IEnumerable<T>,Func<T,PushParser<T,T>>>(Enumerable.Empty<T>(), t => Of<T,T>(t));
        }
        public static PushParser<T,R> Distinct<T, R>(this PushParser<T,R> parser)
        {
            var p = parser();
            return () => Tuple.Create(p.Item1.Distinct(), 
                p.Item2==null ? (Func<T, PushParser<T, R>>)null : t => p.Item2(t).Distinct());

        }
    }
}
