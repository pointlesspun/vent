VENT TO JSON
============

The Vent To Json project allows for serializing an `entity registry` to json and deserializing a json file to an `entity registry`. For example:

```csharp
    using static Vent.ToJson.Utf8JsonWriterExtensions;
    using static Vent.ToJson.Readers.Utf8JsonEntityReader;

    ...

    // create a registry with a some random assortments of entities entity
    var registry = new EntityRegistry()
    {
        new StringEntity("foo"),
        new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1),
        new PropertyEntity<int>(42),
        new ObjectWrapperEntity<StringEntity>(new StringEntity("bar"))
    };

    // write the object to a json string
    var jsonString = WriteRegistryToJson(registry);

    // create an entity reader and read the data from the json string
    var clonedRegistry = ReadEntity<EntityRegistry>(jsonString);

    Assert.IsTrue(clonedRegistry.Equals(registry));
```

In most cases this is generally all you will need. However there are a number of implementation details which are good to know when using the vent json serialization which is discussed in the rest of this document.

Supported Types
----------------

The Json de/serializing supports public non static properties (fields are not de/serialized) as long as they are part of a limited set of types. The supported types include (in order of deserialization):

* Null
* Entities if declared in the [Class Lookup](#class-lookup)
* Primitive or string
* Array
* DateTime
* List
* Dictionary; keys are limited to strings, primitives and datetime.
* Object/Struct

The generated Json format
-------------------------

The generated json for the example above will look like this:

```json
{
  "__entityType": "Vent.Registry.EntityRegistry",
  "EntitySlots": {
    "0": {
      "__entityType": "Vent.Entities.StringEntity",
      "Value": "foo",
      "Id": 0
    },
    "1": {
      "__entityType": "Vent.ToJson.Test.TestEntities.MultiPropertyTestEntity",
      "BooleanValue": true,
      "StringValue": "foo",
      "CharValue": "a",
      "IntValue": -42,
      "UIntValue": 42,
      "FloatValue": 0.1,
      "DoubleValue": -0.1,
      "Id": 1
    },
    "2": {
      "__entityType": "Vent.Entities.PropertyEntity<System.Int32>",
      "Value": 42,
      "Id": 2
    },
    "3": {
      "__entityType": "Vent.ToJson.Test.TestEntities.ObjectWrapperEntity<Vent.Entities.StringEntity>",
      "Value": {
        "__entityType": "Vent.Entities.StringEntity",
        "Value": "bar",
        "Id": -1
      },
      "Id": 3
    }
  },
  "MaxEntitySlots": 2147483647,
  "NextEntityId": 4,
  "Version": "0.2",
  "Id": -1
}
```

The json for an EntityRegistry is supposed to be straightforward. Each object, including the EntityRegistry itself, will have the following structure:

* If the object is an Entity and it's serialized as a value (see [Entity References](#entity-references)), the first property must have the property name "__entityType" followed by a string containing the class declaration. This class declaration must be part of the [class lookup](#class-lookup).

* If the object is an Entity and serialized as reference (see [Entity References](#entity-references)), it will be represented as a number or a string representing a number. We see this in the keys of the "EntitySlots" property in the EntityRegistry.

* The remainder of the object/entities' public, non static properties of the [supported types](#supported-types) in a key/value format where the key is the property name and the value is the serialized value.

Class Lookup
------------

When deserializing json, the deserialization process will create new objects as needed. To provide some bare-bones security, you will need to specify what objects can be created during the deserialization. Types that are allowed need to be added to `ClassLookup`. To make life a little easier the class lookup can be constructed using the following methods:

* Using `ClassLookup.CreateDefault()`, this will create a lookup based on the public, non static types in the calling assembly, executing assembly (which is generally Vent.Json) and the  types in Vent (ie IEntity et al). Furthermore `List<>` and `Dictionary<>` are added.
* By using `ClassLookup.CreateFrom()` with all the assemblies you want to use for providing the types.
* By using `ClassLookup.WithType()` to add specific types.

The `ClassLookup` must be added to the `JsonReaderContext` which can be provided to a `JsonReader`. In the example above none if this is used because it's taken care of using the default settings. To use a non default class lookup use the following:

```csharp
      // create a registry with a some random assortments of entities entity
      var registry = new EntityRegistry()
      {
          new StringEntity("foo"),
          new MultiPropertyTestEntity(true, "foo", 'a', -42, 42, 0.1f, -0.1),
          new PropertyEntity<int>(42),
          new ObjectWrapperEntity<StringEntity>(new StringEntity("bar"))
      };

      // write the object to a jsonstring
      var jsonString = WriteRegistryToJson(registry);

      // setup the class lookup
      var classLookup = ClassLookup.CreateFrom(typeof(EntityRegistry),
                                              typeof(StringEntity), typeof(MultiPropertyTestEntity),
                                              typeof(PropertyEntity<>), typeof(ObjectWrapperEntity<>),
                                              typeof(Dictionary<,>));

      var context = new JsonReaderContext(registry, classLookup);
      var reader = new Utf8JsonEntityReader();
      var clonedRegistry = reader.ReadFromJson(jsonString, context, EntitySerialization.AsValue);

      Assert.IsTrue(clonedRegistry.Equals(registry));
```        

If all of that seems like too much work you can simply refer to the assemblies you need and add specific classes if required eg

```csharp
var classLookup = ClassLookup.CreateFrom(typeof(MultiPropertyTestEntity).Assembly, typeof(StringEntity).Assembly)
                                 .WithType(typeof(Dictionary<,>))
                                 .WithType(typeof(List<>));
```

Entity References
-----------------

Entities can be de/serialized in two ways: 

* By reference 
* By value

When the de/serialization encounters an entity (property) it will check the current entity serialization parameter. This parameter can have the value `EntitySerialization.AsReference` or `EntitySerialization.AsValue`. If the value is a `AsReference` the entity's id will be serialized and when deserializing it will try to find the entity by id in the `RegistryContext`'s current Registry. If the value is `AsValue` the entity will be de/serialized as if it was an Object: all properties will be saved as well as its EntityType.

Entity properties of the type `IEntity` are generally saved as reference. However the class can add the `SerializeAsValue` attribute to indicate the specific entity value needs to be de/serialized by value. See `EntityRegistry.EntitySlots` for an example.

Forward References
------------------

(NOTE: you don't need to know this unless you're delving into the implementation of Vent.ToJson)

Vent to Json uses the System.Text.Json json reader, which according to the documentation `"Provides a high-performance API for forward-only, read-only access to the UTF-8 encoded JSON text."`. This means Vent won't be handed conveniently parsed and structured nuggets of information like newton and has to do some 'heavy' lifting itself. One aspect of this is that when entities are serialized by [reference](#entity-references), the entity itself may not have been parsed yet. Consider the following scenario:

```csharp
  var ent1 = new SomeEntity();
  var ent2 = new SomeEntity();

  ent1.entityProperty = ent2;
  ent2.entityProperty = ent1;

  var registry = new EntityRegistry() { ent1, ent2 };
```

When this registry gets serialized and then deserialized, it will encounter ent1 and starts deserializing. In ent1 it will encounter `entityProperty` which refers, by reference, to ent2. It will look in the current registry and find no ent2. Moreover since the json library is forward only, ent2 will not exist until later in the deserialization process.

To deal with this situation, the reader generates a placeholder entity of the type `ForwardEntityReference`. This placeholder entity will capture all relevant information. When the entity registry has completed deserialization, it will go through all collected forward references and resolve them accordingly (see `TypeNameNode.ResolveForwardReferences`). 

Adding more readers writers
---------------------------

At the moment the current implementation does not support adding custom readers or writers. If more custom readers/writers are necessary, you can add them to `Utf8JsonReaderExtensions.ReadValue`.

Custom Entity Serialization
---------------------------

If the existing set of reading and writing is not sufficient for the a specific entity, you can implement the `ICustomJsonSerializable` interface. This interface comes with two methods `Read` and `Write`. In these methods you will only need to de/serialize the specific properties of this entity. Note that the order in which values are serialized must match the order in which the value were serializard, eg:

```csharp
public class CustomMultiPropertySerializableTestEntity : MultiPropertyTestEntity, ICustomJsonSerializable
{
    public void Read(ref Utf8JsonReader reader, JsonReaderContext _)
    {
        Id = reader.ReadPrimitiveProperty<int>(nameof(Id));
        StringValue = reader.ReadPrimitiveProperty<string>(nameof(StringValue));
        IntValue = reader.ReadPrimitiveProperty<int>(nameof(IntValue));
    }

    // we only write some selected properties
    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(Id), Id);
        writer.WriteProperty(nameof(StringValue), StringValue);
        writer.WriteProperty(nameof(IntValue), IntValue);
    }
}
```