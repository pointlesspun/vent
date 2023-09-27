using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vent.PropertyEntities;
using Vent.ToJson.Test.TestEntities;

namespace Vent.ToJson.Test
{
    [TestClass]
    public class RegistryConverterTest
    {
        [TestMethod]
        public void SingleStringEntityTest()
        {
            var registry = new EntityRegistry
            {
                new StringEntity("foo")
            };

            CloneAndTest(registry);
        }

        [TestMethod]
        public void MultipleStringEntityTest()
        {
            var registry = new EntityRegistry
            {
                new StringEntity("foo"),
                new StringEntity("bar"),
                new StringEntity("qaz")
            };

            CloneAndTest(registry);
        }

        [TestMethod]
        public void MultipleMultiPropertyEntityTest()
        {
            var registry = new EntityRegistry()
            {
                new MultiPropertyTestEntity(true, "foo", 'c', -42, 42, -42.0f, 42.42),
                new MultiPropertyTestEntity(false, "bar", 'b', -0, 1, float.MaxValue, Double.MinValue),
            };

            CloneAndTest(registry);
        }

        [TestMethod]
        public void EntityReferenceTest()
        {
            var registry = new EntityRegistry();
            var str1 = registry.Add(new StringEntity("foo"));
            registry.Add(new EntityPropertyEntity(str1));

            var str2 = new StringEntity("bar");

            // point to an entity ahead, this will require the converter
            // to delay registration until str2 appears in the store
            registry.Add(new EntityPropertyEntity(str2));
            registry.Add(str2);
            
            // add entity with a null reference
            registry.Add(new EntityPropertyEntity());

            CloneAndTest(registry);
        }

        [TestMethod]
        public void IntListEntityTest()
        {
            var registry = new EntityRegistry()
            {
                new IntListEntity()
                {
                    IntList = new List<int> { 1, 2 },
                }
            };

            CloneAndTest(registry);
        }

        /// <summary>
        /// Test an Entity list with both backward and forward entity references
        /// </summary>
        [TestMethod]
        public void EntityListEntityTest()
        {
            // backward reference - ie foo is registered before the list, so it
            // will exist when the list is deserialized            
            var foo = new StringEntity("foo");

            // forward reference - ie bar is registered after the list, so it
            // will not exist when the list is deserialized and will need
            // to be resolved after the deserialization
            var bar = new StringEntity("bar");

            var registry = new EntityRegistry()
            {
                foo,
                new EntityListEntity()
                {
                    EntityList = new List<IEntity> { foo, bar }
                },
                bar
            };

            CloneAndTest(registry);
        }

        /// <summary>
        /// Test cloning a registry containing an object which has an entity with an entity property
        /// marked as serialize as value rather than the default (serialize as reference)
        /// </summary>
        [TestMethod]
        public void ObjectWrapperTest()
        {
            var multiPropertyEntity = new MultiPropertyTestEntity(true, "foo", 'x', -42, 43, 0.1f, -0.1);
            CloneAndTest(new EntityRegistry()
            {
                // ObjectWrapper's value is defined as [SerializeAsValue], so
                // the MultiPropertyTestEntity will be fully serialized as a value
                new ObjectWrapper<MultiPropertyTestEntity>(multiPropertyEntity),
                new ObjectWrapper<MultiPropertyTestEntity>()
            });
        }

        /// <summary>
        /// Same as ObjectWrapperTest, but we throw in the original object an ObjectWrapper
        /// is referring to as well as an object which refers to this original object
        /// </summary>
        [TestMethod]
        public void MixedObjectWrapperTest()
        {
            // the original object
            var multiPropertyEntity = new MultiPropertyTestEntity(true, "foo", 'x', -42, 43, 0.1f, -0.1);
            CloneAndTest(new EntityRegistry()
            {
                multiPropertyEntity,
                // this will 'copy' multiPropertyEntity as its value property is marked as  
                // SerializeAsValue
                new ObjectWrapper<MultiPropertyTestEntity>(multiPropertyEntity),
                // this will reference multiPropertyEntity as its property is not marked 
                // as SerializeAsValue
                new PropertyEntity<IEntity>(multiPropertyEntity)
            });
        }

