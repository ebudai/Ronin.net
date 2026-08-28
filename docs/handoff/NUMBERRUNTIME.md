# The number ladder's runtime — the library is the last question, and it is nearly forced

**Short version: C# primitives, with one hand-written kernel, and no native
dependency until a measurement demands one.** But the reason is not a benchmark.
It is that **you already ruled the semantics**, and the semantics you ruled make
the hot path *integer*, not floating point — which retires most of the question
before it is asked.

Two things need you before anything is built. They are in §4 and §7.

---

## §0 — the decision that is already made, and the runtime that does not implement it

On record from earlier:

> **exact by default with 64-bit, `fast number` opt-in, boundary at roots and
> transcendentals rather than division** — and the goal that *the programmer
> should not need to know computer math to get good math*.

"Boundary at division rather than roots" would have meant decimals. **Boundary
*past* division means `1/3` is exact, which means rationals.** That is a
numeric tower, not a float, and it changes what the implementation question even
is.

Meanwhile the tree implements none of it. `Values.Arithmetic` is
`Func<double, double, double>` and `Evaluator` parses every literal with
`double.TryParse`. So the ladder today is one rung, and it is the rung you opted
*out* of.

There is also a live inconsistency worth fixing whatever else happens. `Divide()`
refuses to produce an infinity, and its comment is the best statement of the
language's numeric posture anywhere in the tree:

> *An infinity would satisfy the hardware and then poison everything downstream
> silently, which is precisely the spreadsheet failure the error model exists to
> make visible.*

But `Arithmetic("*")` on two large doubles **produces exactly that infinity**,
silently, and the guide still says `number` stores *"infinite or undefined."*
Three positions on IEEE specials, one type. Division was ruled; multiplication
was inherited. That is semantics being set one operator at a time, which is the
thing to stop before choosing a library — because **a library you pick becomes
the semantics you shipped.**

## §1 — what exact-by-default does to the performance question

This is the part that makes the answer easy. Under exact-by-default:

- the **common** case is a rational with denominator 1 — i.e. an integer;
- the hot path is `long` add/multiply with an **overflow check**;
- floating point does not appear at all until `fast number`, a root, or a
  transcendental.

So the question is not "double or BigInteger or GMP." It is "how fast is checked
64-bit integer arithmetic," and the answer is: about as fast as unchecked, because
the check is a flag test and a never-taken branch. .NET emits that natively. No
library is involved and none would help.

**The expensive thing in an exact tower is not big multiplication. It is
normalisation** — the GCD on every rational result. That is where the time
actually goes, and it is a twenty-line kernel, not a dependency.

## §2 — so: primitives, and one kernel worth writing

| rung | representation | source |
|---|---|---|
| integer-valued `number` | `long`, checked | primitive |
| non-integer `number` | `long`/`long` rational, normalised | primitive + one kernel |
| `fast number` | `double` | primitive |
| `large whole number` | `BigInteger` | BCL |
| `large number` | **nothing in the BCL** | see §3 |

The kernel is **binary GCD (Stein)** over `long`, plus overflow-aware rational
add/multiply that normalises *before* combining rather than after, so
`a/b + c/d` does not overflow on operands that had a perfectly representable
answer. That is the one place "custom implementation" earns its keep: it is
small, self-contained, testable exhaustively at the boundaries, and it is on the
hot path. Writing your own bignum is not that; writing your own GCD is.

`net8.0` also gives you **generic math** (`INumber<T>`, .NET 7+), which matters
more here than it looks. The ladder is *representation selected at compile time
by examining usage* — that is monomorphisation, which you have **already ruled is
forced**. So the arithmetic gets written once over a type parameter and
instantiated per representation, using the mechanism the compiler already has.
Primitives are not merely adequate; they are the option that composes with a
decision on record.

## §3 — where the BCL runs out, and it is exactly one rung

