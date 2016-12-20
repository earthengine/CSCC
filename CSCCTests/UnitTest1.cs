using CSCC.Compile;
using CSCC.Parser;
using CSCC.SyntaxTree;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSCCTests
{
    [TestFixture]
    public class UnitTest1
    {
        private Rule ParseRule(string s)
        {
            return Parsers.Parse(Rule.PParser, s).First();
        }

        private void AddRule(string s, List<Rule> rules, Dictionary<string, ICCType> ruleTypes, List<Tuple<string,ICCType>> defs)
        {
            var rule = ParseRule(s);
            var ruletype = CCTypes.TypeAnalysis(rule, ruleTypes, defs);
            if(!ruleTypes.ContainsKey(rule.RuleName))
                ruleTypes.Add(rule.RuleName, ruletype);
            else
            {
                var oldtype = ruleTypes[rule.RuleName];
                var st = new Stack<TypeEquation>();
                st.Push(new TypeEquation(oldtype, ruletype));
                CCTypes.Unify(st, defs);
                ruleTypes[rule.RuleName] = ruletype.Substitute(defs);
            }
            rules.Add(rule);
        }

        [Test]
        public void TestRuleSet()
        {
            var rules = new List<Rule>();
            var defs = new List<Tuple<string, ICCType>>();
            var ruleTypes = new Dictionary<string, ICCType>();

            AddRule("Zero.z.s -> z", rules, ruleTypes, defs);
            AddRule("S.n.z.s->s.n", rules, ruleTypes, defs);
            AddRule("AddCBV.x.y.r  -> x.(r.y).(AddCBV'.y.r)", rules, ruleTypes, defs);
            AddRule("AddCBV'.y.r.x' -> AddCBV.x'.(S.y).r", rules, ruleTypes, defs);
        }

        [Test]
        public void TestCCType()
        {
            var boolType = CCTypes.Tran(CCTypes.Tran(CCTypes.Bottom(), CCTypes.Bottom()),CCTypes.Bottom());
            var NatType = CCTypes.Redefine("T", 
                CCTypes.Tran(CCTypes.Bottom(), CCTypes.Tran(CCTypes.Tran(CCTypes.Var("T"),CCTypes.Bottom()),CCTypes.Bottom())));
            var ListType = CCTypes.Redefine("T", CCTypes.Tran(CCTypes.Bottom(), 
                CCTypes.Tran(CCTypes.Tran(CCTypes.Var("A"), CCTypes.Tran(CCTypes.Var("T"),CCTypes.Bottom())),CCTypes.Bottom())));

            var bool1 = CCTypes.Tran(CCTypes.Tran(CCTypes.Bottom(), CCTypes.Bottom()), CCTypes.Bottom());
            Assert.That(boolType, Is.EqualTo(bool1));
            var l1 = ListType.Substitute("A", NatType);
            Assert.That(l1, Is.Not.EqualTo(ListType));
            var l2 = ListType.Substitute("List", NatType);
            Assert.That(l2, Is.EqualTo(ListType));

            var defs = new List<Tuple<string, ICCType>>();
            var equations = new Stack<TypeEquation>();
            equations.Push(new TypeEquation(CCTypes.Tran(CCTypes.Var("A"), CCTypes.Bottom()),
                CCTypes.Tran(CCTypes.Var("B"), CCTypes.Tran(CCTypes.Var("C"), CCTypes.Bottom()))));
            Assert.That(CCTypes.Unify(equations, defs), Is.False);

            equations.Clear();
            defs.Clear();
            equations.Push(new TypeEquation(CCTypes.Var("t0"),
                CCTypes.Continuation(CCTypes.Var("t1"), CCTypes.Var("t2"))));                
            equations.Push(new TypeEquation(CCTypes.Var("t1"), CCTypes.Bottom()));
            equations.Push(new TypeEquation(CCTypes.Var("t3"),
                CCTypes.Continuation(CCTypes.Var("t4"), CCTypes.Var("t5"), CCTypes.Var("t6"))));
            equations.Push(new TypeEquation(CCTypes.Var("t6"),
                CCTypes.Continuation(CCTypes.Var("t4"))));
            equations.Push(new TypeEquation(CCTypes.Var("t7"),
                CCTypes.Continuation(CCTypes.Var("t8"), CCTypes.Var("t9"), CCTypes.Var("t10"))));
            equations.Push(new TypeEquation(CCTypes.Var("t8"),
                CCTypes.Continuation(CCTypes.Var("t11"), CCTypes.Var("t12"))));
            equations.Push(new TypeEquation(
                CCTypes.Tran(CCTypes.Var("t9"), CCTypes.Tran(CCTypes.Var("t10"), CCTypes.Var("t12"))),
                CCTypes.Continuation(CCTypes.Var("t14"), CCTypes.Var("t15"), CCTypes.Var("t16"))));

            CCTypes.Unify(equations, defs);
        }
    }
}
