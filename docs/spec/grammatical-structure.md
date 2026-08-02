# 4 Grammatical Structure
***Syntax*** is defined as an ordered grouping of specific ***token***s.
## 4.1 Mutability
One of `var`, `constant`, or `let`
## 4.2 Modifier
One of `compiled`, `optional`, `shared`, `persistent`, `export`, or `extends`
## 4.3 Name
Sequence of one or more ***word***s or ***symbol***s which are not ***punctuation***.

A name is its **words**, not the text it was written as.  Some keywords are two
words — `part of`, `for each` — and each is a single word for every purpose:
`ready part of world` is three words and not four, however it was spaced, and
`ready part of world` and `ready part  of world` are the same name.

That matters wherever a rule counts words.  A pattern's glue may be one of
these, and a name may not contain it (§ *scope rules*, R5) — so the comparison
is between words and never between renderings.
## 4.4 Identifier
Sequence of one or more ***name***s or ***parameters***.

A ***keyword*** that introduces a production may not be the identifier's **first
word**, because there is where an outer production would otherwise take the
declaration: `function f => Number { … }` would parse as a datum named
`function f`.  Anywhere else it is an ordinary word, so `var ready if needed` is
a name and `function send (x) part of (y)` is a pattern.

A **bracket in a name marks one argument**, not a parameter list — a declared
name has no parameter lists.  `send (message) to (recipient)` is called `send x
to y`, so `(message)` is one hole with one name.  `()` is therefore a hole with
no name and is refused: a function that takes nothing is declared `function
ping`, which is what `ping` is called.

The same bracket in a ***delegate*** is a **signature** and not a name, so it
does list its parameters: `() => { … }` and `(a, b) => { … }` are both well
formed.

The distinction is not about the character but about what is being declared:

> A bracket is a **hole** where a *call-site shape* is being declared, and a
> **signature** where a *callable value's type* is being described.

`function send (message) to (recipient)` declares syntax — how a call is
written.  `var callback => () => Number` describes a value.  So the two rules
are one rule applied to two kinds of thing, and do not contradict.

A hole in a name is also a hole in a *declared* name only.  A ***parameter*** and
a ***loop variable*** are names and nothing else — they are bound to one value on
entry, so there is nothing for a hole in one to mean, and one is refused.

Every hole names its argument.  `(_)` is *pattern notation* — what the registry
renders when it is describing a shape and the names are not its business — and
is not source.

An identifier's words must **read back as themselves**, and this holds for
*every* declaration — data, constants, types, functions, patterns and loop
variables alike.  Trivia between the two words of a composite keyword is the one
way to write one that does not: `compute part /* gap */ of (x)` declares three
words that, written down, are two.  It is refused, because a name is stored by
its rendering, and a name whose rendering states different words than the
declaration holds is one the compiler cannot tell apart from a different name.
A **parameter is a declaration**, checked exactly as any other: its words must
read back as themselves, it may not take a reserved prefix, and it may not
collide with anything in scope.  It is declared into the body it is bound in, so
a body redeclaring one is shadowing it — and so is a parameter named after
something the enclosing scope already has.

## 4.4 Declaration
### 4.4.1 Datum
***mutability***? *identifier* (`=>` ***modifier**** *datatype*)? (`=` *initializer*)?
- identifier is ***words***
- datatype is a ***reference***
- initializer is a ***value***
### 4.4.2 Function
***modifier**** `function` ***identifier*** (`=>` ***modifier**** *datatype*)? (*body*|`;`)
- modifiers is `export` | `shared`
- datatype is a ***reference***
- body is a ***definition***
### 4.4.3 Datatype
`extends`? `datatype` *identifier* (`=` *algebra*) *body*
- identifier is a ***name***
- algebra is a ***reference***
- body is a ***definition***
## 4.5 Scope
Scopes may not be preceeded by an ***assignment***.  All scopes may be preceeded by `compiled`.
### 4.5.1 Anonymous
`export`? *body*
- body is a ***definition***
### 4.5.2 Conditional
`if` *condition* *body*
- condition is a ***refrence***
- body is a ***definition***
### 4.5.3 Repeating
`while` *condition* *body*
- condition is a ***reference***
- body is a ***definition***
### 4.5.4 Iterating
`for each` *loop variable* `in` *collection* *body*
- loop variable is a ***word***, or a ***bracketed name***
- collection is a ***reference***
- body is a ***definition***