        [TestMethod]
        public void StringDictionaryTest()
        {
            var stringDictionary = new ObjectEntity<Dictionary<string, string>>()
            {
                Value = new Dictionary<string, string>()
                    {
                        { "fooKey", "fooValue" },
                        { "barKey", "barValue" },
                        { "qazKey", "qazValue" },
                    }
            };

            CloneAndTest(new EntityRegistry()
            {
                stringDictionary
            },
            (source, clone) =>
            {
                AssertRegistriesPropertiesMatch(source, clone);
                
                var sourceDict = (source[stringDictionary.Id] as ObjectEntity<Dictionary<string, string>>).Value;
                var cloneDict = (clone[stringDictionary.Id] as ObjectEntity<Dictionary<string, string>>).Value;

                AssertDictionaryValuesMatch(sourceDict, cloneDict);
            });
        }

        [TestMethod]
        public void IntDictionaryTest()
        {
            var intDictionary = new ObjectEntity<Dictionary<int, int>>()
            {
                Value = new Dictionary<int, int>()
                    {
                        { 1, 2 },
                        { 3, 4 },
                        { 5, 6 },
                    }
            };

            CloneAndTest(new EntityRegistry()
            {
                intDictionary
            },
            (source, clone) =>
            {
                AssertRegistriesPropertiesMatch(source, clone);

                var sourceDict = (source[0] as ObjectEntity<Dictionary<int, int>>).Value;
                var cloneDict = (clone[0] as ObjectEntity<Dictionary<int, int>>).Value;

                AssertDictionaryValuesMatch(sourceDict, cloneDict);
            });
        }

        [TestMethod]
        public void EntityDictionaryTest()
        {
            var ent1 = new StringEntity("foo");
            var ent2 = new StringEntity("bar");
            
            var entDictionary = new ObjectEntity<Dictionary<string, StringEntity>>()
            {
                Value = new Dictionary<string, StringEntity>()
                {
                    { "1", ent2 },
                    { "2", ent1 },
                }
            };

            CloneAndTest(new EntityRegistry()
            {
                ent1,
                entDictionary,
                ent2,
            },
            (source, clone) =>
            {
                AssertRegistriesPropertiesMatch(source, clone);

                var sourceDict = (source[entDictionary.Id] as ObjectEntity<Dictionary<string, StringEntity>>).Value;
                var cloneDict = (clone[entDictionary.Id] as ObjectEntity<Dictionary<string, StringEntity>>).Value;

                AssertDictionaryValuesMatch(sourceDict, cloneDict);
            });
        }

        [TestMethod]
        public void EntityDictionaryWithWrapperTest()
        {
            var ent1 = new StringEntity("foo");
            var ent2 = new StringEntity("bar");

            var entDictionary = new ObjectWrapper<Dictionary<string, StringEntity>>()
            {
                // ent1 and ent2 are not added to the registry because the objectwrapper
                // declares the value as SerializeAsValue, so the entities are actually
                // serialized not referenced
                Value = new Dictionary<string, StringEntity>()
                {
                    { "1", ent2 },
                    { "2", ent1 },
                }
            };

            CloneAndTest(new EntityRegistry()
            {
                entDictionary,
            },
            (source, clone) =>
            {
                AssertRegistriesPropertiesMatch(source, clone);

                var sourceDict = (source[entDictionary.Id] as ObjectEntity<Dictionary<string, StringEntity>>).Value;
                var cloneDict = (clone[entDictionary.Id] as ObjectEntity<Dictionary<string, StringEntity>>).Value;

                AssertDictionaryValuesMatch(sourceDict, cloneDict);
            });
        }

        [TestMethod]
        public void ListObjectTest()
        {
            var ent1 = new StringEntity("foo");
            var ent2 = new StringEntity("bar");

            CloneAndTest(
                new EntityRegistry()
                {
                    ent1,
                    new ObjectEntity<List<StringEntity>>()
                    {

                        Value = new List<StringEntity>()
                        {
                            ent2,
                            ent1,
                        }
                    },
                    ent2
                },
                (source, clone) =>
                {
                    AssertRegistriesPropertiesMatch(source, clone);

                    var sourceList = (source[1] as ObjectEntity<List<StringEntity>>).Value;
                    var cloneList = (clone[1] as ObjectEntity<List<StringEntity>>).Value;

                    Assert.IsTrue(sourceList.SequenceEqual(cloneList));
                }
            );
        }

