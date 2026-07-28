# The Ronin Programming Language Guide
## Overview
A Ronin program consists of one or more **module**s which contain one or more **scope**s and a **name**.  Each scope contains one or more **statement**s.  Top-level statements are part of an implicit scope, and are run once on that module's import.  Implicit scopes with no name are part of the **global module**.

During compilation, each file is considered independently.  All files in the specified (defaulting to the current) directory and all subdirectories are compiled.  Only files with a .ron extension will be processed directly, though data files can be used as type and data providers.

## Identifiers
Identifiers can contain any number of **names** and **parameter blocks** in any order.  An identifier must contain at least one of either of these.  An identifier must be unique within its own **context** and all ancestor contexts (ie: no shadowing).  If the first component of an identifier is a word, that word cannot be a **keyword**.

### Keywords
`alias`\
`compiled`\
`extend`\
`function`\
`global`\
`if`\
`import`\
`for each`\
`in`\
`hidden`\
`let`\
`optional`\
`part of`\
`reactive`\
`type`\
`var`\
`when`\
`while`

### Names
Names can contain one or more **words**, **symbols** (any character the unicode standard defines as a 'symbol' or 'punctuation'), or numerals (digits 0-9), except **punctuation**, defined as any one of\
 `=> ( [ { ) ] } , ; " = ?`\
Names cannot start with numerals. Names are separated by whitespace, but whitespace between a word and a symbol may be elided.

### Parameter Blocks
Parameters are identifiers followed by a typing and/or an initializer.  Parameter blocks contain zero or more parameters and are surrounded by `(`brackets`)`.  All parameters are bound to an **input** when their identifier is referenced. All parameters are passed by reference unless the caller prefixes the input with `copy`.  References are constant by default; mutable references are specified in the parameter by prefixing `var`.  Note that mutable parameters may also be assigned to, and the assignment remains visible outside the parameter's context.

Parameters may be designated optional by using the modifier `optional` or by specifying an initializer.  Optional parameters do not need to be bound to an input.

Parameters blocks in which one or no parameters are bound may elide the `(`brackets`)`.

