# 3 Lexical Analysis

The *lexer* transforms an ordered sequence of unicode characters into an ordered sequence of ***token***s.  Every character in the lexer's input must be represented in one and only one token of its output.  The lexer may not produce errors.

To produce the required transformation, the lexer iterates through every character in the input sequence, appending it to a buffer.  When the buffer satisfies all requirements for a type of token, a token of that type is created with representation set to the contents of the buffer and then appended to the lexer's output.  Once completed, the buffer is cleared and the process repeated until there are no more characters to consider.

The lexer uses the following order of token types in its attempt to satisfy one of their requirements:

1. ***whitespace***
1. ***comment***
1. ***literal***
    1. ***character***
    1. ***date***
    1. ***time***
    1. ***currency***
    1. ***numeric***
    1. ***text***
    1. ***url***
1. ***symbol***
    1. *interval*
    1. ***punctuation***
        1. *returns*
        1. *assign*
        1. *add assign*
        1. *and assign*
        1. *divide assign*
        1. *multiply assign*
        1. *or assign*
        1. *subtract assign*
        1. *end ordinal*
        1. *end scope*
        1. *end values*
        1. *separator*
        1. *start ordinal*
        1. *start scope*
        1. *start values*
        1. *terminal*
        1. *text delimiter*    
    1. *character delimiter*
1. ***keyword***
1. ***word***