# 2 Lexical Structure

A *token* consists of a type and the sourcecode which satisfies its constraints, called its *representation*.  The *lexicon* consists of the following types of tokens:

## 2.1 Trivium

Tokens which do not participate in grammatical analysis.

- **comment**
    - single line `\\` (any character except `eol`)* `eol`
    - multiline `/*` (any character)* `*/`
        - multiline comments may be nested
- **whitespace**
    - any character for which the unicode standard considers whitespace (see https://unicode.org/reports/tr44/#White_Space)
    
## 2.2 Keyword

Specific ***word***s which have pre-defined meaning by the compiler.  They are all intended to assist in grammatical and semantic analysis.

- `alias`
- `compiled`
- `constant`
- `datatype`
- `extends`
- `for each`
- `function`
- `hidden`
- `import`
- `in`
- `let`
- `optional`
- `part of`
- `var`

## 2.3 Literal

A ***constant*** value specified directly in source code.

- **lexicographic**
    - unicode character values
        - `'` any character `'`
        - `\u` | `\U` four digits `'`
    - text values
        - `"` (any character except `"`) `"`
        - *escape sequence* is defined as (`\` any character) | `{{` | `}}`
        - *interpolator* is defined as `{` (any character)+ `}`
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
        - two digits `:` two digits `:` two digits (`a`|`am`|`A`|`AM`|`p`|`pm`|`P`|`PM`)?
    - moment in time values
        - ***date*** followed by ***time*** and possible *timezone acronym*
- **url**
    - (`http`|`https`|`git`) `://` *domain* (`@` *version*)?
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