Versioned Entities (VENT 0.1)
==============================

VENT is a system allowing applications to track - and move between the state of entities over the lifetime of the application. In other words it's a system enabling undo-redo functionality. Do note that the implementation is closer to how versioning systems like Git work rather than a "traditional" undo-redo implementation. 

For more information see the [vent-in-csharp readme](https://github.com/pointlesspun/vent/blob/main/vent-in-csharp/readme.md).

Vent is released under Creative Commons BY-SA,see https://creativecommons.org/licenses/by-sa/4.0/ 
 
(c) Pointlesspun https://github.com/pointlesspun/vent

Short example:

```csharp
public void BasicCommitUndoExample()
{
    // create a new store
    var store = new EntityStore();

    // commit an entity to the store to track information
    var ent = store.Commit(new PropertyEntity<string>("foo"));

    // change the entity (head)
    ent.Value = "bar";

    // commit the current entity state
    store.Commit(ent);

    // change the entity value
    ent.Value = "qez";

    // first undo
    Assert.IsTrue(store.Undo());
    Assert.AreEqual("bar", ent.Value);

    // move to "foo" check if ent is still in the store
    Assert.IsTrue(store.Undo());
    Assert.AreEqual("foo", ent.Value);
    Assert.IsTrue(ent.Id >= 0);
    Assert.IsTrue(store.Contains(ent));

    // cannot move any further, foo has been removed from the store.
    Assert.IsFalse(store.Undo());
    Assert.AreEqual("foo", ent.Value);
    Assert.IsTrue(ent.Id == -1);
    Assert.IsFalse(store.Contains(ent));

    // redo will seem to do nothing
    Assert.IsTrue(store.Redo());
    Assert.AreEqual("foo", ent.Value);
    Assert.IsTrue(ent.Id == -1);
    Assert.IsFalse(store.Contains(ent));

    // until this point ... foo makes it back into the store
    Assert.IsTrue(store.Redo());
    Assert.AreEqual("foo", ent.Value);
    Assert.IsTrue(ent.Id >= 0);
    Assert.IsTrue(store.Contains(ent));

    Assert.IsTrue(store.Redo());
    Assert.AreEqual("bar", ent.Value);
    Assert.IsTrue(ent.Id >= 0);
    Assert.IsTrue(store.Contains(ent));

    // reached the end
    Assert.IsFalse(store.Redo());
}
```