Parameters may be bound by name and `=`. Remaining parameters are subsequently bound in dependency order (so one parameter's initializer can refer to another parameter), and left-to-right otherwise.

##### Examples:
`fastest horse!`\
`write (book contents => text) to (library => Library)`\
`generics are supported (things => ?, stuff => things type)`

### Overloading
If two identifiers differ only by parameter types, then those identifiers are considered **overloaded**.  In order for an overload candidate to be considered the correct one, all of its parameters must be the same or closer than all parameters of all other candidates.  The return type of a function participates in overload resolution.

## Data
Data can be **imperative**, which does not change until explicitly set to a value, or **reactive**, which will automatically change when one of its dependent values changes. All data is typed, either explicitly via `=> type` or implicitly via `= some value`.

Imperative data is declared via\
`var identifier => [modifiers] type [= initial state];` or\
`var identifier [=> modifiers] = initial state;`

Reactive data is declared via\
`let identifier [=> [modifiers] type] = value;` (early binding) or\
`var identifier => [modifiers] reactive type;` (late binding)

Datum identifiers may not contain parameters.

Data is assigned via `identifier = new value;`.  For reactive data which (even through multiple reactive variables) is bound to an imperative variable, that can be assigned a new value using `set identifier = new value;`.  Reactive variables which are bound to a function call cannot use `set`.

### Modifiers
`compiled` - causes the variable to be computed at compile time.\
`global` - when applied to a member variable, this causes it to be accessed via the type's identifier rather than the variable's identifier.  The value is shared between all instances of the type.  When applied to a local variable of a function, it can be accessed via a delegate to that function without having to invoke it, and does not get cleaned up when the scope resolves.\
`optional` - allows the variable to be set to the special value of `nothing`.\
`hidden` - hides the member from code completion unless requested\
`reactive` - causes the variable to be late-bound reactive

## Types
Types describe the shape and behaviour of data.

Types are declared via\
`type identifier [= algebra] { members }` or\
`type identifier = algebra;`

### Primary Types
There are four primary types which all built-in and user-defined types are composed of.  They are:\
`number` - can store integers, reals, positive, negative, infinite or undefined.  Underlying type is determined at compile time by examining usage.  If you require integer division, specify `whole number`.  If you require arbitrary precision above 64 bits, specify `large number` or `large whole number`.\
`text` - utf-8\
`date` - year-month-day, with 30 days per month, 12 months per year, and year is between 0 and 2^57\
`true/false`

### Members
All types have at least one member, which can be a ***datum***, a ***function***, or a ***type***.  Member functions have an implicit variable of the type of the enclosing type, called `me`.

### Algebra
Algebraic types are supported via `and` and `or`.  Sum types are discriminated.

### Extensions
All types may be extended, meaning having new members added, member functions overridden, and member types extended.  Types can be extended via `extend identifier { members }`\
The only changes available are extensions on inner types, and overriding functions.

## Functions
Functions are scopes with an identifier, and are declared via\
`function identifier [=> type] { statements }`

The function will not infer the return type unless they all match.  Common product types will be inferred, but sum types will not be implicitly created.  Functions always resolve to a value.  Scopes which fail to explicitly return a value implicitly return `nothing`.  The last statement in a function's scope does not need to be concluded with a terminator `;`, and does not need to be prefixed by `return`.

### Overrides
Functions can be overridden via\
`override identifier [=> type] { statements }`

The identifier and return type of both original and override must match.  Overridden functions have an implicit delegate named `original` which has the definition of the original function.

### Partial Application
Functions and delegates can be created from functions using a type or `?` in place of one or more parameters.  `?` can be applied to one parameter, or the entire parameter block.

##### Example:
Given `function rev (engine => Engine, amount => number) { ... }`, we can create a new function using `function partially applied (amount => number) = rev (my car engine, ?);`, or a delegate using `var partially applied = rev (?, 500);` which can then be called using `partially applied my car engine;`

### Delegates
Delegates are function values which can be assigned to variables.  They are expressed via\
`(parameters*) => { statements* }` or via partial application



## Statements
Statements contain references which refer to one identifier belonging to a member.  In order for an identifier to match a given reference, all names must be the same, and all parameters must be accounted for.  A statement may be an **import**, **export**, **alias**,  **assignment**, **declaration**, **reference**, **scope**, or a **temporary value**.

## Contexts
Contexts contain a parent context, as well as a list of members, which can be data, functions, and/or types.  The two types of contexts are ***scopes*** and ***modules***.

### Modules
A Module is defined as a named lists of **scopes** and child **modules**.\
The order by which scopes are added to a module is not defined.\
A scope may not belong to more than one context.\
Nested modules prefix their parents module's names, ie: if a module `calculus` is declared under `standard math`, the `calculus` module's full name is `standard math calculus`.\
Module identifiers cannot contain parameters.

### Scopes
Scopes are surrounded by `{`braces`}` and contain a list of statements separated by `;`.  These statements are resolved in the order they are written when the scope is resolved.

If a statement begins with the keyword `return`, or it is the last statement in a scope, the scope resolves to the resolution of that statement.  If the scope's resolved value is not assigned, its parent also resolves to that value, and so on until assigned.  If no assignment takes place, the program concludes.

### Exports
Any basic scope which is not assigned to a variable may join any module via\
`part of identifier;`

### Imports
Any scope can use any module via\
`import identifier` or\
`import alias = identifier`

#### Data and Type Providers
Types and data can be imported from sqlite, excel, or csv files by using the filename (without folder) for the identifier.  For excel and sqlite, types will be generated from each table or tab, and and lists of instances of those types will be populated from the data in those tables or tabs.  For csv, the filename (without extension) will be used for the type name.  For excel and csv, there must be a header row.

### Global Module
There exists one and only one global module in each program.  The global module is visible from all scopes.  The global module does not have a parent context.

## Aggregates

### Lists
Lists contain zero or more instances of a particular type, set during declaration.  All lists are dynamically sized.
## Errors
## Types of scopes
### basic
### conditional
### conditional reactive
### iterating
### reactive