**The loop variable is a binding occurrence, pinned to one word.**  A
multi-word name goes in brackets: `for each (order in transit) in shipments`.
It declares the name rather than referring to one, so it is not looked up and
costs nothing — and it is a *name*, not a value: a literal, an operation,
several values, a square or curly bracket, or a mismatched pair are all
refused.  The pin is what makes a
loop header have exactly one reading — a free-growing variable could swallow the
`in` and take part of the collection with it, and the competing readings do not
tie, so nothing would report it.

`in` is **not reserved**.  It was, and the reservation was the first way to
force one reading; pinning gets the same guarantee without taking a word away
from anyone.  A hole fixed at one token cannot grow across the word that follows
it, so the split point is determined by the pattern's shape rather than by a
rule about names.  `var minutes in transit => Number;` is legal, and so is a
loop over it.

That generalises: a pattern reserves a glue word only where the hole before it
could grow over that word.  A hole is **determinate** when it cannot — pinned to
one token, or required to be bracketed — and glue after a determinate hole costs
nothing.  `docs/reserved-words.txt` is generated from that condition and
currently lists no reserved words at all.

A loop injects one name into its body: `index of` followed by the loop
variable, so `for each bank in banks` gives `index of bank`.  It is derived from
the variable rather than being a bare `index` because this language has no
shadowing, so a bare one would collide with every `index` a program declares.

**Counting starts at 1.**  `index of bank` is 1 on the first iteration, and
`item 1 in banks` is the first item.  There is no pointer arithmetic and no C
legacy to stay consistent with, and exact-numbers-by-default has already
rejected "match what the machine does" as a principle.

The rule that matters more than the number is that there is **one convention,
everywhere the words `index` or `item` appear**.  Anything genuinely
machine-facing that needs 0-based counting — a byte offset into a buffer, an
interop boundary — is called `offset`, and the difference is documented at both.
Two conventions under similar names is the failure to avoid; which end they
start from is a detail.

`index` and `of` are therefore protected: no pattern may use either as glue,
because a pattern that reserved one would make the injected name illegal
wherever it is in scope.
### 4.5.5 Reactive
`when` (*condition* | *name*) *body*
- condition is a ***reference***
- name is ***words***

A ***when*** may be declared at **module scope** or inside a ***type***, and
nowhere else.  Not in a function body, a block, a loop, a delegate, or another
`when`.

A propagation step happens *between* statements rather than during one, so a
`when` inside a scope that closes has two possible lifetimes and both are wrong:
it leaves its scope before any step runs, so it can never fire and the
declaration is dead; or it outlives its scope, so it holds references to locals
that are gone.  There is no third option, which is why the restriction costs
nothing.

It is also what lets the lifetime rule be stated whole: a module `when` lives as
long as the module, and a type `when` as long as the instance.

A `when` inside a ***type*** is **designed and not implemented**.  Writing one
is refused by name rather than as a syntax error.  What it waits on is the
instance binding model, which is now decided:

> **One cell per declared member, holding N values.  Not one node per instance.**

The reason is not the benchmark, though there is one — per-instance scalar nodes
ran about twenty times slower on a simulation workload, and the cost was
edge-chasing and cache behaviour rather than arithmetic, so it does not come
back with tuning.  That is corroboration.  The argument is:

> Under grouped storage the dependency graph is the size of the **source text**.
> Under per-instance nodes it is the size of the **world**.

