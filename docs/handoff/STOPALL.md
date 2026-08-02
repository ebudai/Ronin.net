# `stop` / `stop all` — good idea, and it catches a bug I was about to ship

Not a bad idea. Better than what I wrote, and for a reason that only became
clear once I tried to write down why it was redundant.

---

## 1. It is not one piece of state, and I had just merged two

`CHAIN-ACTIVATIONS.md` said *"`stop` zeroes the count: the chain is over."*
`WHEN-TYPE-SCOPE.md` said *"the node goes when the mask empties."* Put those
together under counting and you get:

> a chain that completes normally has count 0, therefore its mask is empty,
> therefore its `when` is removed — **every time it finishes**.

A one-shot chain would work and a repeating one would silently stop working
after its first run. That is exactly the class of bug this whole thread has
been trying to avoid, and it was two sentences away from being built.

The reason is that these are **two different pieces of state** and I had
collapsed them:

| | what it means | normal life |
|---|---|---|
| **armed** | may this `when` be triggered at all? | true from declaration until stopped |
| **activations** | how many chain runs are in flight? | 0 → k → 0, constantly |

An empty activation set is the *resting state* of a healthy chain. It carries no
information about whether the `when` should still exist. Budai's split maps
exactly onto the two, which is why it does not feel redundant.

## 2. The semantics

```
stop        ends THIS activation — do not advance to the next segment.
            Other pending activations are unaffected. The «when» stays armed.

stop all    disarms the «when». Every pending activation is abandoned and the
            node is removed. At type scope, for every instance.
```

`stop` is the common case and is unmarked; `stop all` is rare and is marked.
That is the right way round.

Both still only ever *shrink* the graph, so the cascade argument is untouched:
a statically legal program stays legal, and `Cascades` needs no dynamic
analysis.

Note what falls out for free: a one-shot chain is `when ready { init; stop }` —
the activation ends, the count returns to 0, and because nothing re-triggers
`ready`, nothing runs again. No `stop all` needed for the common one-shot.
`stop all` is for abandoning work that is genuinely in flight, which is why it
earns its keep most clearly at type scope: *this rule is off now, for every
instance*, is a thing you cannot say any other way.

## 3. One constraint, and it is not cosmetic

**`stop all` must be a single lexer token.** `stop` is a prefix of `stop all`,
and R6 requires determinate prefixes to be prefix-free — so as two patterns
they cannot coexist, for the same reason `for (_)` cannot sit beside
`for each (_) in (_)`.

The precedent is already in the compiler: `for each` is one token, which is
exactly why `for` remains available. `stop all` follows it, and inherits the
same requirement from that decision — `stop  all` and `stop<TAB>all` must be
the same keyword.

## 4. The one thing I would push back on

`all` is doing a lot of work in a two-word keyword, and *all what* is not
obvious from the words: all activations, all instances, or all `when`s? It
happens to mean the first two and not the third.

I do not have a better spelling — `cancel` loses the connection to `stop`,
`stop everything` has the same prefix problem and reads worse — so I would keep
it and make the diagnostic and the guide carry the distinction:

> «stop» ends this run of the chain. «stop all» disarms «when payment cleared»
> entirely, abandoning 3 runs that were in flight.

An IDE that can report *how many were abandoned* turns the one genuinely
surprising thing about `stop all` into something visible at the moment it
happens, which is what the always-running environment is for.
