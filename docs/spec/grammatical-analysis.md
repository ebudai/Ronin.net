# 5 Grammatical Analysis

The *parser* transforms an ordered sequence of ***token***s into an ordered sequence of ***syntax*** elements.  Every token in the parser's input must be represented in one and only one syntax element of its output.  The parser may not produce errors.

To produce the required transformation, the parser validates each type of syntax element against the token sequence, starting at the current point (which starts at the beginning of the token sequence).  If a type of syntax element's constraints are satisfied, a syntax element of that type is generated and appended to the parser's output.  The tokens are consumed, so the current point becomes the first token which does not participate in the generation of the syntax element.  This process repeats until there are no tokens remaining in the sequence to be consumed.

The parser uses the following order of syntax element types in its attempt to satisfy one of their requirements:

1. ***trivium***
1. ***alias***
1. ***export***
1. ***import***
1. ***function declaration***
1. ***datatype declaration***
1. ***assignment***
1. ***reference***
1. ***inline value***
1. ***delegate***
1. ***inputs***
1. ***collection***
    - one production for a ***list*** and a ***lookup***, whose kind is decided
      after every entry is parsed rather than by which alternative was tried
      first
1. ***datum declaration***
1. ***anonymous scope***
1. ***conditional scope***
1. ***repeating scope***
1. ***iterating scope***
1. ***reactive scope***
1. ***unknown syntax***