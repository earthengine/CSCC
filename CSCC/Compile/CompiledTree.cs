using CSCC.SyntaxTree;
using CSCC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCC.Compile
{
    public interface ICCType
    {
        T Accept<T>(ICCTypeVisitor<T> acc);
        void Accept(ICCTypeVisitor acc);
    }
    public interface ICCTypeVisitor
    {
        void VisitBottom();
        void VisitVar(string name);
        void VisitTran(ICCType src, ICCType dst);
        void VisitRedefined(string name, ICCType inner);
    }
    public interface ICCTypeVisitor<T>
    {
        T VisitBottom();
        T VisitVar(string name);
        T VisitTran(ICCType src, ICCType dst);
        T VisitRedefined(string name, ICCType inner);
    }

    public class TypeEquation
    {
        public ICCType Lhs { get; }
        public ICCType Rhs { get; }
        public TypeEquation(ICCType l, ICCType r)
        {
            Lhs = l;
            Rhs = r;
        }
        public override string ToString()
        {
            return $"{Lhs} = {Rhs}";
        }
    }

    public static class CCTypes
    {
        private class BottomType : ICCType
        {
            public void Accept(ICCTypeVisitor acc)
            {
                acc.VisitBottom();
            }

            public T Accept<T>(ICCTypeVisitor<T> acc)
            {
                return acc.VisitBottom();
            }
            public override bool Equals(object obj)
            {
                return obj is BottomType;
            }
            public override int GetHashCode()
            {
                return HashHelper.Base
                    .HashObject("BottomType");
            }
            public override string ToString()
            {
                return "⊥";
            }
        }
        public static ICCType Bottom()
        {
            return new BottomType();
        }
        private class VarType : ICCType
        {
            public VarType(string name)
            {
                Name = name;
            }
            public override bool Equals(object obj)
            {
                return (obj as VarType)?.Name == Name;
            }
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public void Accept(ICCTypeVisitor acc)
            {
                acc.VisitVar(Name);
            }
            public T Accept<T>(ICCTypeVisitor<T> acc)
            {
                return acc.VisitVar(Name);
            }
            public override string ToString()
            {
                return Name;
            }

            public string Name { get; }
        }
        public static ICCType Var(string name)
        {
            return new VarType(name);
        }
        private class TranType :ICCType
        {
            private readonly ICCType src;
            private readonly ICCType dst;

            public TranType(ICCType s, ICCType d)
            {
                src = s;
                dst = d;
            }

            public void Accept(ICCTypeVisitor acc)
            {
                acc.VisitTran(src, dst);
            }
            public T Accept<T>(ICCTypeVisitor<T> acc)
            {
                return acc.VisitTran(src,dst);
            }
            public override bool Equals(object obj)
            {
                var ct = obj as TranType;
                if (ct == null) return false;
                return src.Equals(ct.src) && dst.Equals(ct.dst);
            }
            public override int GetHashCode()
            {
                return HashHelper.Base
                    .HashObject(src)
                    .HashObject(dst);
            }
            public override string ToString()
            {
                return $"({src}→{dst})";
            }
        }
        public static ICCType Tran(ICCType src, ICCType dst)
        {
            return new TranType(src, dst);
        }
        public static ICCType Continuation(params ICCType[] ps)
        {
            if (!ps.Any()) return Bottom();
            return Tran(ps.First(), Continuation(ps.Skip(1).ToArray()));
        }
        private class RedefinedType : ICCType
        {
            private readonly ICCType innerType;
            public RedefinedType(string name, ICCType inner)
            {
                innerType = inner;
                Name = name;
            }
            public string Name { get; }

            public void Accept(ICCTypeVisitor acc)
            {
                acc.VisitRedefined(Name, innerType);
            }
            public T Accept<T>(ICCTypeVisitor<T> acc)
            {
                return acc.VisitRedefined(Name, innerType);
            }
            public override bool Equals(object obj)
            {
                var rt = obj as RedefinedType;
                if (rt == null) return false;
                return rt.Name == Name && rt.innerType.Equals(innerType);
            }
            public override int GetHashCode()
            {
                return HashHelper.Base
                    .HashObject(Name)
                    .HashObject(innerType);
            }
            public override string ToString()
            {
                return $"µ{Name}.{innerType}";
            }
        }
        public static ICCType Redefine(string name, ICCType inner)
        {
            return new RedefinedType(name, inner);
        }

        private class DelegateTypeVisitor<T> : ICCTypeVisitor<T>
        {
            private readonly Func<T> bottom;
            private readonly Func<string,T> var;
            private readonly Func<ICCType, ICCType, T> tran;
            private readonly Func<string, ICCType, T> redefine;

            public DelegateTypeVisitor(Func<T> b, Func<string,T> v, Func<ICCType, ICCType, T> t, Func<string, ICCType, T> r)
            {
                bottom = b;
                var = v;
                tran = t;
                redefine = r;
            }

            public T VisitBottom()
            {
                return bottom();
            }

            public T VisitRedefined(string name, ICCType inner)
            {
                return redefine(name, inner);
            }

            public T VisitTran(ICCType src, ICCType dst)
            {
                return tran(src, dst);
            }

            public T VisitVar(string name)
            {
                return var(name);
            }
        }
        private class DelegateTypeVisitor : ICCTypeVisitor
        {
            private readonly Action bottom;
            private readonly Action<string> var;
            private readonly Action<ICCType, ICCType> tran;
            private readonly Action<string, ICCType> redefine;

            public DelegateTypeVisitor(Action b, Action<string> v, Action<ICCType, ICCType> t, Action<string, ICCType> r)
            {
                bottom = b;
                var = v;
                tran = t;
                redefine = r;
            }

            public void VisitBottom()
            {
                bottom();
            }

            public void VisitRedefined(string name, ICCType inner)
            {
                redefine(name, inner);
            }

            public void VisitTran(ICCType src, ICCType dst)
            {
                tran(src, dst);
            }

            public void VisitVar(string name)
            {
                var(name);
            }
        }
        public static ICCType Substitute(this ICCType type, string var, ICCType subsTo)
        {
            return type.Accept(new DelegateTypeVisitor<ICCType>(
                    () => Bottom(),
                    name => name == var ? subsTo : Var(name),
                    (src,dst) => Tran(src.Substitute(var, subsTo),dst.Substitute(var,subsTo)),
                    (name,inner) => name==var ? Redefine(name, inner) : Redefine(name, inner.Substitute(var, subsTo))
                ));
        }
        public static ICCType Substitute(this ICCType type, IEnumerable<Tuple<string, ICCType>> maps)
        {
            var result = type;
            foreach(var map in maps)
            {
                result = result.Substitute(map.Item1, map.Item2);
            }
            return result;
        }
        public static bool Unify(Stack<TypeEquation> equations, List<Tuple<string,ICCType>> defs)
        {
            while (equations.Any())
            {
                var t = equations.Pop();
                string nametoadd = null;
                ICCType typetoadd = null;
                var r = t.Lhs.Accept(new DelegateTypeVisitor<bool>(
                    () => t.Rhs.Accept(new DelegateTypeVisitor<bool>(
                        () => true,
                        name => { nametoadd = name; typetoadd = Bottom(); return true; },
                        (src, dst) => false,
                        (name, inner) => inner.Equals(Bottom()))),
                    name => t.Rhs.Accept(new DelegateTypeVisitor<bool>(
                        () => { nametoadd = name; typetoadd = Bottom(); return true; },
                        name1 => { nametoadd = name; typetoadd = Var(name1); ; return true; },
                        (src, dst) => { nametoadd = name; typetoadd = Tran(src, dst); return true; },
                        (name1, inner) => { nametoadd = name; typetoadd=Redefine(name1, inner); return true; })),
                    (src, dst) => t.Rhs.Accept(new DelegateTypeVisitor<bool>(
                        () => false,
                        name => { nametoadd = name; typetoadd=Tran(src, dst); return true; },
                        (src1, dst1) => { equations.Push(new TypeEquation(src, src1)); equations.Push(new TypeEquation(dst, dst1)); return true; },
                        (name, inner) => { equations.Push(new TypeEquation(Tran(src, dst), inner.Substitute(name, Redefine(name, inner)))); return true; })
                        ),
                    (name, inner) => t.Rhs.Accept(new DelegateTypeVisitor<bool>(
                        () => inner.Equals(Bottom()),
                        name1 => { nametoadd = name; typetoadd = Redefine(name1, inner); return true; },
                        (src, dst) => { equations.Push(new TypeEquation(Tran(src, dst), inner.Substitute(name, Redefine(name, inner)))); return true; },
                        (name1, inner1) => { equations.Push(new TypeEquation(inner.Substitute(name, inner), inner1.Substitute(name1, inner1))); return true; }
                        ))
                    ));
                if (!r) return false;
                if (nametoadd!=null)
                {
                    if (typetoadd.Equals(Var(nametoadd)))
                        continue;
                    else if (defs.Any(def => def.Item1 == nametoadd))
                        equations.Push(new TypeEquation(
                            defs.Where(def => def.Item1 == nametoadd).Select(def => def.Item2).First(),
                            typetoadd));
                    else
                    {
                        var substituted = typetoadd.Substitute(defs);
                        if (substituted.ContainsVar(nametoadd))
                        {
                            var newvar = $"t{varNo++}";
                            defs.Add(Tuple.Create(nametoadd, Redefine(newvar, substituted.Substitute(nametoadd, Var(newvar)))));
                        }
                        else
                            defs.Add(Tuple.Create(nametoadd, substituted));
                    }
                }
            }
            return true;
        }
        private static bool ContainsVar(this ICCType type, string varname)
        {
            return type.Accept(new DelegateTypeVisitor<bool>(
                () => false,
                name => name == varname,
                (src, dst) => src.ContainsVar(varname) || dst.ContainsVar(varname),
                (name, inner) => name == varname ? false : inner.ContainsVar(varname)
                ));
        }

        private static int varNo;

        private static ICCType FindVarOrNew(Dictionary<string, ICCType> argtyps, Dictionary<string, ICCType> ruleTypes, string name)
        {
            ICCType statyp;
            if (argtyps.ContainsKey(name))
                statyp = argtyps[name];
            else if (ruleTypes.ContainsKey(name))
                statyp = ruleTypes[name];
            else
            {
                statyp = Var($"t{varNo++}");
                ruleTypes.Add(name, statyp);
            }
            return statyp;
        }

        public static ICCType TypeAnalysis(this Rule r, 
            Dictionary<string, ICCType> ruleTypes, List<Tuple<string,ICCType>> defs)
        {
            
            var argtyps = new Dictionary<string, ICCType>();
            var ruletype = Bottom();
            foreach (var arg in r.Args.Reverse())
            {
                var atyp = Var($"t{varNo++}");
                argtyps.Add(arg, atyp);
                ruletype = Tran(atyp, ruletype);
            }
            var equations = new Stack<TypeEquation>();
            if (ruleTypes.ContainsKey(r.RuleName))
            {
                equations.Push(new TypeEquation(ruletype, ruleTypes[r.RuleName]));
            }

            var sttypes = new List<ICCType>();
            var headtype = Bottom();
            foreach(var st in r.Body.Terms.Reverse())
            {
                var termtype = Var($"t{varNo++}");
                var sthtype = termtype;
                foreach(var sta in st.Args.Reverse())
                {
                    var statyp = FindVarOrNew(argtyps, ruleTypes, sta.Name);
                    sthtype = Tran(statyp, sthtype);
                }
                var headtyp = FindVarOrNew(argtyps, ruleTypes, st.Head.Name);
                equations.Push(new TypeEquation(headtyp, sthtype));
                headtype = Tran(termtype, headtype);
            }
            var head = FindVarOrNew(argtyps, ruleTypes, r.Body.Head.Name);
            equations.Push(new TypeEquation(head, headtype));

            Unify(equations, defs);
            return ruletype.Substitute(defs);
        }
    }
}