Everything downstream inherits that — edge counts, dirty propagation, cascade
analysis, the SCC check, the cutoff comparison, and any diagnostic that names a
node all scale with how much code was written rather than how much data exists.
That holds at twelve instances and at a million, so it does not depend on the
benchmark generalising, and it is a *comprehensibility* property before it is a
performance one: the graph a person debugs is the graph they wrote.  At twelve
instances grouped storage wins nothing on speed and costs a little indirection.
It still wins.

What follows from it:

- a type-scope `when` is **one** node, evaluating a predicate across the member
  array and firing its body per instance whose entry changed;
- `stop` clears the caller's bit in the liveness mask, as above;
- an instance identity is an **index** into the member arrays, not a pointer;
- adding or removing an instance is an array operation, and removal wants a free
  list or swap-with-last plus a stable handle table;
- cutoff becomes array-valued and so O(N) per cell — a dirty range or a digest,
  never a full compare;
- adding or removing a *member* is adding or dropping one array rather than
  walking N objects, so live editing gets easier rather than harder.

Three things sit inside the decision rather than reopening it, and none needs
answering yet: **subtypes**, where members only some instances have make the
arrays ragged and the answer is one array set per concrete type; **sparse
firing**, where a predicate over N entries is wasteful when three are armed and
wants a dirty list rather than the mask, since reading the mask still scans; and
**references between instances**, where a member holding a reference stores an
index and a polymorphic one needs a type beside it.

When instances are built, this decision is pinned by a test rather than a
comment, because a comment does not survive an optimisation pass: *create N
instances of a type with M members and one type-scope `when`; assert the graph's
node count is a function of M alone and is unchanged between N = 1 and
N = 1000.*  The failure mode to watch for is archetype count growing with
instance count rather than with type count.

#### `stop`

`stop` is legal only in a `when` body, takes effect at the end of the round, and
**removes the node** rather than disabling it: a stopped `when` that lingers
still costs an edge walk and still counts toward cascades, which is the leak the
placement rule above exists to prevent.

Because it can only *shrink* the graph it cannot make a legal program illegal,
so cascade analysis over the never-stops graph stays sound and needs no dynamic
counterpart.

**Every `when` carries a liveness mask, and module scope is the one-element
case.**  `stop` clears the caller's bit; the node is removed when the mask
empties.  This is what makes `stop` mean the same thing in both scopes: under
one cell per member there is a *single* node evaluating a predicate across every
instance, so removing it on `stop` would stop the behaviour for all of them —
and the instance that breaks is not the one whose code ran.  Neither `stop` nor
the mask is built; the rule is recorded here so that whoever writes `stop` first
does not have to rediscover it.

## 4.6 Aggregates
A collection of zero or more specific syntax separated by a given delimiter.  The sequence cannot be ended by the delimiter unless otherwise specified.
### 4.6.1 Definition
`{` (***statement***`;`)* `}`
- a ***statement*** whose last token is `}` needs no `;`, and neither does the
  last statement before the closing `}`

The elision is what makes `function f { if x { return 1; } return 2; }` — a
block followed by another statement, which is most programs — read the way it
looks.  A `;` there is permitted and means the same thing.  The elision is
scoped to statement sequences: a list or a lookup still needs its commas, so
`{ { 1 } { 2 } }` is two values with no separator and is refused.

A statement takes a `;` unless its last token is `}`, or it is the last thing in
the file.  **This is one rule and it holds at every level** — the top of a file
is a statement sequence exactly as a braced definition is, so `1 2;` is refused
in both and `function f {} var second = 2;` is accepted in both.  Moving a
statement into or out of a block does not change whether it is legal.

**Statement boundaries are structural, not resolved.**  A block is split into
elements on `;` and on `}` before anything is resolved.  The resolver is then
handed one element and either resolves it or fails; it never joins two or
splits one.  Without that, how many statements a program has would depend on
what names are in scope, which is a worse property than any single misreading.

