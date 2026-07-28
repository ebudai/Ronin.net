# 4 Grammatical Structure
***Syntax*** is defined as an ordered grouping of specific ***token***s.
## 4.1 Mutability
One of `var`, `constant`, or `let`
## 4.2 Modifier
One of `compiled`, `optional`, `shared`, `persistent`, `export`, or `extends`
## 4.3 Name
Sequence of one or more ***word***s or ***symbol***s which are not ***punctuation***.
## 4.4 Identifier
Sequence of one or more ***name***s or ***parameters***.
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
- loop variable is a ***words***
- collection is a ***reference***
- body is a ***definition***

`in` is a reserved word and may not appear in any name.  A multi-word name
containing it would make the split point in a loop header ambiguous — and the
competing readings do not tie, so nothing would report it.
### 4.5.5 Reactive
`when` (*condition* | *name*) *body*
- condition is a ***reference***
- name is ***words***
## 4.6 Aggregates
A collection of zero or more specific syntax separated by a given delimiter.  The sequence cannot be ended by the delimiter unless otherwise specified.
### 4.6.1 Definition
`{` (***statement***`;`)* `}`
- ***statement*** sequence must be ended by `;`
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