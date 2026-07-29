#!/usr/bin/env python3
"""
ronin_grammar_probe.py

An instrument, not a compiler.

Ronin proposes two features that interact badly:
  (F1) identifiers may contain spaces        -> lexing needs the symbol table
  (F2) single-argument calls need no brackets -> argument extent is unmarked

This probe implements a symbol-table-driven scannerless parser that enumerates
*every* valid parse of an input under a given scope, so ambiguity can be
measured instead of argued about.

DISAMBIGUATION RULE UNDER TEST (Budai's rule):
  Of all valid parses, take the one performing the FEWEST symbol-table
  lookups. A lookup is one resolution: each name reference costs 1, each
  pattern call costs 1. A tie is a compile error -- never a silent pick.

This single metric subsumes both maximal-munch on names ("base price" is one
lookup, "base" + "price" is two) and longest-pattern-preference ("send _ to _"
is one lookup, "send _" plus a second call is two). That is a real economy of
mechanism: one rule where most candy-grammar languages need three.

Remaining policy knob:

  arg_mode   : 'atom'   an unbracketed argument is exactly one atom
               'expr'   an unbracketed argument is a full expression

A rule set is viable iff every well-formed program has exactly one
minimum-cost parse.
"""

from dataclasses import dataclass
from itertools import count
import re

# ---------------------------------------------------------------- tokenizer

BINOPS = {'+', '-', '*', '/'}
PUNCT = set('()=:,') | BINOPS

TOKEN_RE = re.compile(r'''
      (?P<num>\d+(?:\.\d+)?)
    | (?P<str>"[^"]*")
    | (?P<word>[A-Za-z_][A-Za-z0-9_]*)
    | (?P<punct>[()=:,+\-*/])
    | (?P<ws>\s+)
''', re.VERBOSE)


def tokenize(src):
    """Raw tokens only. No identifier assembly -- that is the parser's job,
    because it cannot be done without the symbol table."""
    toks, i = [], 0
    while i < len(src):
        m = TOKEN_RE.match(src, i)
        if not m:
            raise SyntaxError(f'stray character {src[i]!r} at {i}')
        i = m.end()
        if m.lastgroup != 'ws':
            toks.append(m.group())
    return toks


# ------------------------------------------------------------------- scope

@dataclass(frozen=True)
class Scope:
    """names   : set of word-tuples, e.g. ('base','price')
       patterns: set of segment-tuples, where a segment is a word or the
                 sentinel HOLE, e.g. ('send', HOLE, 'to', HOLE)"""
    names: frozenset
    patterns: frozenset


HOLE = None


def pat_str(pat):
    return ' '.join('(_)' if s is HOLE else s for s in pat)


# --------------------------------------------------------------- AST nodes

@dataclass(frozen=True)
class Lit:
    text: str

    def show(self):
        return self.text


@dataclass(frozen=True)
class Name:
    words: tuple

    def show(self):
        return '«' + ' '.join(self.words) + '»'


@dataclass(frozen=True)
class Call:
    pat: tuple
    args: tuple

    def show(self):
        out, ai = [], 0
        for s in self.pat:
            if s is HOLE:
                out.append(self.args[ai].show())
                ai += 1
            else:
                out.append(s)
        return '[' + ' '.join(out) + ']'


@dataclass(frozen=True)
class Bin:
    op: str
    lhs: object
    rhs: object

    def show(self):
        return f'({self.lhs.show()} {self.op} {self.rhs.show()})'


# ------------------------------------------------------------------ parser

