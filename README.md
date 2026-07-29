# The Ronin Programming Language
Ronin's purpose is to implement game logic, user interfaces, and mods for the Unnamed Game Engine. 

## Goals
- readable
- easy to learn
- composable
- reactivity without all the boilerplate and ceremony
- nothing locked-down or private
- fast
- minimize extraneous details - "you don't need to worry about it"
- hot-reloadable

## Non-goals
- general purpose
- satisfying any particular programming paradigm

## Hello World
```
print "Hello world!";
```

## Identifiers
Identifiers in Ronin can any number or *words* or *parameter blocks* in any order.

**Words** can contain letters, numbers, or symbols.  They cannot start with a number, and some symbols are reserved (called *punctuation*): `=> ( [ { ) ] } , ; " = ?`. Additionally, there are keywords which identifiers cannot start with:

`compiled`\
`extend`\
`for each`\
`function`\
`global`\
`if`\
`import`\
`hidden`\
`let`\
`optional`\
`part of`\
`reactive`\
`type`\
`var`\
`when`\
`while`

**Parameter Blocks** are surrounded by `(`brackets`)` and contain zero or more *parameters* specified by an identifier, and separated by a comma.  If a parameter has an initializer, or is marked as `optional`, the parameter need not be specified.  If fewer than two parameters are being bound in a particular block, the brackets may be elided.

## Variables
Variables must be declared prior to use.  All variables have a type which cannot change, and is set by the declaration.  The type can be specified directly or by using an expression to initialize it.  

Variables in Ronin can be either imperative or reactive.  Imperative variables are declared using `var` and their value does not change until explicitly set.

```js
var name = "Billy Williamson";
var fastest horse => Horse;
var top speed => Number = 7.2;
var candy choices = [lollypop, hard candy];
var my car => Car = 
(
    speed = 9001,
    colour = red,
    22,
    options = 
    (
        turbo = true,
        greeting = "♪boo bee boo♫",
        heated seats = false,
    )
);
```

Reactive variables are declared using `let` and their value changes whenever any of their dependant values changes.  They can be early- or late-bound.  Late-bound reactive variables cannot be referred to before they are initialized.

```js
let x = y + 7;
let speed = distance * time;
let expensive calculation result = calculate(things, stuff, 7, "the other one");
let late-bound fastest horse => Horse;
```

## Control Flow Statements
Ronin supports both imperative and reactive control flow.

```js
if year > 2025 
{
     print "17";
}
else 
{
    print "12";
};

var slow = if speed < 10 => 3; else => 500;
```

```js
for each shoe in shoes
{
    print line index of shoe;
}
```

```js
while year < 12
{
    print "working" + year;
}
```

```js
when year is 2001
{
    print "turn of the millenium";
}
```

## Functions
Functions can be created via declaration, equality, or partial application.
```js
function save the (species)
{
    species is saved = true;
    return species;
}

function getting stung = save the bees;

function add (other => Number) to 3 = 3 + ?;
```

All functions can be overridden
```js
override add (other => Number) to 3 = 77 + ?;
```

Functions which implicitly return `nothing` can be extended
```js
extend print line (value)
{
    print "printed " + value;
}
```

## Comments
Comments can start with `//` and extend to the end of the line.  Multiline comments are surrounded by `/*` and `*/`.  Multiline comments can be nested.

## Modules
Any implicit or anonymous scope can be declared as belonging to a module.
```
part of standard math calculus;
```

Scopes which join modules are not processed along with their parent scopes.  They are reparented to the module they join.  Imports for reparented scopes are not transitive with other scopes belonging to that same module.

Any module can be imported to any scope.
```js
import standard math calculus;
import matrix math = standard math algebra;
```

## Types
Type names are conventionally Capitalized.  There are four primary types from which all others are constructed:
- `Number`
- `Text`
- `Date`
- `True or False`

There is also the type `Nothing` intended for use with optional variables.  It is a singleton named `nothing`.  There is no `void` type.

`Number` is a signed 64-bit integer, unless it is ever the result of division, in which case the underlying type is a 64-bit float.  This is determined at compile time.  In order to lock the type to a signed 64-bit integer, use `Whole Number`.  To lock the type to a 64-bit float, use `Real Number`.  If you require arbitrary precision, use `Irrational Number`.

`Text` is a UTF-8 string, unless many concatenations are performed, in which case the underlying representation is a rope.  There is no character type.

`Date` has 30 days per month, 12 months per year, and years ranging from 0 to 2^55.  Year 0 is considered the beginning of time.  There is no time-of-day type.

`true or false` is a boolean type.

Types can be user defined:
```js
type Dog(speed => Number, how much fun, nutritional requirements => Food)
{
    var name => Text;
    var running speed = speed;
    let is fun = how much fun >= 2;
    var owners => [Owner];
    var thinks he is people => true or false;
    var location => Location;

    function fetch (the ball)
    {
        location = the park;
        shoes 3 = 4;
        return the ball;
    }
}

var good boy => Dog = (3.2, 8, ());
```

Algebraic data types are supported via `and` and `or`.

```js
type Transformer = Robot and Vehicle;
type Platypus = Mammal and Bird and
{
    var distinct types of venom not found in nature = 3;    
};
var the only real outcomes => Win or Lose;
```