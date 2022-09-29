# Lwnr
Experimental automatic resource control

## Rules
1. functions can never return values
2. every constructor must have a deconstructor
3. everything is deconstructed when it goes out of scope
4. memory addresses are never available

## Implications
* to get a result from a function, you must pass in a container.
* we must have a way to create something in a container's scope.

* every type must have a constructor and deconstructor
* any external resources must be opened during constructor and closed in destructor
* instances are only accessible from their container? (Construct into container first, then alias -- aliases cannot be added to containers)
* every type can be serialised and deserialised perfectly, including containers.
* instances can only be moved from one container to another by copying.
* instances can be passed as arguments to a function even if they are in containers.

* We get some basic type classes:
  1. `Primitive` -- bottom value of types. Things like byte, int, float, etc. Could be any value stored in a contiguous series of bytes with a length that is known and is not variable, maybe including serialised forms?
  2. `Container` -- roughly equivalent to reference types, but holds an internal reference to its scope
  3. `Alias` -- refers to a value inside a container. Can be copied to make a primitive, but cannot be added to a container directly

* We probably don't want to allow declaration of containers without making an instance
* There is no need for a null value, as empty container does fine.
* There will need to be some built-in containers, and they need to be good and flexible
  - `map` hashmap (variable size)
  - `vec` multi-vector (stack,queue,vector) (variable size)
  - `maybe` single-or-none container (fixed size)
  - `array` basic pre-allocated list (fixed size)
  - ??? `char` NOT byte, but a uint16/uint32 that can be represented as one or more uint8 (fixed size)


* Lambda functions should be fine, but only available as `alias` class, preventing them from being stored for future use.
* Spawning threads only makes sense if the caller waits for them to end. This probably works fine with a supervisor->worker model with message passing.

## Optimisations

* If a _lexical_ scope does no allocation in itself (does not include calling a container's allocator), then there is no need to start or end a _memory_ scope.
* The actual implementation of containers could be optimised to the way they are used -- i.e. have several types of Vector, and pick based on e.g. do we ever dequeue?

## Motivating example?

```
(def
  database-query (into query)
  
  (set conn connect-to-database) # creates and opens db connection
  
  . . .
  (alias item (into new)) # 'item' is NOT in our scope, as it's part of 'into', and aliased here
  
  (conn :query-into item) # alias passed in, as a reference to a fixed primitive
  
  # db connection is closed here
)

(
  (set x (maybe.new)) # binds with scope here
  (database-query x "SELECT COUNT(*) FROM table")
  
  (if (x hasValue)
    then:(log (x value))
    else:(log "Could not read value")
  )
  
  # 'x' AND all its content gets destroyed here
)
```

```
(def work (work-queue)
  ...do stuff...
)

(
  (set q (vector.new))
  
  ... fill queue...
  
  (set t1 (thread.new method: work data: q))
  (set t2 (thread.new method: work data: q))
  (set t3 (thread.new method: work data: q))
  
  (thread.join t1 t2 t3)
)
```