So `return 1 return 2;` is one element and not two, and it is one the resolver
refuses — there is no juxtaposition rule that would let `1 return 2` be an
expression.  `return return 1` does resolve, because `return` takes an
expression and a `return` is one.
### 4.6.2 Inputs
`(` (***value***|***assignment***`,`)* `)`
### 4.6.3 List
`{` (***value***`,`)* `}`
### 4.6.4 Lookup
`{` (***value***`=`***value***`,`)* `}`
### 4.6.5 Indexer
`[` (***value***`,`)+ `]`
### 4.6.6 Parameters
`(` (***datum declaration***`,`)* `)`
- declarators for each parameter can only be blank, `var` or `let`
## 4.7 Reference
A sequence of ***component***s, each a ***words***, an ***anonymous value***, or
a ***symbol***.  What may lead it decides what may follow.

**A ***words*** may be followed by anything.**  An anonymous value after a word
is an *argument*, so `thing 7 ("stuff")`, `f (1) (2)` and `f [0] [1]` are each
one reference, and `x > 3` is one too.

**An ***anonymous value*** may lead, and then only two things may follow it:**

- an ***indexer***, which attaches to the value — `{ 1, 2 } [0]`; or
- a ***symbol***, which takes what has been built so far as its left operand and
  continues the expression — `3..test`, `3 + 4`.

**These compose.**  An indexer attaches to a value and the result is a value, so
another indexer may attach to that, and a symbol may take the whole of it:
`{ 1, 2 } [0] + 3` and `{ 1, 2 } [0] [1] + 3` are each one reference.

Anything else after a leading anonymous value is a *second* value, and two
values side by side need the separator §4.6 asks for: `{ 1 } { 2 }` is refused,
and so is `{ 1 } { 2 } name` — a trailing word does not buy the missing comma.

An anonymous value **alone** is not a reference.  It is a value, and §4.9 makes
it a statement.

That is also why `(x) => { … } (1)` is not immediate application: an input block
is neither an indexer nor a symbol, so the delegate ends the reference and the
input begins a new statement.  Whether the language wants immediate application
is open; today it does not have it, and a source that looks like it is a
sequence of statements each legal on its own.
## 4.8 Anonymous value
Can be ***inline value***, ***delegate***, ***lookup***, ***list***, ***inputs***, or ***indexer***.
### 4.8.1 Inline value
One or more ***literal***s
### 4.8.2 Delegate
(***name*** | ***delegate parameters***) `=>` *body*
- delegate parameters is `(` ((***datum declaration*** | ***name***) `,`)* `)`
- body is a ***definition***

The grouping matters: whichever alternative is chosen owns the arrow and the
body.  A single untyped parameter needs no brackets — `x => { … }` — and types
go *inside* the brackets: `(x => Number) => { … }`.  A bare typed declaration is
not a delegate, so `x => Number => { … }` is not one either.

**Reading a zero-argument delegate invokes it.**  There is no call syntax; a
delegate is read like any other name.  Anything else would reintroduce `ping()`
at the call site, which is the exact spelling `function ping ()` is refused for
— so a language that refuses the declaration cannot accept the call.

That makes a zero-argument delegate a deferred computation evaluated on read,
which is a `let` that can be passed around.  Two questions follow and neither is
settled: whether **higher-order cells** are permitted at all, and whether a
first-class computation is distinct enough from a `let` to want both.  See
`FAILUREMODES.md` §6 — `() => …` being well formed puts them in scope whether or
not those decisions are taken.
## 4.9 Statements
An expression of programmer intent.  All are completed with either ***punctuation*** or the end of file.  ***Reference***s and ***anonymous value***s are also considered statements.
### 4.9.1 Export
`part of` *name*
- name is ***words***
### 4.9.2 Import
`import` (*name* | *url*) (`as` *identifier*)?
- name is ***words***
- identifier is ***words***
### 4.9.3 Assignment
*name* `=` | `+=` | `-=` | `*=` | `/=` | `&=` | `|=` *value*
- name is a ***reference***
- value is a ***value***
## 4.10 Alias
`alias` *name* `=` *original*
- name is ***words***
- original is ***words***
## 4.11 Trivium
(***whitespace*** | ***comment***)+
## 4.12 Unknown
Any sequence of tokens which does not match any other syntax