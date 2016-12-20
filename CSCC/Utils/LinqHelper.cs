using System;
using System.Collections.Generic;

namespace CSCC.Utils
{
    public static class LinqHelper
    {
        public static IEnumerable<R> ZipEqualCount<T1, T2, R>(this IEnumerable<T1> f, IEnumerable<T2> s, Func<T1,T2,R> r)
        {
            if (f == null) throw new ArgumentNullException("f");
            if (s == null) throw new ArgumentNullException("s");
            if (r == null) throw new ArgumentNullException("r");

            using (var e1 = f.GetEnumerator())
            using (var e2 = s.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    if (e2.MoveNext())
                        yield return r(e1.Current, e2.Current);
                    else throw new InvalidOperationException("f have more elements");
                }
                if (e2.MoveNext())
                    throw new InvalidOperationException("s have more elements");
            }
        }
    }
}
