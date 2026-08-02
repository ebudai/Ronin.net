# The displacement counter is now stricter than anything observable

A disclosure rather than a question, in the same class as `LeadingHole` and the
anchor-run form of R6: a rule the runtime enforces that no program can currently
be written to detect.

## What changed

`REAUDIT20` found the fourth form of one shape. The credit was owned by the right
wait and the right generation, and then the **round** was declared free on the
strength of it — so a queue draining three deep handed its exemption to an
unrelated `when` writing what its own trigger reads, and a runaway configured to
be caught after two rounds fired five times.

The repair is that an exemption belongs to the firing that earned it. A round is
free only when everything in it was servicing inherited work.

## What that does to the `2k` bound

Q2 settled that each inherited run carries two credits: one for being displaced,
one for being drained. The displacement credit is a counter, and spending it is
what bounds the exemption at `2k` rounds for `k` inherited runs.

Under the new round rule I could not construct a program in which that counter's
**exhaustion** changes any outcome. Three attempts:

1. the previous test's shape — a phaser re-arming the head. Its phaser fires in
   every round, so no round is ever free and the credit is never consulted. It
   gave identical results with the counter, with a graph-wide pool, and with
   unlimited credit, which means it had stopped testing anything at all. Deleted
   rather than renumbered.
2. a three-segment chain where position 2 claims and position 3 is displaced
   repeatedly. Identical results with and without the counter.
3. a chain re-arming its own head, to starve its tail in pure rounds for ever.
   It settles; the head only re-arms every other round, and in the gap the tail
   gets its turn.

Removing the counter entirely — displacement forgiven while a run is still
parked at that wait, without spending anything — passes all 867 tests, including
the hang control and the Q2 both-free control.

So on the evidence the counter is currently unobservable. It is **kept**, for
three reasons:

- it is what `Q2SETTLED.md` decided, and I have no program that argues against
  it;
- it is what makes the maintained `2k` bound true rather than aspirational, and
  a bound nobody can currently reach is still a bound; and
- "I could not construct one" is not "none exists". The three attempts above rule
  out the shapes I thought of, and this project has twice found the fourth shape
  after three looked exhaustive.

## What that means for the tests

`AndOneParkedRunForgivesOneDeferralNotEveryOneAfterIt` is gone. It was the
positive-cap test written for `REAUDIT18`, and it was correct then; the round
rule made it vacuous, and I could not rebuild it. Keeping a test whose expected
numbers I would have had to update to whatever the code now does — while it
passed equally with the mechanism deleted — is the exact failure `Q2SETTLED.md`
§2 named.

What remains and is non-vacuous, each verified by sabotage:

| test | breaks when |
|---|---|
| a round that deferred work did not fail to settle | the exemption is removed |
| and a step that inherited nothing is forgiven nothing | the cap is removed — it hangs rather than fails |
| and a run pays for its own displacement and its own drain, both | the two credits are collapsed into one |
| and no other chain's parked run can be spent on it | the credit is pooled across chains |
| and a run that has already drained pays for nothing after it | the drain does not retire the credit |
| and a draining queue buys nothing for an unrelated runaway | the exemption frees the whole round |
| and neither does a run being displaced and then drained | the same, for the other credit |
| and the report says which rounds it counted and how many there were | servicing rounds are counted, or the two figures are conflated |

Two of these needed rebuilding for the round rule, because their drivers fired
in every round: the round that matters has to contain only the firing under
test, so the drivers are now one-shot `when`s whose last one arms the head and
opens the wait in a single write.

## If you would rather it went

Say so and it goes, along with the clamp that keeps it from outliving its run —
one balance per wait, read but not spent. That is simpler, and every test still
passes. The cost is that the `2k` sentence in the spec would have to be retired
in favour of "while a run it inherited is still standing there", which is weaker
and, on today's evidence, indistinguishable.
