# Hot reload — installing a minted body into a running program

> **Ledger** — `[R]` Flags the live half of `MIDSESSIONDESIGN` §0 as a hot-reload gap, per its §7.2: installing a newly monomorphised body into a program already running, for which no path exists today.
> supersedes: none
> superseded by: none

**From:** the successor, at `70d5649`, answering `MIDSESSIONDESIGN` §7.

`MIDSESSIONDESIGN` §0 collapses mid-session monomorphisation to an **edit-time** event —
a call site's argument tuple is ground and static, so new instantiations come from
source changes, not from values flowing at runtime — and hands the one thing that stays
live to hot reload: *"installing a newly minted body into a program that is already
running."* Its §7.2 asks whether a usable hot-reload path exists, and directs that if
not, *"the gap is real but it is a hot-reload gap, not a monomorphisation one — and it
should be ledgered under that name."* This is that row.

## The finding

There is no hot-reload path. `HANDOFF.md` records it plainly: *"Not in either repo —
`Ronin` and `Ronin.net` contain no hot-reload code, and the only mention anywhere is the
word 'hot-reloadable' in the README goals."* So the live-installation half of §0 has
nowhere to land today.

The gap is **not** the monomorphisation design, which `MIDSESSIONDESIGN` settles —
what an instantiation is, what mints it, what invalidates and evicts it, and the cache
contract are all decided, and the checker's step-4 use of them needs no running program.
The gap is the mechanism that takes a body the checker has newly instantiated and
installs it into a session that never restarts.

## What the design must settle

- **The delta.** `HANDOFF.md` names the modern shape twice over: .NET's own Hot Reload
  computes metadata/IL deltas against a baseline and applies them through
  `MetadataUpdater.ApplyUpdate`; React Fast Refresh keeps state when a shape is unchanged
  and re-initialises when it is not. `HANDOFF` records that this project already arrived
  at both halves — a delta model and a copy-what-did-not-change heuristic — so the
  question is their contract, not their invention.
- **The boundary with invalidation.** `MIDSESSIONDESIGN` §3 routes cache invalidation
  through the existing dependency graph's **cutoff**; a reload that lands mid-body is the
  worst case that graph already names (`Graph.cs`). The reload path and the invalidation
  path meet there, and the design should say how.
- **Scope.** This gates nothing the successor is building: capture, return inference, and
  the monomorphisation cache are all check-time. It is the runtime installation of what
  they produce, ledgered here so it is not rediscovered when a running session first needs
  a body it did not start with.
