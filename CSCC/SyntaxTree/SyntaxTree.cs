using CSCC.Parser;
using System.Collections.Generic;
using System.Linq;

namespace CSCC.SyntaxTree
{
    public class Atom
    {
        private string name;

        private Atom(string v)
        {
            this.name = v;
        }

        public string Name { get { return name; } }

        public static PushParser<char,Atom> PParser
        {
            get
            {
                return (from s in PushParsers.Char(c => !new[] { '.', '(', ')' }.Contains(c)).Some()
                        select string.Concat(s).Trim())
                       .Or(from _1 in PushParsers.Char('"')
                           from v in PushParsers.Char(c => c != '"').Many()
                           from _2 in PushParsers.Char('"')
                           select string.Concat(v))
                       .Or(from _1 in PushParsers.Char('\'')
                           from v in PushParsers.Char(c => c != '\'').Many()
                           from _2 in PushParsers.Char('\'')
                           select string.Concat(v))
                        .Many()
                        .Select(x => new Atom(string.Concat(x)));
            }
        }
        
        public static List<Result<StringPos, Atom>> Parse(StringPos input){
            return (from s in Parsers.Char(c => !new[] { '.', '(', ')', '"', '\'' }.Contains(c)).Some()
                    select string.Concat(s).Trim())
                    .OR(from _1 in Parsers.Char('"')
                        from v in Parsers.Char(c => c!='"').Many()
                        from _2 in Parsers.Char('"')
                        select string.Concat(v))
                    .OR(from _1 in Parsers.Char('\'')
                        from v in Parsers.Char(c => c!='\'').Many()
                        from _2 in Parsers.Char('\'')
                        select string.Concat(v))
                    .Many()
                    .Select(x => new Atom(string.Concat(x)))(input);
        }
        public static Parser<StringPos, Atom> TheParser = Parse;
    }

    public class SimpleTerm
    {
        private Atom head;
        private List<Atom> args;

        public SimpleTerm(Atom h, IEnumerable<Atom> args)
        {
            this.head = h;
            this.args = args.ToList();
        }

        public Atom Head { get { return head; } }
        public IEnumerable<Atom> Args { get { return args; } }

        public static PushParser<char, SimpleTerm> PParser
        {
            get {
                return (from r in Atom.PParser.SepBy1(PushParsers.Char('.'))
                        select new SimpleTerm(r.First(), r.Skip(1)));
            }
        }

        public static List<Result<StringPos, SimpleTerm>> Parse(StringPos input)
        {
            return (from r in Atom.TheParser.SepBy1(Parsers.Char('.'))                    
                    select new SimpleTerm(r.First(), r.Skip(1)))(input);
        }
        public static Parser<StringPos, SimpleTerm> TheParser = Parse;
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

        public static PushParser<char, RuleTerm> PParser
        {
            get
            {
                return from flat in SimpleTerm.PParser
                       from inBrackets in (
                                           from _1 in PushParsers.Char('.')
                                           from _2 in PushParsers.Char('(')
                                           from inBracket in SimpleTerm.PParser
                                           from _3 in PushParsers.Char(')')
                                           select inBracket).Many()
                       select new RuleTerm(flat, inBrackets);
            }
        }

        public static List<Result<StringPos, RuleTerm>> Parse(StringPos input)
        {
            return (from flat in SimpleTerm.TheParser
                    from inBrackets in (
                                       from _1 in Parsers.Char('.')
                                       from _2 in Parsers.Char('(')
                                       from inBracket in SimpleTerm.TheParser
                                       from _3 in Parsers.Char(')')
                                       select inBracket).Many()
                    select new RuleTerm(flat, inBrackets))(input);
        }
        public static Parser<StringPos, RuleTerm> TheParser = Parse;
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

        public static PushParser<char, Rule> PParser
        {
            get
            {
                return from st in SimpleTerm.PParser
                       from _1 in (from _2 in PushParsers.Char('-')
                                   from _3 in PushParsers.Char('>')
                                   select 0)
                       from rt in RuleTerm.PParser
                       select new Rule(st, rt);
            }
        }

        public static List<Result<StringPos, Rule>> Parse(StringPos input)
        {
            return (from st in SimpleTerm.TheParser
                    from _1 in (from _2 in Parsers.Char('-')
                                from _3 in Parsers.Char('>')
                                select 0)
                    from rt in RuleTerm.TheParser
                    select new Rule(st, rt))(input);
        }
        public static Parser<StringPos, Rule> TheParser = Parse;
    }
}
