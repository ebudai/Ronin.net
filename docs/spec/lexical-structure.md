# 2 Lexical Structure
A *token* consists of a type and a sequence of unicode characters which satisfy its constraints, called its *representation*.  The *lexicon* is the set of all token types and consists of the following:
## 2.1 Trivium
Tokens which do not participate in grammatical analysis.
- **comment**
    - single line `\\` (any character except `eol`)* `eol` or `eof`
    - multiline `/*` (any character)* `*/`
        - multiline comments may be nested provided they are balanced
- **whitespace**
    - any character for which the unicode standard considers whitespace (see https://unicode.org/reports/tr44/#White_Space)    
## 2.2 Keyword
Instances of a specific ***word*** which has pre-defined meaning by the compiler.  They are all intended to assist in grammatical and semantic analysis.
- `alias`
- `compiled`
- `constant`
- `datatype`
- `export`
- `extends`
- `for each`
- `function`
- `import`
- `let`
- `optional`
- `part of`
- `var`
- `when`
## 2.3 Literal
A ***constant*** value specified directly in source code.
- **lexicographic**
    - unicode character values
        - `'` any character `'`
        - `'\u` | `'\U` four digits `'`
        - `'\''` for the `'` character literal
    - text values
        - `"` (any character except `"`) `"`
        - *escape sequence* is `\` (any character) | `{{` | `}}`
        - *interpolator* is defined as `{` (any character except (`{` | `}`))+ `}`
        - text values may contain zero or more escape sequences and interpolators        
- **currency**
    - `$` positive ***numeric*** value
    - `$` may be replaced by any symbol the unicode standard considers a currency symbol (see https://www.unicode.org/charts/beta/nameslist/n_20A0.html)
    - values may be prefixed by `-`
- **numeric**
    - can consist of any character sequence which is parsable by the operating system's current locale/culture/region, including grouping symbols
- **temporal**
    - date values
        - (four digits | a number >= 1000) `-` two digits `-` two digits
    - time of day values
        - two digits `:` two digits `:` two digits (`a` | `am` | `A` | `AM` | `p` | `pm` | `P` | `PM`)?
    - moment in time values
        - ***date*** followed by ***time*** and possible *timezone acronym*
            - for timezones see https://www.iana.org/time-zones or https://en.wikipedia.org/wiki/List_of_tz_database_time_zones
- **url**
    - (`http` | `https` | `git`) `://` *domain* (`@` *version*)?
    - **domain** conforms to *IDNA2008* (see https://www.rfc-editor.org/info/rfc5892)
    - as the terminal `;` is a valid character in the **domain**, if the value of the url ends with `;`, it is not considered part of the **domain** and is instead interpreted as a statement terminator.
    - **version** is interpreted as *semver* with wildcards as default values.  Omitting version will default to the latest available.
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
    - `[` start ordinal
    - `{` start scope
    - `(` start values
    - `]` end ordinal
    - `}` end scope
    - `)` end values
## 2.5 Word
A type of token for which no other conforms.