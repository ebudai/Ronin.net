# 3 Grammatical Structure
***Syntax*** is defined as an ordered grouping of specific ***token***s.
## 3.1 Name
Sequence of one or more ***words*** or ***parameters***.
### 3.1.1 Words
Sequence of one or more ***word***s or ***symbol***s that are not ***punctuation***.
## 3.2 Declaration
### 3.2.1 Datum
(`var` | `constant` | `let`)? *identifier* (`=>` *modifiers** *datatype*)? (`=` *initializer*)?
- identifier is ***words***
- modifiers is `compiled` | `optional` | `shared` | `persistent`
- datatype is a ***reference***
- initializer is a ***value***
### 3.2.2 Function
*modifiers** `function` *identifier* (`=>` `optional`? *datatype*)? (*body*|`;`)
- modifiers is `export` | `shared`
- identifier is a ***name***
- datatype is a ***reference***
- body is a ***definition***
### 3.2.3 Datatype
`extends`? `datatype` *identifier* (`=` *algebra*) *body*
- identifier is a ***name***
- algebra is a ***reference***
- body is a ***definition***
## 3.3 Scope
Scopes may not be preceeded by an ***assignment***.  All scopes may be preceeded by `compiled`.
### 3.3.1 Anonymous
`export`? *body*
- body is a ***definition***
### 3.3.2 Conditional
`if` *condition* *body*
- condition is a ***refrence***
- body is a ***definition***
### 3.3.3 Repeating
`while` *condition* *body*
- condition is a ***reference***
- body is a ***definition***
### 3.3.4 Iterating
`for each` *loop variable* `in` *body*
- loop variable is a ***words***
- body is a ***definition***
### 3.3.5 Reactive
`when` (*condition* | *name*) *body*
- condition is a ***reference***
- name is ***words***
## 3.4 Aggregates
A collection of zero or more specific syntax separated by a given delimiter.  The sequence cannot be ended by the delimiter unless otherwise specified.
### 3.4.1 Definition
`{` (***statement***`;`)* `}`
- ***statement*** sequence must be ended by `;`
### 3.4.2 Inputs
`(` (***value***|***assignment***`,`)* `)`
### 3.4.3 List
`{` (***value***`,`)* `}`
### 3.4.4 Lookup
`{` (***value***`=`***value***`,`)* `}`
### 3.4.5 Ordinal
`[` (***value***`,`)+ `]`
### 3.4.6 Parameters
`(` (***datum declaration***`,`)* `)`
- declarators for each parameter can only be blank, `var` or `let`
## 3.5 Reference
One or more ***words*** or ***anonymous value*** + optional ***ordinal***
## 3.6 Anonymous value
Can be ***inline value***, ***delegate***, ***lookup***, ***list***, ***inputs***, or ***ordinal***.
### 3.6.1 Inline value
One or more ***literal***s
### 3.6.2 Delegate
***datum declaration*** | ***parameters*** `=>` *body*
## 3.7 Statements
An expression of programmer intent.  All are completed with either ***punctuation*** or the end of file.  ***Reference***s and ***anonymous value***s are also considered statements.
### 3.7.1 Export
`part of` *name*
- name is ***words***
### 3.7.2 Import
`import` (*name* | *url*) (`as` *identifier*)?
- name is ***words***
- identifier is ***words***
### 3.7.3 Assignment
*name* `=` | `+=` | `-=` | `*=` | `/=` | `&=` | `|=` *value*
- name is a ***reference***
- value is a ***value***
## 3.8 Alias
`alias` *name* `=` *original*
- name is ***words***
- original is ***words***
## 3.9 Unknown
Any sequence of tokens which does not match any other syntax