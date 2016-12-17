using CSCC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCC.Compile
{
    public interface ICCType
    {
        string Name { get; }
    }
    public static class CCTypes
    {
        private class AtomicType : ICCType
        {
            public AtomicType(string name)
            {
                Name = name;
            }
            public override bool Equals(object obj)
            {
                return (obj as AtomicType)?.Name == Name;
            }
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
            public string Name { get; }
        }
        public static ICCType Atomic(string name)
        {
            return new AtomicType(name);
        }
        private class ContinuationType :ICCType
        {
            private readonly List<ICCType> arguments;

            public ContinuationType(IEnumerable<ICCType> args)
            {
                arguments = args.ToList();
            }

            public string Name
            {
                get
                {
                    var sb = new StringBuilder();
                    sb.Append("~(");
                    sb.Append(string.Join(",", arguments.Select(a => a.Name).ToArray()));
                    sb.Append(")");
                    return sb.ToString();
                }
            }
            public override bool Equals(object obj)
            {
                var ct = obj as ContinuationType;
                if (ct == null) return false;
                return Enumerable.SequenceEqual(ct.arguments, arguments);
            }
            public override int GetHashCode()
            {
                return HashHelper.Base
                    .HashEnumerable(arguments);
            }
        }
        public static ICCType Continuation(params ICCType[] args)
        {
            return new ContinuationType(args);
        }
        private class RedefinedType : ICCType
        {
            private readonly ContinuationType innerType;
            public RedefinedType(string name,ContinuationType t)
            {
                innerType = t;
                Name = name;
            }
            public string Name { get; }
            public override bool Equals(object obj)
            {
                var rt = obj as RedefinedType;
                if (rt != null) return false;
                return rt.Name == Name && rt.innerType == innerType;
            }
            public override int GetHashCode()
            {
                return HashHelper.Base
                    .HashObject(Name)
                    .HashObject(innerType);
            }
        }
        public static ICCType Redefine(string name, params ICCType[] definition)
        {
            return new RedefinedType(name, new ContinuationType(definition));
        }



    }
}
