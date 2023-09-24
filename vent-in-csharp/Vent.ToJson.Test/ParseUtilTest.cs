using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class ParseUtilTest
    {
        [TestMethod]
        public void EmptyTypeParseTest()
        {
            Assert.IsNull(ParseUtil.ParseGenericArgs(null).node);
            Assert.IsNull(ParseUtil.ParseGenericArgs("").node);
            Assert.IsNull(ParseUtil.ParseGenericArgs("  ").node);
            Assert.IsNull(ParseUtil.ParseGenericArgs(" < > ").node);
            Assert.IsNull(ParseUtil.ParseGenericArgs("<>").node);
        }

        [TestMethod]
        public void SingleTypeParseTest()
        {
            Assert.IsTrue(ParseUtil.ParseGenericArgs("  < type >").node[0].MainTypeName == "type");
            Assert.IsTrue(ParseUtil.ParseGenericArgs("<type>").node[0].MainTypeName == "type");
        }

        [TestMethod]
        public void MultipleTypeParseTest()
        {
            var result = ParseUtil.ParseGenericArgs("  < type1,type2 , type3, type4,type5>");

            Assert.IsNotNull(result);
            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(result.node[i].MainTypeName == "type" + (i+1));
            }
        }

        [TestMethod]
        public void SingleCompoundTypeParseTest()
        {
            var (node, currentIndex) = ParseUtil.ParseGenericArgs("<type<subtype>>");
            Assert.IsTrue(node[0].MainTypeName == "type");
            Assert.IsTrue(node[0].GenericTypes[0].MainTypeName == "subtype");
        }

        [TestMethod]
        public void MultipleCompoundTypeParseTest()
        {
            var (node, currentIndex) = ParseUtil.ParseGenericArgs("<type1<subtype1>, type2 < subtype2 >, type3 <  subtype3>>");

            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(node[i].MainTypeName == "type" + (i + 1));
                Assert.IsTrue(node[i].GenericTypes[0].MainTypeName == "subtype" + (i + 1));
            }
        }

        [TestMethod]
        public void SingleComplexCompoundTypeParseTest()
        {
            var (node, currentIndex) = ParseUtil.ParseGenericArgs("<type<subtype1, subtype2>>");
            Assert.IsTrue(node[0].MainTypeName == "type");
            Assert.IsTrue(node[0].GenericTypes[0].MainTypeName == "subtype1");
            Assert.IsTrue(node[0].GenericTypes[1].MainTypeName == "subtype2");
        }
        
        [TestMethod]
        public void MultipleComplexCompoundTypeParseTest()
        {
            var (node, currentIndex) = ParseUtil.ParseGenericArgs("<t1<s1, s2>, t2 < s1, s2 >>");

            for (var i = 0; i < 2; i++)
            {
                Assert.IsTrue(node[i].MainTypeName == "t" + (i + 1));
                for (var j = 0; j < 2; j++)
                {
                    Assert.IsTrue(node[i].GenericTypes[j].MainTypeName == "s" + (j + 1));
                }
            }
        }
    }
}
