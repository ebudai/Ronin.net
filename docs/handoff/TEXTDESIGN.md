# Text — the unit of indexing decides everything, and StringZilla is the last question again

> **Ledger** — `[V]` verdict (ratified: «yes to all of it. no stringzilla, utf-8,
> update to dotnet 10»). The `text` design: grapheme indexing with `fast text` as
> the escape hatch, NFC on construction, invariant case and collation by default,
> UTF-8 storage. Rules against StringZilla for now, with the trigger named. Binding
> for `text @ number → text` (`SLICEONETYPINGS` §3).
> supersedes: none
> superseded by: none

**Short answer on StringZilla: not now, and the reason is not performance.** It
has **no C#/.NET binding** — v3 ships C, C++, Rust, Swift and Python — so adopting
it means writing and maintaining P/Invoke; .NET's own span primitives are already
SIMD-accelerated so the delta is one vector implementation against another, not
vector against scalar; and **it accelerates the byte layer, which under §1 is the
escape hatch rather than the default.** Optimising the escape hatch before the
default exists is backwards.

But the same thing happened here as with numbers: **the questions worth answering
first are about what `text` means.** Four of them, and the first one decides the
rest.

---

## §1 — the decision: what does `text @ 1` return?

This is the exactness question wearing different clothes. `text` is UTF-8 per the
guide, so a value is a byte sequence, and "the first character" has three answers:

```
  "héllo"      byte      -> the first byte of a 2-byte «h»?  no — «h», then «é» spans two
  "héllo"      code point-> «h», «é», «l», «l», «o»                      5 units
  "👨‍👩‍👧"       code point-> 5 units — three people and two joiners
  "👨‍👩‍👧"       grapheme  -> 1 unit, which is what a person sees
```

Ronin's premise settles it. A reader who has never thought about Unicode writes
`name @ 1` and means *the first thing they can see*. Byte and code-point indexing
give an answer that is wrong in a way the source does not show — which is the
`01-02-03` shape exactly: one spelling, several readings, no cue.

> **`text` is indexed, sliced and measured in grapheme clusters.**

That is the expensive answer and it is the one this language takes elsewhere.
Swift made the same choice and is generally judged the most correct and the
slowest; Rust refuses indexing outright; Python indexes code points; JavaScript
indexes UTF-16 units, which is the worst of all worlds and is the reason
`"👍".length` is 2.

### And the escape hatch already has a name

```
  number  /  fast number        exact rational   /  IEEE double
  text    /  fast text          grapheme units   /  raw bytes
```

This is worth more than the convenience. It gives `fast` **one meaning across the
whole language**:

> **`fast X` is the machine's X rather than the person's X.**

It predicts the trade correctly in both cases — you get speed, and you give up the
property a non-expert assumed was there. And it means `fast` is a language-wide
modifier with a stated rule rather than a numeric quirk that later needs a second
word for text.

## §2 — normalisation: NFC at construction, and the argument is decisive

Same visible text, two byte sequences:

```
  "café"   as  c a f é          (é = U+00E9)              NFC
  "café"   as  c a f e ́          (e + combining acute)     NFD
```

Both are ordinary. The second is what macOS filenames give you. Under value
equality all the way down, `is` compares byte sequences and returns **false** on
two texts a reader cannot tell apart. That is the worst failure available to this
language: the source says the true thing and the program disagrees.

The fix is normalisation, and the only real question is *when*. **Construction, not
comparison** — and this is not a preference:

> If normalisation happens only at comparison, then `a is b` is **true** while
> `length of a` differs from `length of b` and `a @ 3` differs from `b @ 3`.
> Equality that the other operations do not respect is not equality.

So normalise where text enters — literals, file reads, network, editor input — and
everything downstream is comparable, indexable and measurable consistently. Most
text in the world is already NFC, so the pass is usually a scan that changes
nothing, and it lands at the same boundary as the UTF-8 transcode in §5. One pass,
both jobs.

## §3 — case and collation: invariant by default, locale explicit

Two operations look innocent and are not:

```
  "I" to lowercase        ->  "i"      almost everywhere
                          ->  "ı"      in Turkish
  sort ["Ä", "Z"]         ->  Ä first  in German
                          ->  Z first  in Swedish
```

A program whose output depends on the machine's locale gives different answers on
different machines — the same property lost that platform-dependent
transcendentals lose, and refused there for the same reason.

> **Case conversion and ordering are locale-independent by default. A locale is an
> explicit argument.**

Reproducibility is the default because it is the one a reader can predict from the
source. The locale-aware forms must exist — a RAD language sorting a list of names
for a Swedish user needs them — but they say so.

## §4 — the reactive concatenation runaway, and the watchdog already covers it

Worth flagging while the watchdog is being built:

```
  let log = old log + line
```

An immutable text rebuilt per update is O(n) per update and O(n²) overall, and in
an always-running program the length is unbounded. **That is the denominator
runaway in a different quantity**, and `Draining`'s low-water predicate separates
the two cases correctly on its own: a buffer that is periodically flushed drains, a
log that never is accumulates.

So **text length is a third subject for the same watchdog**, alongside pending
runs and denominator width — which is the argument for extracting the window logic
rather than copying it, arriving one document later than the recommendation.

