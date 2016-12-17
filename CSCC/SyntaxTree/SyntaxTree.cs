using CSCC.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCC.SyntaxTree
{
    public class Atom
    {
        private readonly string name;

        private Atom(string v)
        {
            this.name = v;
        }

        public string Name { get { return name; } }

        public override bool Equals(object obj)
        {
            return (obj as Atom)?.name == name;
        }
        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
        public override string ToString()
        {
            return name;
        }

        public static PushParser<char,Atom> PParser
        {
            get
            {
                return (from s in PushParsers.Char(c => !new[] { '.', '(', ')' }.Contains(c)).Some()
                        select string.Concat(s).Trim())
                       .Or(from _1 in PushParsers.Char('"')
                           from v in PushParsers.Char(c => c != '"' && c !='\\')
                                     .Or(from _3 in PushParsers.Char('\\')
                                         from c in PushParsers.Char('"')
                                         select c).Many()
                           from _2 in PushParsers.Char('"')
                           select string.Concat(v))
                        .Many()
                        .Select(x => new Atom(string.Concat(x)))
                        .Distinct();
            }
        }
    }

    public class SimpleTerm
    {
        private readonly Atom head;
        private readonly List<Atom> args;

        public SimpleTerm(Atom h, IEnumerable<Atom> args)
        {
            this.head = h;
            this.args = args.ToList();
        }
        public override bool Equals(object obj)
        {
            var st = obj as SimpleTerm;
            return st != null && st.head == head &&
                Enumerable.SequenceEqual(st.args, args);
        }
        public override int GetHashCode()
        {
            return new { head, args }.GetHashCode();
        }
        public override string ToString()
        {
            return string.Join(".", new[] { head.ToString() }.Concat(args.Select(a => a.ToString())).ToArray(), 0, args.Count + 1);
        }

        public Atom Head { get { return head; } }
        public IEnumerable<Atom> Args { get { return args; } }

        public static PushParser<char, SimpleTerm> PParser
        {
            get {
                return (from r in Atom.PParser.SepBy1(PushParsers.Char('.'))
                        select new SimpleTerm(r.First(), r.Skip(1))).Distinct();
            }
        }
    }

    public class RuleTerm
    {
        private readonly SimpleTerm flatPart;
        private readonly List<SimpleTerm> inBrackets;

        public SimpleTerm FlatPart { get { return flatPart; } }
        public IEnumerable<SimpleTerm> InBrackets { get { return inBrackets; } }

        public RuleTerm(SimpleTerm flat, IEnumerable<SimpleTerm> inBrackets)
        {
            var head = flat.Head;
            var args = flat.Args.ToList();
            var inbs = new List<SimpleTerm>();
            foreach(var st in inBrackets)
            {
                if (!st.Args.Any() && !inbs.Any())
                    args.Add(st.Head);
                else
                    inbs.Add(st);
            }
            flatPart = new SimpleTerm(head, args);
            this.inBrackets = inbs;
        }
        public override bool Equals(object obj)
        {
            var rt = obj as RuleTerm;
            return rt != null && rt.flatPart == flatPart &&
                Enumerable.SequenceEqual(rt.inBrackets, inBrackets);
        }
        public override int GetHashCode()
        {
            return new { flatPart, InBrackets }.GetHashCode();
        }
        public override string ToString()
        {
            var ap = new StringBuilder();
            ap.Append(flatPart);
            foreach(var inb in inBrackets)
            {
                ap.Append(".(");
                ap.Append(inb.ToString());
                ap.Append(")");
            }
            return ap.ToString();
        }

        public static PushParser<char, RuleTerm> PParser
        {
            get
            {
                return (from flat in SimpleTerm.PParser
                       from inBrackets in (
                                           from _1 in PushParsers.Char('.')
                                           from inBracket in (from _2 in PushParsers.Char('(')
                                                    from inBracket1 in SimpleTerm.PParser
                                                    from _3 in PushParsers.Char(')')
                                                    select inBracket1).Or(
                                                    from atom in Atom.PParser
                                                    select new SimpleTerm(atom,Enumerable.Empty<Atom>())
                                                    )
                                           select inBracket).Many()
                       select new RuleTerm(flat, inBrackets)).Distinct();
            }
        }
    }

    public class Rule
    {
        private readonly SimpleTerm declare;
        private readonly RuleTerm body;

        public Rule(SimpleTerm st, RuleTerm rt)
        {
            this.declare = st;
            this.body = rt;
        }
        public override bool Equals(object obj)
        {
            var r = obj as Rule;
            return r != null && r.declare == declare && r.body == body;
        }
        public override int GetHashCode()
        {
            return new { declare, body }.GetHashCode();
        }
        public override string ToString()
        {
            return declare.ToString() + "->" + body.ToString();
        }

        public static PushParser<char, Rule> PParser
        {
            get
            {
                return (from st in SimpleTerm.PParser
                       from _1 in (from _2 in PushParsers.Char('-')
                                   from _3 in PushParsers.Char('>')
                                   select 0)
                       from rt in RuleTerm.PParser
                       select new Rule(st, rt)).Distinct();
            }
        }
    }
}
