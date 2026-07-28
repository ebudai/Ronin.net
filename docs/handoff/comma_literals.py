#!/usr/bin/env python3
"""
Comma as digit separator, with «a well-formed literal takes precedence».

The rule works, but needs a second clause. Without it, whitespace stops
mattering in the one place a reader relies on it:

    f(1, 234)     <- everyone reads this as two arguments

If «well-formed literal wins» ignores whitespace, that is the number 1234 and
the reader is simply wrong. So:

  RULE 1  A comma is a digit separator only when it sits DIRECTLY between
          digits -- no whitespace on either side.
  RULE 2  Groups must be well formed: first group 1-3 digits, every later
          group exactly 3.
  RULE 3  Take the LONGEST well-formed prefix. What follows is separators.

Rule 1 is what makes the reader right: «1,234» is a number, «1, 234» is a
list, and that is how the two are already written by hand.
"""

import re

DIGIT_RUN = re.compile(r'\d[\d,]*')


def longest_literal(text, i):
    """Return (end_index, digits) for the longest well-formed literal starting
    at i, or (i, None) if the first group alone is all that survives."""
    m = DIGIT_RUN.match(text, i)
    if not m:
        return i, None
    run = m.group()

    # rule 1: a trailing comma is never part of the literal
    run = run.rstrip(',')

    # rule 3: shrink to the longest well-formed prefix
    while run:
        groups = run.split(',')
        if len(groups) == 1:
            # RULE 2 APPLIES ONLY TO SEPARATED RUNS. A bare digit run is always
            # a valid integer -- «2345» must not fail because 2345 > 3 digits.
            # Getting this wrong made «f(1,2345)» lex «2» as a stray symbol.
            return i + len(run), run
        ok = (groups[0] and len(groups[0]) <= 3
              and all(len(g) == 3 for g in groups[1:]))
        if ok:
            return i + len(run), ''.join(groups)
        run = run[:run.rfind(',')]        # drop the last group and retry
    return i, None


def lex(text):
    """Only enough of a lexer to show the rule. Words, numbers, commas,
    brackets."""
    out, i = [], 0
    while i < len(text):
        c = text[i]
        if c.isspace():
            i += 1
            continue
        if c.isdigit():
            end, digits = longest_literal(text, i)
            if digits is not None:
                out.append(('num', digits))
                i = end
                continue
        if c == ',':
            out.append(('sep', ',')); i += 1; continue
        if c in '()':
            out.append(('open' if c == '(' else 'close', c)); i += 1; continue
        m = re.match(r'[A-Za-z_][A-Za-z0-9_]*', text[i:])
        if m:
            out.append(('word', m.group())); i += len(m.group()); continue
        out.append(('sym', c)); i += 1
    return out


def show(toks):
    return ' '.join(v if k != 'num' else f'#{v}' for k, v in toks)


def arity(toks):
    """Count top-level arguments between the outermost brackets."""
    depth, args, seen = 0, 1, False
    for k, v in toks:
        if k == 'open':
            depth += 1
            continue
        if k == 'close':
            depth -= 1
            continue
        if depth == 1:
            if k == 'sep':
                args += 1
            else:
                seen = True
    return args if seen else 0


W = 74
print('=' * W)
print('THE RULE ON A CORPUS')
print('=' * W)
print(f"  {'source':24} {'tokens':26} {'args'}")
print('  ' + '-' * (W - 4))
CASES = [
    'f(1,234)',            # well formed -> ONE number
    'f(1, 234)',           # whitespace -> TWO arguments
    'f(1,23)',             # 2-digit group -> not well formed
    'f(1,2345)',           # 4-digit group -> longest prefix is «1»
    'f(12,345,678)',       # fully well formed
    'f(1,234,56)',         # last group short -> prefix «1,234»
    'f(7,000,876, 2)',     # a number and an argument
    'f(a, 234)',           # word then number -> always two
    'f(1,000,000)',        # the case the feature exists for
    'f(2345)',             # bare run longer than 3 digits -- must be one number
    'f(1000000)',          # unseparated, long
    'f(1 ,234)',           # space BEFORE the comma -- still a separator
]
for src in CASES:
    toks = lex(src)
    print(f'  {src:24} {show(toks):26} {arity(toks)}')

print('\n' + '=' * W)
print('THE ONE RESIDUAL HAZARD')
print('=' * W)
pairs = [('f(count, 234)', 'f(1, 234)', 'spaced: stays two arguments'),
         ('f(count,234)',  'f(1,234)',  'unspaced: TWO becomes ONE')]
for before, after, note in pairs:
    a, b = arity(lex(before)), arity(lex(after))
    flag = '' if a == b else '   <-- ARITY CHANGED'
    print(f'  {before:18} ({a} args)  ->  {after:12} ({b} args)   {note}{flag}')

print('''
  Inlining a constant can change arity, but only in the unspaced form -- and
  the unspaced form is the one a reader already parses as a number. Rule 1
  confines the hazard to code that was already misleading.

  It is also CAUGHT rather than silent: the call now supplies one argument
  where the declaration wants two, so binding fails. Only the message is bad,
  and the lexer can fix that by recording that an alternative tokenisation
  existed:

      «f» expects 2 arguments but the call supplies 1.
      «1,234» was read as a single number (comma is a digit separator).
      If two arguments were meant, write «1, 234».

  That turns the one bad case into a self-explaining one, which is the most
  this rule can be asked to do.''')
