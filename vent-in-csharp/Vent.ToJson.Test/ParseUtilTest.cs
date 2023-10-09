﻿/// Vent is released under Creative Commons BY-SA see https://creativecommons.org/licenses/by-sa/4.0/
/// (c) Pointlesspun

using Vent.Entities;
using Vent.Registry;
using Vent.ToJson.Test.TestEntities;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class ParseUtilTest
    {
        [TestMethod]
        public void EmptyTypeParseTest()
        {
            Assert.IsNull(VentClassName.ParseGenericArgs(null).node);
            Assert.IsNull(VentClassName.ParseGenericArgs("").node);
            Assert.IsNull(VentClassName.ParseGenericArgs("  ").node);
            Assert.IsNull(VentClassName.ParseGenericArgs(" < > ").node);
            Assert.IsNull(VentClassName.ParseGenericArgs("<>").node);
        }

        [TestMethod]
        public void SingleTypeParseTest()
        {
            Assert.IsTrue(VentClassName.ParseGenericArgs("  < type >").node[0].TypeName == "type");
            Assert.IsTrue(VentClassName.ParseGenericArgs("<type>").node[0].TypeName == "type");
        }

        [TestMethod]
        public void MultipleTypeParseTest()
        {
            var result = VentClassName.ParseGenericArgs("  < type1,type2 , type3, type4,type5>");

            Assert.IsNotNull(result);
            for (var i = 0; i < 5; i++)
            {
                Assert.IsTrue(result.node[i].TypeName == "type" + (i+1));
            }
        }

        [TestMethod]
        public void SingleCompoundTypeParseTest()
        {
            var (node, _) = VentClassName.ParseGenericArgs("<type<subtype>>");
            Assert.IsTrue(node[0].TypeName == "type");
            Assert.IsTrue(node[0].GenericTypeNames[0].TypeName == "subtype");
        }

        [TestMethod]
        public void MultipleCompoundTypeParseTest()
        {
            var (node, _) = VentClassName.ParseGenericArgs("<type1<subtype1>, type2 < subtype2 >, type3 <  subtype3>>");

            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(node[i].TypeName == "type" + (i + 1));
                Assert.IsTrue(node[i].GenericTypeNames[0].TypeName == "subtype" + (i + 1));
            }
        }

        [TestMethod]
        public void SingleComplexCompoundTypeParseTest()
        {
            var (node, _) = VentClassName.ParseGenericArgs("<type<subtype1, subtype2>>");
            Assert.IsTrue(node[0].TypeName == "type");
            Assert.IsTrue(node[0].GenericTypeNames[0].TypeName == "subtype1");
            Assert.IsTrue(node[0].GenericTypeNames[1].TypeName == "subtype2");
        }
        
        [TestMethod]
        public void MultipleComplexCompoundTypeParseTest()
        {
            var (node, _) = VentClassName.ParseGenericArgs("<t1<s1, s2>, t2 < s1, s2 >>");

            for (var i = 0; i < 2; i++)
            {
                Assert.IsTrue(node[i].TypeName == "t" + (i + 1));
                for (var j = 0; j < 2; j++)
                {
                    Assert.IsTrue(node[i].GenericTypeNames[j].TypeName == "s" + (j + 1));
                }
            }
        }

        [TestMethod]
        public void VentClassNameTest()
        {
            var name = typeof(ObjectWrapperEntity<>).ToVentClassName();

            Assert.IsTrue(name == "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity");

            name = typeof(ObjectWrapperEntity<string>).ToVentClassName();


            Assert.IsTrue(name == "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<System.String>");

            name = typeof(ObjectWrapperEntity<ObjectWrapperEntity<string>>).ToVentClassName();

            Assert.IsTrue(name == "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<System.String>>");

            name = typeof(Dictionary<string, StringEntity>).ToVentClassName();

            Assert.IsTrue(name == "System.Collections.Generic.Dictionary<System.String,Vent.Entities.StringEntity>");
        }

        [TestMethod]
        public void ResolveTypeTest()
        {
            var lookup = ClassLookup.CreateFrom(typeof(ObjectWrapperEntity<>).Assembly,
                                                    typeof(IEntity).Assembly)
                                                    .WithType(typeof(Dictionary<,>));

            var name = typeof(ObjectWrapperEntity<string>).ToVentClassName();

            Assert.IsTrue(name == "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<System.String>");

            var node = VentClassName.ParseVentClassName(name);
            var nodeType = node.ResolveType(lookup);

            Assert.IsTrue(nodeType == typeof(ObjectWrapperEntity<string>));
            
            name = typeof(ObjectWrapperEntity<ObjectWrapperEntity<string>>).ToVentClassName();

            Assert.IsTrue(name == "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<System.String>>");

            node = VentClassName.ParseVentClassName(name);
            nodeType = node.ResolveType(lookup);

            Assert.IsTrue(nodeType == typeof(ObjectWrapperEntity<ObjectWrapperEntity<string>>));

            name = typeof(Dictionary<string, StringEntity>).ToVentClassName();

            Assert.IsTrue(name == "System.Collections.Generic.Dictionary<System.String,Vent.Entities.StringEntity>");

            node = VentClassName.ParseVentClassName(name);
            nodeType = node.ResolveType(lookup);

            Assert.IsTrue(nodeType == typeof(Dictionary<string, StringEntity>));
        }
    }
}
