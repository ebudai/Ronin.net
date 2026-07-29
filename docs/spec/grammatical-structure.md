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

A **bracket in a declaration marks one argument**, not a parameter list — Ronin
has no parameter lists.  `send (message) to (recipient)` is called `send x to
y`, so `(message)` is one hole with one name.  `()` is therefore a hole with no
name and is refused: a function that takes nothing is declared `function ping`,
which is what `ping` is called.

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
One or more ***words*** or ***anonymous value*** + optional ***indexer***
## 4.8 Anonymous value
Can be ***inline value***, ***delegate***, ***lookup***, ***list***, ***inputs***, or ***indexer***.
### 4.8.1 Inline value
One or more ***literal***s
### 4.8.2 Delegate
***datum declaration*** | ***parameters*** `=>` *body*
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