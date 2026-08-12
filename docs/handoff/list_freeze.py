#!/usr/bin/env python3
"""
list_freeze.py -- the three ways "normalise on entry" is implemented, two of
which do not fix anything.

The programmer's question is whether «Graph.Var("xs", new object[]{...})» stays
legal for hosts and tests. The answer turns on what "normalise" means, because
two of the obvious implementations preserve the exact defect they are meant to
close:

    WRAP    store a read-only view over the caller's array
            -> the caller still holds the array. Immutability is now CLAIMED
               and still false, which is worse than today.
    SHALLOW copy the top level only
            -> a nested list is still the caller's mutable array, one level down
    DEEP    copy recursively, bottom-up
            -> actually immutable; and this is where the cycle guard belongs,
               because it is the only place a cycle can be reported with a path
               instead of a StackOverflow
"""

W = 78


class Frozen:
    """Stands in for the sealed runtime type: private storage, no accessor
    that hands the backing store out."""
    __slots__ = ('_items',)

    def __init__(self, items):
        object.__setattr__(self, '_items', tuple(items))

    def __getitem__(self, i):
        return self._items[i]

    def __len__(self):
        return len(self._items)

    def __eq__(self, other):
        return isinstance(other, Frozen) and self._items == other._items

    def __repr__(self):
        return '[' + ', '.join(map(repr, self._items)) + ']'


class ReadOnlyView:
    """The wrap. Looks immutable, is not: the caller kept the array."""
    __slots__ = ('_backing',)

    def __init__(self, backing):
        self._backing = backing

    def __getitem__(self, i):
        return self._backing[i]

    def __len__(self):
        return len(self._backing)

    def __repr__(self):
        return '[' + ', '.join(map(repr, self._backing)) + ']'


class Cycle(Exception):
    pass


def normalise(v, mode, seen=None):
    if not isinstance(v, list):
        return v
    if mode == 'wrap':
        return ReadOnlyView(v)
    if mode == 'shallow':
        return Frozen(v)
    seen = set() if seen is None else seen
    if id(v) in seen:
        raise Cycle('a list contains itself')
    seen = seen | {id(v)}
    return Frozen(normalise(x, mode, seen) for x in v)


print('=' * W)
print('1. WRAP does not fix it -- the caller kept the array')
print('=' * W)
caller = [1]
for mode in ('wrap', 'deep'):
    stored = normalise(caller, mode)
    caller[0] = 2                                  # host mutates its own array
    print(f'  normalise mode {mode:8}  host wrote 2  ->  graph reads {stored[0]}')
    caller[0] = 1
print('''
  The audit asks that "storage cannot be recovered by casting". That closes one
  route. It does not close the other: the caller never gave up its reference.
  A read-only VIEW satisfies the casting requirement and still fails, and it
  fails worse than today because the invariant is now asserted.

  So normalisation has to be a COPY. Wrapping is not freezing.''')

print('=' * W)
print('2. SHALLOW leaves the hole one level down')
print('=' * W)
inner = [1]
outer = [inner]
for mode in ('shallow', 'deep'):
    stored = normalise(outer, mode)
    inner[0] = 99
    print(f'  normalise mode {mode:8}  host wrote 99 into the inner list  ->  '
          f'graph reads {stored[0][0]}')
    inner[0] = 1
print('''
  A list of lists is the common case the moment «match» arms or grouped data
  exist, so shallow is not a stopgap -- it is the same bug with one more step
  of indirection.''')

print('=' * W)
print('3. The cycle guard belongs at the boundary, not in Same')
print('=' * W)
cyc = [1]
cyc.append(cyc)
try:
    normalise(cyc, 'deep')
    print('  deep normalise: accepted (unexpected)')
except Cycle as e:
    print(f'  deep normalise: REFUSED -- {e}')
except RecursionError:
    print('  deep normalise: recursion error (guard missing)')


def same_no_guard(a, b, depth=0):
    if isinstance(a, list) and isinstance(b, list):
        if len(a) != len(b):
            return False
        return all(same_no_guard(x, y, depth + 1) for x, y in zip(a, b))
    return a == b


c1, c2 = [1], [1]
c1.append(c1)
c2.append(c2)
try:
    same_no_guard(c1, c2)
    print('  Same on two self-containing lists: returned (unexpected)')
except RecursionError:
    print('  Same on two self-containing lists: RecursionError '
          '(= StackOverflowException in .NET, unrecoverable)')
print('''
  Both stop the crash. Only one of them can say WHAT went wrong: at the
  boundary the value has a name and a host call site, so the message is
  «xs contains itself» and it points at the caller. In «Same» the two values
  are anonymous and the only honest message is "too deep".

  So: enforce acyclicity in the normaliser. Keep a cheap depth cap in «Same»
  anyway -- "Same can never see a cycle" is exactly the class of invariant
  this project keeps finding unenforced, and the cap costs one integer.''')

print('=' * W)
print('4. What Read can hand back -- and why one option is the bug again')
print('=' * W)
print('''  a. the immutable type          honest; host code changes once
  b. a defensive object[] copy   host code unchanged, and a host that mutates
                                 what it got back sees nothing happen. That is
                                 the SAME silent-mutation defect, moved to the
                                 read side, plus an O(n) allocation per read
  c. a read-only interface       host sees IReadOnlyList; mutation attempts do
                                 not compile / throw loudly

  (b) is the one that looks like compatibility and is not. Between (a) and (c)
  either is defensible; (c) keeps the host boundary convenient and still
  refuses mutation out loud.''')

print('=' * W)
print('5. Empty is a singleton, which is free and is not interning')
print('=' * W)
EMPTY = Frozen(())
print(f'  EMPTY is EMPTY            {EMPTY is EMPTY}   O(1)')
print(f'  Frozen(()) == Frozen(())  {Frozen(()) == Frozen(())}   O(1) by length')
print('''
  One cached instance for the commonest list in any program. That is not the
  global intern table LISTEQUALITY.md §5 refused -- it is a single static, with
  no table, no lookup and no contention.''')