class Probe:
    def __init__(self, scope, arg_mode='expr', name_match='all', depth_cap=40):
        # A pattern beginning with a hole is left-recursive: resolving an atom
        # at position p would require resolving an atom at position p. This is
        # not an implementation limit, it is a language constraint --
        # user-defined infix patterns cannot coexist with unbracketed args.
        for pat in scope.patterns:
            if pat and pat[0] is HOLE:
                raise ValueError(
                    f'left-recursive pattern {pat_str(pat)!r}: '
                    'patterns must begin with a word')
        self.scope = scope
        self.arg_mode = arg_mode
        self.name_match = name_match
        self.depth_cap = depth_cap

    # -- atoms ------------------------------------------------------------
    def atoms(self, toks, pos, depth=0):
        if depth > self.depth_cap or pos >= len(toks):
            return
        t = toks[pos]

        if t[0].isdigit() or t[0] == '"':
            yield Lit(t), pos + 1
            return

        if t == '(':
            for node, p in self.exprs(toks, pos + 1, depth + 1):
                if p < len(toks) and toks[p] == ')':
                    yield node, p + 1
            return

        # -- multi-word names: the symbol table drives the lexer
        hits = []
        for words in self.scope.names:
            n = len(words)
            if tuple(toks[pos:pos + n]) == words:
                hits.append(words)
        if hits and self.name_match == 'longest':
            best = max(len(w) for w in hits)
            hits = [w for w in hits if len(w) == best]
        for words in hits:
            yield Name(words), pos + len(words)

        # -- pattern calls
        for pat in self.scope.patterns:
            for args, p in self.match_pattern(pat, 0, toks, pos, depth + 1):
                yield Call(pat, tuple(args)), p

    def match_pattern(self, pat, si, toks, pos, depth):
        if si == len(pat):
            yield [], pos
            return
        seg = pat[si]
        if seg is not HOLE:
            if pos < len(toks) and toks[pos] == seg:
                for rest, p in self.match_pattern(pat, si + 1, toks, pos + 1, depth):
                    yield rest, p
            return
        # a hole: how far does an unbracketed argument reach?
        producer = self.atoms if self.arg_mode == 'atom' else self.exprs
        for arg, p in producer(toks, pos, depth):
            for rest, p2 in self.match_pattern(pat, si + 1, toks, p, depth):
                yield [arg] + rest, p2

    # -- expressions ------------------------------------------------------
    def exprs(self, toks, pos, depth=0):
        """atom (binop atom)*  -- chaining is maximal, so operators contribute
        no ambiguity of their own and we measure naming ambiguity alone."""
        for lhs, p in self.atoms(toks, pos, depth):
            if p < len(toks) and toks[p] in BINOPS:
                op = toks[p]
                for rhs, p2 in self.exprs(toks, p + 1, depth + 1):
                    yield Bin(op, lhs, rhs), p2
            else:
                yield lhs, p

    # -- scoring ----------------------------------------------------------
    @staticmethod
    def cost(node):
        """Budai's metric: how many symbol-table lookups does this reading
        require? Each name reference and each pattern call is one lookup.
        Literals and operators are free -- they need no table."""
        if isinstance(node, Lit):
            return 0
        if isinstance(node, Name):
            return 1
        if isinstance(node, Bin):
            return Probe.cost(node.lhs) + Probe.cost(node.rhs)
        if isinstance(node, Call):
            return 1 + sum(Probe.cost(a) for a in node.args)
        raise TypeError(node)

    def parse_all(self, src):
        """Every distinct full parse, as (cost, rendering)."""
        toks = tokenize(src) if isinstance(src, str) else src
        seen, out = set(), []
        for node, p in self.exprs(toks, 0):
            if p == len(toks):
                s = node.show()
                if s not in seen:
                    seen.add(s)
                    out.append((self.cost(node), s))
        return sorted(out)

    def resolve(self, src):
        """Apply the rule. Returns (verdict, winners, all_parses)."""
        parses = self.parse_all(src)
        if not parses:
            return 'NO PARSE', [], parses
        best = parses[0][0]
        winners = [s for c, s in parses if c == best]
        if len(winners) > 1:
            return 'TIE -> ERROR', winners, parses
        return 'OK', winners, parses

    # -- repair search ----------------------------------------------------
    def repairs(self, src):
        """The escape hatch: bracket one argument so it becomes its own
        substatement. Enumerate every single bracket insertion and record
        which readings each one makes uniquely reachable.

        Property under test: every reading of an ambiguous statement is
        reachable by SOME bracketing. If not, the language has programs no
        programmer can write."""
        toks = tokenize(src)
        reachable = {}
        for i in range(len(toks)):
            for j in range(i, len(toks)):
                cand = toks[:i] + ['('] + toks[i:j + 1] + [')'] + toks[j + 1:]
                verdict, winners, _ = self.resolve(cand)
                if verdict == 'OK':
                    # strip the brackets we added to compare readings by shape
                    key = winners[0].replace('(', '').replace(')', '')
                    reachable.setdefault(key, ' '.join(cand))
        return reachable


# ------------------------------------------------- declaration pre-pass test

DECL_LET = re.compile(r'^\s*let\s+(?P<name>[^=]+?)\s*=\s*(?P<body>.+)$')
DECL_TO = re.compile(r'^\s*to\s+(?P<pat>[^:]+?)\s*:\s*(?P<body>.*)$')


def prepass(source_lines):
    """Claim under test: definition *headers* are context-free, so a cheap
    pre-pass can build the symbol table without needing the symbol table.
    If this holds, mutual recursion and use-before-definition both work."""
    names, patterns = set(), set()
    for line in source_lines:
        m = DECL_LET.match(line)
        if m:
            names.add(tuple(m.group('name').split()))
            continue
        m = DECL_TO.match(line)
        if m:
            segs, buf = [], m.group('pat')
            for chunk in re.finditer(r'\(([^)]*)\)|([A-Za-z_][A-Za-z0-9_]*)', buf):
                if chunk.group(1) is not None:
                    segs.append(HOLE)
                else:
                    segs.append(chunk.group(2))
            patterns.add(tuple(segs))
    return names, patterns
