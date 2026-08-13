#!/usr/bin/env python3
"""
tail_sugar.py -- «{ x }» as sugar for «{ return x; }»: is the determination total?

Budai's ruling, plus: no «action» keyword, and a function with no return is an
action. Both accepted. What this checks is the thing a ruling of this shape can
get wrong -- a block whose meaning is undetermined, or determined differently in
two contexts that look alike.

Enumerated: every last-statement shape against every block context. The rule
under test:

    in a FUNCTION BODY, if the final statement is an expression whose type is not
    the action type, it is «return <that expression>». Otherwise the body has no
    return site and the function is an action.
"""

W = 78

# (label, the type the final statement produces, is it an expression at all)
LASTS = [
    ('«x» -- a name',                   'value',  True),
    ('«x + 1»',                         'value',  True),
    ('«if c { 1 } otherwise { 2 }»',    'value',  True),
    ('«print x» -- an action call',     'action', True),
    ('«return x»',                      'return-value', False),
    ('«return» -- bare',                'return-none',  False),
    ('«var y = 3;»  a declaration',     'none',   False),
    ('nothing -- an empty block',       'empty',  False),
]

CONTEXTS = ['function body', 'when body', 'if branch']


def rule(context, kind):
    if context == 'function body':
        if kind == 'value':        return ('sugars', 'return that value')
        if kind == 'action':       return ('no', 'a call, then the body ends -> ACTION')
        if kind == 'return-value': return ('no', 'already a return site')
        if kind == 'return-none':  return ('no', 'bare return -> ACTION')
        if kind in ('none', 'empty'):
            return ('no', 'no answer anywhere -> ACTION')
    if context == 'when body':
        if kind == 'value':        return ('NO', 'a when never answers -> value discarded, WARN')
        if kind == 'action':       return ('no', 'ordinary last statement')
        if kind == 'return-value': return ('no', 'refused -- a when has nobody to answer')
        if kind == 'return-none':  return ('no', 'ends this firing')
        return ('no', 'ordinary')
    # an if branch is ALREADY an expression -- IF-AS-EXPRESSION.md
    if kind == 'value':            return ('sugars', 'already the branch value today')
    if kind == 'action':           return ('no', 'the branch answers nothing')
    if kind == 'return-value':     return ('no', 'returns from the enclosing FUNCTION')
    if kind == 'return-none':      return ('no', 'returns from the enclosing body')
    return ('no', 'the branch answers nothing')


print('=' * W)
print('Every last-statement shape, in every block context')
print('=' * W)
undetermined = 0
for ctx in CONTEXTS:
    print(f'  {ctx.upper()}')
    for label, kind, _ in LASTS:
        verdict, why = rule(ctx, kind)
        if verdict is None:
            undetermined += 1
        print(f'    {label:32} {verdict:>8}   {why}')
    print()

print(f'''  undetermined cases: {undetermined}

  The determination is total, and the reason it is total is that the ACTION TYPE
  is not admissible in a value position (FIVE-RULINGS §2b). «print x» cannot be
  returned, so the sugar simply does not reach it -- no extra rule, no guess.''')

print('=' * W)
print('The argument that makes this a correction rather than an addition')
print('=' * W)
print('''  «if c { a } otherwise { b }» is an EXPRESSION (IF-AS-EXPRESSION.md), so
  «{ a }» in that position ALREADY means "the value a". Without this ruling,
  «{ x }» means one thing inside an «if» and a different thing inside a function
  body -- which is the kind of near-miss a reader has to hold in their head.

  So the sugar is not a convenience bolted on; it makes a block mean one thing
  everywhere it appears. That is a stronger argument than brevity, and it is the
  one to put in the guide.''')

print()
print('=' * W)
print('Three guards the rule needs, and one style line')
print('=' * W)
print('''  1. ONLY THE FINAL statement sugars. A bare value expression EARLIER in a
     body computes something and throws it away -- almost always a mistake, and
     silence there means someone writes «x» in the middle intending a return and
     gets nothing. Ephemeral warning, the class Budai proposed for the
     single-variable case.

  2. A «when» BODY DOES NOT SUGAR. A when never answers, so a trailing value
     there is discarded -- the same warning, for the same reason. Without this
     guard the sugar would produce «return x» in a when, which
     RETURN-AND-LITERALS §1b refuses, and the author would get a message about a
     «return» they did not write.

  3. A TRAILING TERMINATOR DOES NOT DISABLE IT. «{ x; }» sugars exactly as
     «{ x }» does. Aggregate.Parsed already treats a trailing separator as
     elision rather than as an empty statement, so this only needs saying, not
     building -- but it needs saying, or it gets discovered.

  STYLE, one line for the guide:

      the sugar is for the ANSWER; «return» is for an EARLY exit.

  That makes the two forms non-competing instead of two ways to write the same
  thing, which is what stops an idiom war before it starts.''')
