#!/usr/bin/env python3
"""
reactive_core.py -- executable answers to the three interpreter questions.

  1. how `let` and `var` differ at evaluation
  2. what a Call invokes
  3. what reactivity means for the dependency graph

Prose specs go ambiguous exactly where implementations need precision, so this
is the spec. Port it, and use the scenarios at the bottom as the test list.

THE ONE RULE EVERYTHING RESTS ON
    A `let` body is PURE. It may not assign a var and may not touch a resource.
    That is what makes it safe to re-run any number of times, which is what
    makes recompute, live edit, replay, and parallel evaluation all work. The
    interpreter must ENFORCE it, not assume it.
"""

from dataclasses import dataclass, field


class RoninError:
    """Errors are VALUES that flow through the graph, the way #DIV/0! flows
    through a spreadsheet. A node whose dependency is an error becomes an error
    WITHOUT running its body."""

    def __init__(self, message):
        self.message = message

    def __repr__(self):
        return f'error({self.message})'


NOTHING = object()


class PurityViolation(Exception):
    pass


@dataclass
class Node:
    name: str
    kind: str                      # 'var' | 'let'
    body: object = None            # callable(env) for let; None for var
    front: object = None           # the value readers see this generation
    back: object = None            # the value being written this generation
    dirty: bool = True
    evaluating: bool = False       # cycle detection
    deps: set = field(default_factory=set)
    dependents: set = field(default_factory=set)


class Graph:
    def __init__(self):
        self.nodes = {}
        self.pending = {}          # var -> value written this step
        self.trace = []            # what recomputed, for the tests
        self.reading_stack = []    # dynamic dependency capture

    # ---------------------------------------------------------- declaration
    def var(self, name, value):
        """`var` is a SOURCE. Its initialiser is evaluated ONCE, now."""
        node = Node(name, 'var', front=value, dirty=False)
        self.nodes[name] = node
        return node

    def let(self, name, body):
        """`let` is DERIVED. Its body is NOT evaluated at declaration -- it is
        evaluated on first read, and re-evaluated when a dependency changes and
        someone asks for it. Declaration order therefore does not matter."""
        node = Node(name, 'let', body=body, dirty=True)
        self.nodes[name] = node
        return node

    # ------------------------------------------------------------- reading
    def read(self, name):
        node = self.nodes[name]

        # capture the dependency edge DYNAMICALLY. Not from the AST: a
        # conditional depends on the branch it actually took, and that changes
        # between evaluations.
        if self.reading_stack:
            reader = self.reading_stack[-1]
            reader.deps.add(node.name)
            node.dependents.add(reader.name)

        if node.kind == 'var':
            return node.front

        if node.evaluating:
            # cycles are an error, per the language decision. Detected by
            # re-entry, not by static analysis.
            return RoninError(f'cycle through «{node.name}»')

        if node.dirty:
            self.recompute(node)
        return node.front

    def recompute(self, node):
        # clear the old edges first or a stale dependency keeps the node dirty
        # forever after a conditional switches branches
        for dep in node.deps:
            self.nodes[dep].dependents.discard(node.name)
        node.deps.clear()

        node.evaluating = True
        self.reading_stack.append(node)
        try:
            value = node.body(self)
        except PurityViolation as violation:
            value = RoninError(str(violation))
        finally:
            self.reading_stack.pop()
            node.evaluating = False

        node.front = value
        node.dirty = False
        self.trace.append(node.name)

    # ------------------------------------------------------------- writing
    def write(self, name, value):
        """Assignment to a `var`. Goes to the BACK buffer: invisible to readers
        until the propagation step flips. That is what keeps two vars written
        together from being observed half-updated."""
        node = self.nodes[name]
        if node.kind != 'var':
            raise PurityViolation(f'«{name}» is a let; only its body may set it')
        if self.reading_stack:
            raise PurityViolation(
                f'«{self.reading_stack[-1].name}» is a let and may not assign «{name}»')
        self.pending[name] = value

    def step(self):
        """ONE propagation step. Every write made since the last step becomes
        visible at the same instant, so a reader can never see new A with old B.
        Dirty marking is pushed; recomputation is pulled on demand."""
        self.trace = []
        for name, value in self.pending.items():
            node = self.nodes[name]
            if node.front == value:
                continue                    # unchanged: do not wake dependents
            node.front = value
            self.mark_dirty(node)
        self.pending.clear()

    def mark_dirty(self, node):
        for name in list(node.dependents):
            dependent = self.nodes[name]
            if dependent.dirty:
                continue                    # already marked; stop, no rework
            dependent.dirty = True
            self.mark_dirty(dependent)


# ---------------------------------------------------------------- operators

def lift(fn):
    """Every builtin propagates errors instead of running on them. `otherwise`
    is the ONE exception -- it is the only thing that inspects a dependency's
    error state without inheriting it."""
    def wrapped(*args):
        for a in args:
            if isinstance(a, RoninError):
                return a
        return fn(*args)
    return wrapped


add = lift(lambda a, b: a + b)
mul = lift(lambda a, b: a * b)
gt = lift(lambda a, b: a > b)


def otherwise(value, fallback):
    """Catches BOTH nothing and error, per the decision. This is the whole
    ergonomic replacement for forcing `if (x is error)` at every use."""
    if isinstance(value, RoninError) or value is NOTHING:
        return fallback
    return value


# --------------------------------------------------------- what a Call is

@dataclass
class Declaration:
    """The missing link. The resolver produces Call(pattern, args) but a
    Pattern is only a SHAPE. Bind the shape to this at declaration time, and
    the interpreter has something to invoke.

    A hole in the pattern is one PARAMETER BLOCK, not one parameter -- the
    guide allows «(x, y)» and allows the brackets to be elided when fewer than
    two parameters are bound. So the resolver hands over one argument per hole
    and the binder destructures blocks of arity > 1."""
    pattern: tuple                 # ('compute', 'total', 'for', None)
    blocks: tuple                  # (('order',),) -- names per hole
    body: object                   # callable(env, **bound)
    pure: bool = True              # may it appear inside a let body?


class Scope:
    """Pattern shape -> LIST of declarations. A list, not a single entry,
    because overloads share a shape and are separated later by type: the phase
    order is enumerate -> type filter -> rank by lookup -> tie is an error."""

    def __init__(self):
        self.declarations = {}

    def declare(self, declaration):
        self.declarations.setdefault(declaration.pattern, []).append(declaration)

    def invoke(self, graph, pattern, args, inside_let):
        candidates = self.declarations.get(pattern, [])
        if not candidates:
            return RoninError(f'no declaration for «{fmt(pattern)}»')
        if len(candidates) > 1:
            return RoninError(f'«{fmt(pattern)}» is ambiguous after type filtering')

        declaration = candidates[0]
        if inside_let and not declaration.pure:
            return RoninError(
                f'«{fmt(pattern)}» has effects and cannot appear in a let body')

        bound = {}
        for block, arg in zip(declaration.blocks, args):
            if len(block) == 1:
                bound[block[0]] = arg
            else:
                if not isinstance(arg, (list, tuple)) or len(arg) != len(block):
                    return RoninError(
                        f'block {block} needs {len(block)} arguments')
                bound.update(dict(zip(block, arg)))
        for a in bound.values():
            if isinstance(a, RoninError):
                return a
        return declaration.body(graph, **bound)


def fmt(pattern):
    return ' '.join('(_)' if s is None else s for s in pattern)