        [TestMethod]
        public void ListObjectWrapperTest()
        {
            CloneAndTest(
                new EntityRegistry()
                {
                    new ObjectWrapper<List<StringEntity>>()
                    {

                        Value = new List<StringEntity>()
                        {
                            new StringEntity("foo"),
                            new StringEntity("bar"),
                        }
                    },
                },
                (source, clone) =>
                {
                    AssertRegistriesPropertiesMatch(source, clone);

                    var sourceList = (source[0] as ObjectWrapper<List<StringEntity>>).Value;
                    var cloneList = (clone[0] as ObjectWrapper<List<StringEntity>>).Value;

                    Assert.IsTrue(sourceList.SequenceEqual(cloneList));
                }
            );
        }


        [TestMethod]
        public void InnerRegistryTest()
        {
            CloneAndTest(
                new EntityRegistry()
                {
                    new StringEntity("outer-foo"),
                    new EntityRegistry() 
                    {
                        new StringEntity("inner-foo"),
                        new StringEntity("inner-bar"),
                    },
                    new StringEntity("outer-bar"),
                }
            );
        }

        [TestMethod]
        public void CustomSerializableTest()
        {
            CloneAndTest(
                new EntityRegistry()
                {
                    new CustomMultiPropertySerializableTestEntity()
                    {
                        StringValue = "foo",
                        IntValue = -42,
                        UIntValue = 42
                    }
                },
                (source, clone) =>
                {
                    AssertRegistriesPropertiesMatch(source, clone);

                    var sourceEnt = source[0] as CustomMultiPropertySerializableTestEntity;
                    var cloneEnt = clone[0] as CustomMultiPropertySerializableTestEntity;

                    Assert.IsTrue(sourceEnt.StringValue == cloneEnt.StringValue);
                    Assert.IsTrue(sourceEnt.StringValue == "foo");
                    Assert.IsTrue(sourceEnt.IntValue == cloneEnt.IntValue);
                    Assert.IsTrue(sourceEnt.IntValue == -42);
                    Assert.IsTrue(sourceEnt.UIntValue != cloneEnt.UIntValue);
                    Assert.IsTrue(sourceEnt.UIntValue == 42);
                    Assert.IsTrue(cloneEnt.UIntValue == default(uint));
                    Assert.IsTrue(sourceEnt.BooleanValue == cloneEnt.BooleanValue);
                    Assert.IsTrue(sourceEnt.CharValue == cloneEnt.CharValue);
                }
                );
        }

        private void AssertDictionaryValuesMatch(IDictionary d1, IDictionary d2)
        {
            Assert.IsTrue(d1 != d2);

            foreach (var key in d1.Keys)
            {
                Assert.IsTrue(d1[key].Equals(d2[key]));
            }
        }

        // xxx to test
        // - Entity History property
        private static JsonSerializerOptions CreateTestOptions()
        {
            var classLookup = ClassLookup
                                .CreateFrom(typeof(MultiPropertyTestEntity).Assembly, typeof(StringEntity).Assembly)
                                .WithType(typeof(Dictionary<,>))
                                .WithType(typeof(List<>));

            var converter = new EntityRegistryConverter(classLookup);
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                // don't want to see escaped characters in the tests
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    converter
                }
            };
        }

        private void CloneAndTest(EntityRegistry registry, Action<EntityRegistry, EntityRegistry>  testActions = null)
        {
            var serializeOptions = CreateTestOptions();
            var json = JsonSerializer.Serialize(registry, serializeOptions);
            var clone = JsonSerializer.Deserialize<EntityRegistry>(json, serializeOptions);

            if (testActions == null)
            {
                AssertEqualsButNotCopies(registry, clone);
            }
            else
            {
                testActions(registry, clone);
            }
        }

        private void AssertRegistriesPropertiesMatch(EntityRegistry source, EntityRegistry clone)
        {
            Assert.IsTrue(source != null);
            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone != source);
            Assert.IsTrue(clone.MaxEntitySlots == source.MaxEntitySlots);
            Assert.IsTrue(clone.NextEntityId == source.NextEntityId);
            Assert.IsTrue(clone.EntitiesInScope == source.EntitiesInScope);
        }

        private void AssertEqualsButNotCopies(EntityRegistry source, EntityRegistry clone)
        {
            Assert.IsTrue(source != null);
            Assert.IsTrue(clone != null);
            Assert.IsTrue(clone != source);
            Assert.IsTrue(source.Equals(clone));    

            foreach (var kvp in source)
            {
                var clonedEntity = clone[kvp.Key];
                var sourceEntity = source[kvp.Key];

                Assert.IsFalse(clonedEntity == sourceEntity);
            }
        }
    }
}
