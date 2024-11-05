
Aliasing, borrowing, and copying
--------------------------------

No aliasing. All movement is semantically a copy operation. Compiler should do move/rename instead of copy when it can, but copy is the only programmer option to change the name of something. CoW arenas should be supported at everywhere.

All types can serialised, and everything boils down to an array (maybe arenas, maybe types can provide indexing functions - like tree pointer is inside a specific array).

Referencing and indirection is by specific referencing objects, and they are tied to the lifetime of the data they reference.
A reference object can point to a part of a larger object, like passing in a pointer to a value for a sub-func to change.
References cannot be copied or moved, just written or read.

Anything long-term or widely scoped should go out to a database, and stored or read using a message cycle.

Every actor has:
- reference to code being run
- active memory (arenas and call stack)
- an inbox
- an outbox

Actors can be serialised/deserialised too.

Actor is the only thing that can write to the outbox. Kernel Scheduler is only thing that can read.
Actor can only read from inbox, no writing. Actor marks incoming message as handled by writing an out message for the scheduler.

Actors are single threaded, but can request child actors, each or which can run independently. Parent can send work to children by sending messages.

Each inbox has a unique ID. Standard ones for system calls. Can request one to access a library.

The ID of an inbox can be sent; this works as forwarding/indirection, or as a work loop to prevent need for back-tracking a long chain.

Inboxes normally block caller if there is not enough space for more messages.
A mode can be set where new messages are dropped when there is already a message in the box -- this implements
a 'trigger' pattern, where the action is not repeated until last trigger is complete (like when pressing a button, it is
not available until current action is done).

Syntax ideas
------------

Idea for a syntax that doesn't hide much of the behaviour.

`(def fun-name input1 input2 -> outp1 outp2 ( ... ))`

Everything after `->` must be writable and container types.

to use the output of one call as the input of another

`(outer (fun-name in1 in2 -> out1 out2^) -> out3)`

Here `out2` is the first input to `outer`