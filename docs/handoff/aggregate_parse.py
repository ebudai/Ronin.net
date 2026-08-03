#!/usr/bin/env python3
"""
aggregate_parse.py -- the shape of the regression they are asking for, and the
diagnostic the ordered-alternative structure cannot produce.

Two parsers for the same toy grammar:

    aggregate := '[' elem (',' elem)* ']'
    elem      := value | NAME '=' value
    value     := NUM | aggregate

ORDERED   try «all elements are associations» (lookup), then «all elements are
          values» (list). A nested body is descended once per alternative, at
          every level -- so a late failure costs 2^depth.

SINGLE    parse each element ONCE as «(NAME '=')? value», then decide kind from
          the first element and check the rest agree. One descent, and the
          mixed case is a real diagnosis rather than a fall-through.

The counter is element-parse attempts, which is the work-count regression the
finding asks for. It is a counter and not a timer on purpose: a wall-clock
assertion is flaky in CI and an absolute count is machine-independent.
"""

W = 78


class Fail(Exception):
    pass


def lex(s):
    return s.replace('[', ' [ ').replace(']', ' ] ').replace(',', ' , ') \
            .replace('=', ' = ').split()


# ------------------------------------------------------------------ ORDERED
class Ordered:
    """Mirrors Temporary.Parse: try Lookup.Parse, then List.Parse. The point
    that makes it exponential is that an association's KEY is itself a value,
    so deciding "is this an association?" requires parsing the key first -- and
    a nested square inside it re-enters Temporary and tries both again."""

    def __init__(self, toks):
        self.t, self.i, self.work = toks, 0, 0

    def parse(self):
        return self.temporary()

    def temporary(self):
        save = self.i
        try:
            return self.aggregate(assoc=True)
        except Fail:
            self.i = save
        return self.aggregate(assoc=False)

    def aggregate(self, assoc):
        if self.peek() != '[':
            raise Fail()
        self.i += 1
        n = 0
        while self.peek() != ']':
            if n:
                if self.peek() != ',':
                    raise Fail()
                self.i += 1
            self.work += 1
            self.value()
            if assoc:
                if self.peek() != '=':
                    raise Fail()
                self.i += 1
                self.value()
            n += 1
            if self.peek() is None:
                raise Fail()
        self.i += 1
        return 'lookup' if assoc else 'list'

    def value(self):
        if self.peek() == '[':
            return self.temporary()
        if self.peek() is None or self.peek() in (',', '=', ']', 'x'):
            raise Fail()
        self.i += 1

    def peek(self, k=0):
        j = self.i + k
        return self.t[j] if j < len(self.t) else None


# ------------------------------------------------------------------- SINGLE
class Single:
    def __init__(self, toks):
        self.t, self.i, self.work = toks, 0, 0

    def parse(self):
        return self.aggregate()

    def aggregate(self):
        if self.peek() != '[':
            raise Fail()
        self.i += 1
        kinds = []
        while self.peek() != ']':
            if kinds:
                if self.peek() != ',':
                    raise Fail()
                self.i += 1
            kinds.append(self.element())
            if self.peek() is None:
                raise Fail()
        self.i += 1
        if not kinds:
            return 'list'                       # the stated default
        first = kinds[0]
        if any(k != first for k in kinds):       # symmetric: checked AFTER all
            bad = next(n for n, k in enumerate(kinds) if k != first)
            raise Fail(f'mixed aggregate: element 1 is {"an association" if first == "lookup" else "a value"}, '
                       f'element {bad+1} is {"an association" if first == "list" else "a value"}')
        return first

    def element(self):
        self.work += 1
        if self.peek() is not None and self.peek(1) == '=':
            self.i += 2
            self.value()
            return 'lookup'
        self.value()
        return 'list'

    def value(self):
        if self.peek() == '[':
            return self.aggregate()
        if self.peek() is None or self.peek() in (',', '=', ']', 'x'):
            raise Fail()
        self.i += 1

    def peek(self, k=0):
        j = self.i + k
        return self.t[j] if j < len(self.t) else None


def latefail(depth):
    """A nest whose innermost element is followed by a stray token. Nothing
    fails until the bottom, so every alternative at every level is fully
    descended first. This is the hostile shape."""
    return '[' * depth + '1 x' + ']' * depth


print('=' * W)
print('1. Work on a late-failing square nest')
print('=' * W)
print(f'  {"depth":>6} {"ORDERED":>12} {"SINGLE":>10} {"ratio":>10}')
print('  ' + '-' * 42)
prev = None
for d in range(1, 15):
    src = lex(latefail(d))
    o, s = Ordered(src), Single(src)
    for p in (o, s):
        try:
            p.parse()
        except Fail:
            pass
    ratio = o.work / s.work if s.work else 0
    print(f'  {d:>6} {o.work:>12} {s.work:>10} {ratio:>10.1f}')
print('''
  ORDERED doubles per level; SINGLE is linear. That is the curve the
  regression should assert -- and it should assert the RATIO across two
  depths, not an absolute count, so it survives a machine change:

      work(depth 20) / work(depth 10)  must be < 3      (linear)
                                       not  ~1000       (exponential)
''')

print('=' * W)
print('2. The diagnostic the ordered structure cannot produce')
print('=' * W)
for src in ['[ a = 1 , 2 ]', '[ 2 , a = 1 ]', '[ ]', '[ a = 1 ]', '[ 1 , 2 ]']:
    toks = lex(src)
    try:
        o = Ordered(toks[:]).parse()
    except Fail:
        o = 'parse failure (no reason available -- both alternatives failed)'
    try:
        s = Single(toks[:]).parse()
    except Fail as e:
        s = str(e) or 'parse failure'
    print(f'  {src:18}')
    print(f'      ORDERED  {o}')
    print(f'      SINGLE   {s}')
print('''
  Under ordered alternatives «[a = 1, 2]» and «[2, a = 1]» are the same
  mistake reported as two unrelated failures, because each alternative bails
  at its own first mismatch and the caller only sees the last one. Parsing
  once and comparing kinds AFTER all elements are read gives one message for
  one mistake, in both orders. That symmetry is worth stating in the
  recommendation, because "decide kind from the first element" read literally
  would bail at the first mismatch and reproduce the asymmetry.''')
