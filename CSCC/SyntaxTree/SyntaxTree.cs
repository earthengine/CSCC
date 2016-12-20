using CSCC.Parser;
using CSCC.Utils;
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

        public static Parser<char,Atom> PParser
        {
            get
            {
                return (from s in Parsers.Char(c => !new[] { '.', '(', ')' }.Contains(c)).Some()
                        select string.Concat(s).Trim())
                       .Or(from _1 in Parsers.Char('"')
                           from v in Parsers.Char(c => c != '"' && c !='\\')
                                     .Or(from _3 in Parsers.Char('\\')
                                         from c in Parsers.Char('"')
                                         select c).Many()
                           from _2 in Parsers.Char('"')
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
            return HashHelper.Base
                .HashObject(head)
                .HashEnumerable(args);
        }
        public override string ToString()
        {
            return string.Join(".", new[] { head.ToString() }.Concat(args.Select(a => a.ToString())).ToArray(), 0, args.Count + 1);
        }

        public Atom Head { get { return head; } }
        public IEnumerable<Atom> Args { get { return args; } }

        public static Parser<char, SimpleTerm> PParser
        {
            get {
                return (from r in Atom.PParser.SepBy1(Parsers.Char('.'))
                        select new SimpleTerm(r.First(), r.Skip(1))).Distinct();
            }
        }
    }

    public class RuleTerm
    {
        private readonly Atom head;
        private readonly List<SimpleTerm> terms;

        public Atom Head { get { return head; } }
        public IEnumerable<SimpleTerm> Terms { get { return terms; } }

        public RuleTerm(Atom h, IEnumerable<SimpleTerm> t)
        {
            this.head = h;
            this.terms = t.ToList();
        }
        public override bool Equals(object obj)
        {
            var rt = obj as RuleTerm;
            return rt != null && rt.head == head &&
                Enumerable.SequenceEqual(rt.terms, terms);
        }
        public override int GetHashCode()
        {
            return HashHelper.Base
                .HashObject(head)
                .HashEnumerable(terms);
        }
        public override string ToString()
        {
            var ap = new StringBuilder();
            ap.Append(head);
            foreach(var inb in terms)
            {
                ap.Append(".(");
                ap.Append(inb.ToString());
                ap.Append(")");
            }
            return ap.ToString();
        }

        public static Parser<char, RuleTerm> PParser
        {
            get
            {
                return (from flat in Atom.PParser
                       from inBrackets in (
                                           from _1 in Parsers.Char('.')
                                           from inBracket in (from _2 in Parsers.Char('(')
                                                    from inBracket1 in SimpleTerm.PParser
                                                    from _3 in Parsers.Char(')')
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

        public string RuleName { get { return declare.Head.Name; } }
        public IEnumerable<string> Args { get { return declare.Args.Select(a => a.Name); } }
        public RuleTerm Body { get { return body; } }

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
            return HashHelper.Base
                .HashObject(declare)
                .HashObject(body);
        }
        public override string ToString()
        {
            return declare.ToString() + "->" + body.ToString();
        }

        public static Parser<char, Rule> PParser
        {
            get
            {
                return (from st in SimpleTerm.PParser
                       from _1 in (from _2 in Parsers.Char('-')
                                   from _3 in Parsers.Char('>')
                                   select 0)
                       from rt in RuleTerm.PParser
                       select new Rule(st, rt)).Distinct();
            }
        }
    }
}