The repair differs and the fault should say so: for a number it is `fast number`,
for text it is a builder or an explicit flush.

## §5 — storage: UTF-8, and be ready for what that costs

The guide says `text` is UTF-8. **.NET's `System.String` is UTF-16.** That fork has
to be taken deliberately, because it decides every BCL boundary:

- **store UTF-8** (`byte[]` / `ReadOnlySpan<byte>`, `u8` literals, the
  `System.Text.Unicode` helpers) — matches the spec, halves memory for most text,
  and files and sockets already carry UTF-8 so the common path is zero-transcode.
  The cost is transcoding at every `System.String` interop point, and there are
  many.
- **store UTF-16** and treat "UTF-8" as the external encoding only — everything
  BCL is free, and the spec sentence becomes about I/O rather than about the type.

**My lean is UTF-8 storage.** The transcode is at the *edges*, which is exactly
where §2's normalisation pass already sits, so one boundary does both jobs — and
the `fast text` story in §1 is only coherent if the bytes are the storage rather
than an encoding produced on demand. But it is real work and the programmer should
plan for it rather than discover it.

Two smaller notes for the same design: **small-text inlining matters more than SIMD
here** — RAD programs are full of short strings, and avoiding an allocation for a
name beats vectorising a scan over it — and **grapheme boundaries want a lazily
built side index** on long texts, so `@` is not an O(n) scan every time.

## §6 — StringZilla: not now, and here is the trigger

What it is: SIMD byte-level substring and character-set search, edit distances,
sorting, hashing, rolling fingerprints. Good work, and fast at what it does.

**Three reasons it is not the move today.**

**There is no .NET binding.** [v3 ships C, C++, Rust and Swift](https://github.com/ashvardanian/StringZilla/releases/tag/v3.0.0)
plus [Python](https://pypi.org/project/stringzilla/); nothing on NuGet. So this is
not "take a dependency," it is "write and maintain a P/Invoke layer," on top of the
usual native costs — single-file deploy, AOT, the platform matrix.

**The comparison is not SIMD against scalar.** .NET's `IndexOf`/`Contains` on spans
are already vector-accelerated, and
[`SearchValues<string>`](https://github.com/dotnet/runtime/pull/88394) — multi-pattern
search, one of StringZilla's headline features — is **first-party as of .NET 9**.
Ronin targets `net8.0`. **A TFM bump buys a large share of the win for the price of
one line**, and should be tried before anything native is considered.

**And it accelerates the wrong layer.** StringZilla is byte-oriented; it does not do
grapheme segmentation, normalisation or collation. So it speeds up `fast text` and
the search primitives — not the default `text` that §1–§3 describes. Optimising the
escape hatch before the default is built is the wrong order.

**Where it would genuinely earn its keep**, and this is the trigger to write down:
**edit distance and similarity**. The BCL has nothing there, and Ronin has a real
consumer — the resolver's repair suggestions and the editor's completion both want
"did you mean," which is fuzzy matching over the symbol table. If that becomes hot,
StringZilla is the right tool and the P/Invoke is justified by a capability rather
than by a constant factor.

```
  approximation                     successor                  trigger
  BCL span primitives, with a       a native SIMD string       measured symbol-table
  TFM bump for SearchValues         library (StringZilla)      fuzzy matching, or a
                                                               profiled search hot spot
```

## Summary

| | |
|---|---|
| **the decision** | **`text` is indexed, sliced and measured in grapheme clusters** — a reader who has never thought about Unicode writes `name @ 1` and means the first thing they can see |
| the escape hatch | **`fast text`**, mirroring `fast number` — and it gives `fast` one meaning language-wide: **the machine's X rather than the person's X** |
| **normalisation** | **NFC at construction, not at comparison.** Normalising only on compare makes `a is b` true while `length of a` and `a @ 3` disagree — equality the other operations do not respect is not equality |
| **case and order** | **locale-independent by default, locale an explicit argument.** `"I" to lowercase` is `"ı"` in Turkish; a program must not change answers with the machine's locale |
| **the runaway** | `let log = old log + line` is the denominator runaway in another quantity. **Text length is a third watchdog subject** — and `Draining`'s low-water predicate already separates a flushed buffer from a leak |
| **storage** | UTF-8 (the spec) against `System.String`'s UTF-16. **Lean UTF-8** — the transcode sits at the same boundary as normalisation, so one pass does both. Real work; plan it |
| also | **small-text inlining beats SIMD** for RAD workloads, and grapheme boundaries want a lazy side index |
| **StringZilla** | **not now.** No .NET binding (C, C++, Rust, Swift, Python only) — a P/Invoke layer to write and own |
| and | the comparison is **one SIMD implementation against another** — .NET spans are already vectorised, and `SearchValues<string>` is first-party in **.NET 9**. Ronin is on `net8.0`: **try the TFM bump first** |
| and | it accelerates **bytes**, which is the escape hatch, not the default |
| the trigger | **edit distance / fuzzy matching** — the BCL has nothing, and the resolver's repairs and the editor's completion are a real consumer. Revisit when that is measured hot |
