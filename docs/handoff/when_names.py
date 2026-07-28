#!/usr/bin/env python3
"""
Two decisions the programmer needs.

A. `constant` at runtime -- and whether it gets an `old` shadow.
B. `when` has no name, and every diagnostic that matters needs one.

B is the interesting one: the answer requires no syntax at all.
"""

W = 74
def h(t): print('\n' + '=' * W + f'\n{t}\n' + '=' * W)

# =====================================================================
h('A.  `constant` is not a node')

print('''  The suggestion was "a Var that refuses writes after initialisation".
  That works, but it buys a check and keeps a cost that isn't needed.

  A constant never changes, so it can never mark anything dirty. Every
  dependency edge into a constant is an edge that can never fire -- memory
  held, and marking work done, for an event that is impossible.

  DECISION: a constant is not a graph node. It is a symbol table entry
  holding a value, evaluated once during initialisation and thereafter
  indistinguishable from a literal.

    - no write path exists, so there is nothing to refuse
    - reading it creates NO dependency edge
    - it never appears in a dirty set, a ring, or a propagation step

  In a UI or game program -- colours, tuning values, layout metrics, string
  tables -- constants are numerous and read constantly. Edges into them would
  be most of the graph and none of the behaviour.''')

h('A2. and therefore: no `old` shadow')
print('''  `old x` is defined as the previous generation's value. For a constant that
  is provably the current value, so `old pi` is not merely useless -- it is a
  synonym that LOOKS like it means something.

  DECISION: constants get no injection. `old pi` is then an unresolved name,
  and the diagnostic can say why:

      no name «old pi» in scope.
      «pi» is a constant, so it has no previous value -- use «pi».

  Which is better than resolving it silently and leaving the reader to wonder
  why the author wrote it.''')

h('A3. two consequences worth deciding now')
print('''  INITIALISATION ORDER. If a constant's initialiser reads another constant,
  they must be evaluated in dependency order, and a cycle among constant
  initialisers is an error. The `cycles()` detector already written works
  unchanged -- same shape, different node set.

  SNAPSHOT CAPTURE. `constant initial health = health;` reads a var and
  freezes it. That is legitimate but it makes initialisation order OBSERVABLE,
  which is the static-init-order trap every language with this feature has
  fallen into. Worth a warning at declaration: "«initial health» captures a
  snapshot of «health» at initialisation; its value depends on init order."''')

# =====================================================================
h('B.  `when` names: render the trigger, add no syntax')

WHENS = [
    dict(trigger='health changes',            writes={'is alive', 'log'},   reads={'health'}),
    dict(trigger='is alive becomes false',    writes={'respawn timer'},      reads={'is alive'}),
    dict(trigger='respawn timer reaches zero', writes={'health'},            reads={'respawn timer'}),
    dict(trigger='box sizes change',          writes={'box sizes'},          reads={'box sizes'}),
    dict(trigger='temperature changes',       writes={'temperature'},        reads={'temperature'}),
]

print('''  The doc's examples -- «on damage», «on death» -- were invented names, and
  that was misleading of me. The real question is what a synthesised name can
  be built from. Position is unreadable. But the TRIGGER already describes the
  event, in the programmer's own words:''')

print('\n  position-derived:')
print('    when@Player.cs:42 -> when@Player.cs:57 -> when@Respawn.cs:12 -> when@Player.cs:42')
print('\n  trigger-derived:')
print('    when health changes')
print('      -> when is alive becomes false')
print('      -> when respawn timer reaches zero')
print('      -> when health changes')

print('''
  The second is not a fallback for the first -- it is better than the invented
  names were. «on damage» asks you to trust that damage is what changes
  health; «when health changes» says so. And it is greppable, because it is
  literally the source text.

  DECISION: a when's name IS its trigger's source text. No new syntax, no
  ceremony, no naming burden on code that does not need one.

  The parser must therefore keep the trigger's SOURCE SPAN, not only its AST.
  That is the entire implementation cost -- one span per when.''')

h('B2. the three rules it needs')

print('''  MODE IS PART OF THE NAME. «when x > 6» and «when x changes» are different
  events on the same value, and the trigger text distinguishes them already
  because the mode is written in the source. Render the source, not the AST.

  DUPLICATES DISAMBIGUATE BY SCOPE, THEN ORDINAL. Two whens with identical
  trigger text in one scope is legal and rare:''')

print('''
      when health changes            (in type Player)
      when health changes            (in type Enemy)        <- scope suffices
      when health changes #2         (in type Player)       <- ordinal, rare''')

print('''
  TRUNCATE IN THE MIDDLE, NOT THE END. A long trigger keeps both ends, which
  are the informative parts:''')

long_trigger = ('player health is below critical threshold and shield is down '
                'and revive charges remaining is zero')
def elide(text, width=52):
    if len(text) <= width:
        return text
    keep = (width - 5) // 2
    return f'{text[:keep]} ... {text[-keep:]}'

print(f'\n      full:      when {long_trigger}')
print(f'      rendered:  when {elide(long_trigger)}')
print('''
  Full text stays available for the IDE hover and the long-form error.''')

h('B3. what this fixes')
print('''  Graph.When(name, ...)     name comes from the trigger span; no caller
                            has to invent one

  runaway message           "last fired: «when temperature changes»" names
                            the culprit AND its condition in one string

  tier 1 rings              readable without anyone having labelled
                            anything, which was the requirement

  And the good news you found stands: both trigger modes already exist in the
  grammar as Scope.ConditionalReactive and Scope.Reactive. Nothing needs to be
  parsed differently -- only remembered.''')
