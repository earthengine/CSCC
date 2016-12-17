using CSCC.Compile;
using CSCC.Parser;
using CSCC.SyntaxTree;
using NUnit.Framework;
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

        [Test]
        public void TestMethod1()
        {
            var rules = new List<Rule>();

            rules.Add(ParseRule("Zero.z.s -> z"));
            rules.Add(ParseRule("S.n.z.s -> s.n"));

            rules.Add(ParseRule("AddCBV.x.y.r  -> x.(r.y).(AddCBV'.y.r)"));
            rules.Add(ParseRule("AddCBV'.y.r.x' -> AddCBV.x'.(S.y).r"));
        }

        [Test]
        public void TestCCType()
        {
            var boolType = CCTypes.Continuation(CCTypes.Continuation(), CCTypes.Continuation());
            var NatType = CCTypes.Redefine("Nat", CCTypes.Continuation(), CCTypes.Continuation(CCTypes.Atomic("Nat")));
            var ListType = CCTypes.Redefine("List", CCTypes.Continuation(), CCTypes.Continuation(CCTypes.Atomic("A"), CCTypes.Atomic("List")));
            var BinTreeType = CCTypes.Redefine("BinTree", CCTypes.Continuation(), 
                CCTypes.Continuation(CCTypes.Atomic("A"), CCTypes.Atomic("BinTree"), CCTypes.Atomic("BinTree")));

            var bool1 = CCTypes.Continuation(CCTypes.Continuation(), CCTypes.Continuation());
            Assert.That(boolType, Is.EqualTo(bool1));
        }
    }
}