`large number` — arbitrary precision, non-integer — has **no first-party
answer**. There is no `BigDecimal`, no `BigRational`, no big float in .NET. The
managed field is third-party and uneven: [AdamWhiteHat/BigDecimal](https://github.com/AdamWhiteHat/BigDecimal)
is the widely-used one, [SunsetQuest/BigFloat](https://github.com/SunsetQuest/BigFloat) and
[bigfloat.org](https://bigfloat.org/) are alternatives, and
[Numerics.NET](https://numerics.net/documentation/latest/mathematics/arbitrary-precision-arithmetic/arbitrary-precision-floating-point-numbers)
is commercial.

**But under the exact ruling you may not need any of them.** `large number` is
just the 64-bit rational with `BigInteger` in both slots — the same kernel, a
wider element type, which generic math gives you for free. A big *float* is only
needed past the roots-and-transcendentals boundary at more than double precision,
and that is a much smaller feature than "arbitrary precision numbers."

That is worth checking before shopping. It may be that the entire third-party
question dissolves.

**On GMP/MPFR specifically:** the speed is real at large operand sizes, and the
costs are single-file deployment, AOT, a platform matrix, and an LGPL question
for a runtime you may want statically linked. None of that is worth paying for
operand sizes a RAD program rarely reaches. **Ledger it rather than decide it** —
approximation *"`BigInteger` is the wide element type"*, successor *"a native
bignum"*, trigger *"a measured workload exceeding a stated budget."* Same
discipline as the module-path row. The point of the trigger is that nobody has to
remember; something re-derives it.

## §4 — the fork I need you on: what happens at 64 bits

Exact-by-default has one failure mode, and it is the one your own `Divide`
comment already legislates against. When a numerator or denominator outgrows 64
bits, there are three answers:

1. **promote silently to big rationals** — stays exact, gets quietly slower and
   quietly unbounded. No value is corrupted, but a program's performance becomes
   data-dependent with no cue in the source.
2. **round to a float** — silent precision loss. This is the infinity in a
   different costume and I think it is already refused.
3. **`Error`, naming `large number` as the repair** — visible, consistent with
   divide-by-zero and with indexing past the end, and it keeps "64-bit" meaning
   64-bit.

My lean is **3**, because it is the only one where the ladder's rungs mean
something: `number` is the exact 64-bit one, `large number` is the unbounded one,
and the compiler tells you which you needed. Option 1 makes `large number`
decorative. But 1 is defensible on the *batteries-included* argument — the user
did not ask to think about widths — so it is yours.

There is a smaller sibling: **how an exact non-integer prints.** `1/3` is exact
and has no finite decimal. Printing `0.333…` is a lie about a value the language
went to trouble to keep true. This is a readability question, so it is one of
yours too, but it should be answered *with* the representation and not after it.

## §5 — "defer to the OS": the one option with a hidden cost

Worth separating, because the answer differs by operation.

**Basic arithmetic is safe.** `+ − × ÷ √` are correctly rounded by IEEE-754 and
identical on every platform .NET runs on. Deferring costs nothing.

**Transcendentals are not.** `sin`, `cos`, `pow`, `exp` are not required to be
correctly rounded, and they differ across platforms, libm versions, and hardware.
So "the same program gives the same answer" fails across machines — silently, in
the last digits. Under *debug is development*, where a program runs continuously
and runs get compared, that is a property to lose deliberately or not at all.

The choices are: accept and **document** platform variance; or ship correctly
rounded implementations (the CORE-MATH project exists for exactly this). Either
is fine. The one that is not fine is arriving there by default and finding out
from a bug report.

Note where this lands: it is entirely **past the exactness boundary you already
drew**. Roots and transcendentals are precisely the region where Ronin admits
IEEE — so it is the same line, seen from the runtime side.

## §6 — Herbie: yes as an editor affordance, no as a compiler pass

[Herbie](https://herbie.uwplse.org/) takes a floating-point expression and
searches for a rewriting with lower error, using high-precision sampling as
ground truth ([PLDI'15](https://herbie.uwplse.org/pldi15-paper.pdf)). It is good
work. Three separate questions:

**As an automatic pass: no**, and for a Ronin-specific reason rather than a
general one. Herbie's output is **not** semantically equivalent — it is a
different expression that is more accurate *on a sampled input distribution*.
Applying it silently means the source no longer says what runs, which inverts the
language's premise. It also depends on a distribution the source never states,
and it is a search — seconds to minutes — which the always-running compile cannot
absorb.

**As an editor suggestion: yes, and it is exactly the shape you already use.**
*"This expression loses N digits on plausible inputs; here is a more accurate
form — accept?"*, with acceptance **written into the source**. That is
ambiguity-as-error with selectable repairs, applied to accuracy: the tool
proposes, the source records the choice, the compiler never reinterprets.

**But notice how small the target is.** Herbie exists because programmers are
forced to write floating-point expressions. Your stated goal is that they should
not have to. Under exact-by-default, Herbie applies **only** inside `fast number`
and past the roots boundary. That is the correct scope, it is narrow, and it is
not the first thing to build.

**The part worth stealing now** is not Herbie but its method: evaluate at high
precision, sample, compare. That is how the numeric conformance suite gets built
regardless — and it is the only way you will ever know the tower is right.

## §7 — the research instance: commission it, but not for this

Yes, with a scope, and the scope matters because the risk is not where the
literature is. **No literature answers what `number` should mean in Ronin** —
that is a values question about this language, and it is the part that is
actually undecided. Commissioning it there would produce a survey, and a survey
would get read as an answer.

Worth commissioning, because being current beats reasoning:

- **prior art with post-mortems** — and one item dominates: **Scheme's numeric
  tower** is exact-by-default with a boundary at inexact operations. It is your
  design, forty years earlier, with forty years of complaints about it. What its
  implementers regret is worth more than any benchmark. After that: Raku's
  rationals (which chose silent degradation at a width limit — your §4 fork,
  already run as an experiment), Python's int/float split, Julia's promotion
  rules, JS BigInt's separateness.
- **the managed arbitrary-precision field** — maintenance, licence, correctness
  record. Cheap to get wrong by picking from a search result.
- **GMP-versus-managed magnitudes** *with operand sizes and conditions stated*,
  so the ledger trigger in §3 can be written with a real number in it.
- **the state of correctly-rounded libm**, for §5.

Not worth commissioning: **generic-math abstraction overhead** and **checked-vs-
unchecked integer cost**. Both are measurable here in an afternoon, on the actual
tree, on the actual workload.

And one condition, which is the rule already in force for claims about the tree,
extended: **a research instance returns claims about the world.** They need
provenance — citation, date, benchmark conditions — and anything load-bearing
gets re-measured locally before something is built on it. I have already spent one
figure in this project that was true when it was measured and false when I quoted
it. A survey makes that easier to do, not harder.

> Don't research what you can measure, and don't measure what you haven't decided.

## Summary

| | |
|---|---|
| the recommendation | **C# primitives + generic math**, one hand-written kernel, no native dependency until a measurement demands one |
| why, in one line | exact-by-default makes the hot path **checked 64-bit integer**, not floating point — so the bignum library question barely applies to the common case |
| the kernel | **binary GCD + overflow-aware rational normalisation.** Small, hot, exhaustively testable. That is where custom earns its keep; a hand-rolled bignum is not |
| the alignment | representation-selected-at-compile-time **is** monomorphisation, which is already ruled forced. `INumber<T>` instantiates per representation using machinery the compiler has |
| where the BCL ends | **one rung** — `large number`. And under the exact ruling it may just be the same rational with `BigInteger` slots, which dissolves the third-party question |
| GMP/MPFR | real speed at large operands; costs single-file deploy, AOT, platform matrix, LGPL. **Ledger it with a trigger**, don't decide it |
| defer to the OS | fine for `+ − × ÷ √` (correctly rounded, identical everywhere). **Not** fine for transcendentals — they vary by platform, so *same program, same answer* fails silently |
| **needs you (§4)** | what happens at 64 bits: silent promotion, silent rounding, or **`Error` naming `large number`**. My lean is Error — it is the only one where the rungs mean anything |
| **needs you (§4)** | how an exact `1/3` **prints**, since it has no finite decimal. A readability question, answered with the representation and not after |
| Herbie | **editor suggestion yes, compiler pass no** — its output is not equivalent, it depends on an unstated input distribution, and it is a search. Scope is only `fast number` and past the roots boundary |
| steal from Herbie now | the **method** — high-precision ground truth, sampled — as the numeric conformance suite |
| research instance | yes, for **prior art with post-mortems** (Scheme's tower above all), the managed field, GMP magnitudes, libm. **Not** for what `number` should mean, and not for anything measurable locally |
| the tree, today | `double` everywhere, implementing none of the ruling — and `*` silently produces the infinity `/` refuses. Fix that inconsistency regardless |
