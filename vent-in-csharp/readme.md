Versioned Entities (VENT 0.2)
=============================

VENT is a system allowing applications to track - and move between the state of entities over the lifetime of the application. In other words it's a system enabling undo-redo functionality. Do note that the implementation is closer to how versioning systems like Git work rather than a "traditional" undo-redo implementation. This document introduces VENT its core concepts and what quirks to be aware of.

VENT is currently in version 0.1.x and is actively developed. See the 'todo' section below to get more details on the project.

 Vent is released under Creative Commons BY-SA,see https://creativecommons.org/licenses/by-sa/4.0/ 
 (c) Pointlesspun https://github.com/pointlesspun/vent


Use cases
---------

VENT is aimed at applications requiring history tracking and where the user can move from the current state back in time and vice versa. Furthermore this ability to go back and forth in the application's history should allow for clear introspection and debugging similar to Redux. It's best used when the application objects use discrete, granular states like CRUD applications. It's less suitable where objects change continuously and updates cannot easily be grouped like document editing.

The current implementation VENT is not thread safe.

Further reading
---------------

* [Vent basics](./doc/vent-basics.md)
* [Vent in json, Vent de/serialization](./doc/vent-in-json.md)

(Potential) List of Things To Do
------------------------------

* v 0.2.0 Add Json Serialization and Deserialization
  * Clean up code
  * Add documentation
* v 0.3.0 Client / Server Syncing
* v 0.4.0 Implement a game of Solitaire
* v 0.5.0 Object pooling
* v 0.6.0 Thread safe versions
* v 0.7.0 High frequency store

### Done

* v 0.1.0

  - Update documentation
  - Check Release test, make sure behavior is still correct independent of the build type
  - Verify release also runs all tests correctly
  - Test for wrap around; add maxEntities
  - Check if DeleteMutation(n) is possible instead of RemoveOldestMutation() and if so implement this; code has been implemented to test this.
