# The Ronin Programming Language

Ronin's purpose is to implement game logic, user interfaces, and modifications thereof for the Omniverse Game Engine.  It is a weakly-typed, reactive language.

## Goals
- readable
- composable
- nothing locked-down or hidden
- performant
- easy to learn
- "you don't need to worry about it" - minimize extraneous details
- hot-reloadable

## Non-goals
- general purpose
- satisfying any particular programming paradigm

## Overview
A Ronin **program** consists of one or more **modules** which contain one or more **scopes** and a **name**.  Each scope contains one or more **statements**.  A statement may be an **assignment**, **member declaration**, or a 
## Concepts

### Data
Data can be imperative, which does not change until explicitly set to a value, or reactive, which will automatically change when one of its dependent values changes. All data is typed, either explicitly via `=> identifier` or implicitly via `= some value`.

Imperative data is declared via
- `var identifier => [modifiers] type [= initial state];` or
- `var identifier [=> modifiers] = initial state;`

Reactive data is declared via 
- `let identifier [=> [modifiers] type] = value;` (early binding) or
- `var identifier => [modifiers] reactive type;` (late binding)

Datum identifiers may not contain parameters.

Data is written to via `set identifier = new value;`.

#### Modifiers
- `compiled` - causes the variable to be computed at compile time.
- `global` - when applied to a member variable, this causes it to be accessed via the type's identifier rather than the variable's identifier.  The value is shared between all instances of the type.  When applied to a local variable of a function, it can be accessed without invoking the function, and does not get cleaned up on scope exit.
- `optional` - allows the variable to be set to the special value of `nothing`.
- `hidden` - hides the member from code completion unless requested
- `reactive` - causes the variable to be late-bound reactive

### Types
Types describe the shape and behaviour of data.

Types are declared via
- `type identifier [= algebra] { member+ }` or
- `type identifier = algebra;`

#### Primary Types
There are four primary types which all built-in and user-defined types are composed of.  They are:
- `number` - can store integers, reals, positive, negative, infinite or undefined.  Underlying type is determined at compile time by examining usage.  If you require integer division, specify `whole number`.  If you require arbitrary precision above 64 bits, specify `large number` or `large whole number`.
- `text` - UTF8 character list
- `date` - year-month-day, with 57 bytes for the year, and the remaining 7 for the day of the year (1 to 360)
- `true/false`

#### Members
All types have at least one member, which can be a variable, a function, or a type.  Member functions have an implicit parameter prepended to the identifier, of the type of the enclosing type, called `me`.  This implicit parameter does not have to be referred to directly, and is automatically prefixed on every reference in the member function.

#### Algebra
Algebraic types are supported via `and` and `or`.  Sum types are discriminated.

#### Extensions
All types may be extended, meaning having new members added, member functions overridden, and member types extended.  Types can be extended via
- `extend identifier { members+ }`
Algebra may not be altered.  Data may not be altered.  Nothing can be deleted.

#### Initialization

### Functions
Functions are scopes with an identifier.

### Identifiers
Identifiers can contain zero or more ***names*** and zero or more ***parameters*** in any order.  An identifier must contain at least one of either of these.

#### **Names**
Names can contain one or more words, symbols, or numerals, except ***punctuation***, defined as any one of `=> ( [ { ) ] } , ; " = ?`
Names are separated by whitespace, but whitespace between words and symbols may be elided.  Names cannot start with numerals.

#### **Parameters**
Parameters are one or more comma-delimited identifiers surrounded by `(`brackets`)`.

##### Examples:
- `fastest horse`
- `write (book contents => text) to (library => Library)`
- `restricted list (things => T, T = things type)`

Two identifiers are equivalent if they have the same names and the same parameters with the same types.  If only one or more type differs, the identifiers are ***overloaded***.

### Statements
Statements contain references which refer to one identifier belonging to a member.  In order for an identifier to match a given reference, all names must be the same, and all parameters must be accounted for.

### Contexts
Contexts contain a parent context, as well as a list of members, which can be data, functions, and/or types.  The two types of contexts are ***scopes*** and ***modules***.

#### Scopes
Scopes are surrounded by `{}` and contain a list of statements separated by `;`.  These statements are resolved in the order they are written when the scope is resolved.

If a statement begins with the keyword `return`, or it is the last statement in a scope, the scope resolves to the resolution of that statement.  If the scope's resolved value is not assigned, its parent also resolves to that value, and so on until assigned.  If no assignment takes place, the program concludes and the returned value is printed.

#### Modules
Modules are named lists of scopes.  They may also contain one or modules, provided the child module's name starts with its parent's name.  Any scope may join a module by stating it is `part of module name`.  The order by which scopes are added to a module is not defined.

#### Global Module
There exists one and only one global module in each program.  The global module is visible from all scopes.  The global module does not have a parent context.
