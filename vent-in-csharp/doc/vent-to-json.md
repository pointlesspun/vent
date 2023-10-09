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
        new PropertyEntity<int>(42)
    };

    // write the object to a json string
    var jsonString = WriteRegistryToJson(registry);

    // create an entity reader and read the data from the json string
    var clonedRegistry = ReadEntity<EntityRegistry>(jsonString);

    Assert.IsTrue(clonedRegistry.Equals(registry));
```

In most cases this is generally all you will need. However there are a number of implementation details which are good to know when using the vent json serialization which is discussed in the rest of this document.

Supported values
----------------

The generated Json format
-------------------------

Class Lookup
------------

Entity References
-----------------

Forward References
------------------

Adding more readers writers
---------------------------

