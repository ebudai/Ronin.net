# 2 Lexical Structure
A *token* consists of a type and a sequence of unicode characters which satisfy its constraints, called its *representation*.  The *lexicon* is the set of all token types and consists of the following:
## 2.1 Trivium
Tokens which do not participate in grammatical analysis.
- **comment**
    - single line `\\` (any character except `eol`)* `eol` or `eof`
    - multiline `/*` (any character)* `*/`
- **whitespace**
    - any character for which the unicode standard considers whitespace (see https://unicode.org/reports/tr44/#White_Space)    
## 2.2 Keyword
Instances of a specific ***word*** which has pre-defined meaning by the compiler.  They are all intended to assist in grammatical and semantic analysis.
- `alias`
- `compiled`
- `constant`
- `datatype`
- `export`
- `extend`
- `for each`
- `function`
- `import`
- `let`
- `optional`
- `override`
- `part of`
- `var`
- `when`
## 2.3 Literal
A ***constant*** value specified directly in source code.
- **lexicographic**
    - unicode character values
        - `'` any character `'`
        - `'\u` four digits `'`
        - `'\U` six digits `'`
        - `'\x'` where `x` is any one character
    - text values
        - `"` (any character except `"` unless preceeded by `\`)* `"`
- **currency**
    - `-`? `$` ***numeric*** value
    - `$` may be replaced by any symbol the unicode standard considers a currency symbol (see https://www.unicode.org/charts/beta/nameslist/n_20A0.html)
- **numeric**
    - `-`? digit+`.`digit+
    - the first block of *digit* may contain `,` separating groups of three digits, except the left-most group
- **temporal**
    - date values
        - (four digits | a ***number*** >= 1000) `-` two digits `-` two digits
    - time of day values
        - one or 
        two digits (`:` two digits)? (`:` two digits)? (`a` | `am` | `A` | `AM` | `p` | `pm` | `P` | `PM`)?
    - moment in time values
        - ***date*** followed by ***time*** and possible *timezone acronym*
- **url**
    - (`http` | `https` | `git`) `://` *domain* (`@` *version*)?    
## 2.4 Symbol
A single non-alphabetic character, in addition to some specific character sequences called *compound symbol*s.  Some symbols are classified as *puncutation*, which participate in grammatical analysis.
- **compound**
    - `..` interval
    - `+=` add assign
    - `&=` and assign    
    - `/=` divide assign
    - `-=` subtract assign
    - `*=` multiply assign
    - `|=` or assign
    - `=>` returns
- **punctuation**
    - `=` assign
    - `+=` add assign
    - `&=` and assign    
    - `/=` divide assign
    - `-=` subtract assign
    - `*=` multiply assign
    - `|=` or assign
    - `=>` returns
    - `,` separator    
    - `;` terminal
    - `"` text delimiter
    - `[` start indexer
    - `{` start scope
    - `(` start values
    - `]` end indexer
    - `}` end scope
    - `)` end values
## 2.5 Word
A type of token for which no other conforms.