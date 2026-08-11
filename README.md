# MB-RPN — an HP-12C-compatible RPN calculator in Blazor WebAssembly

**MB-RPN** is a .NET Blazor WebAssembly financial calculator with its own dark, glowing
visual identity and an interactive history panel — functionally compatible with the
HP-12C: it implements the functions the way the HP-12C Owner's Handbook describes them
(RPN stack discipline, 10-significant-digit arithmetic, the financial solvers, dates,
statistics, and full keystroke programming) on the same keyboard layout.

## Running

```
dotnet run --project src\MBRPN.Web        # dev server
dotnet test                                # engine test suite (handbook examples)
dotnet publish src\MBRPN.Web -c Release    # static site in bin\Release\net10.0\publish\wwwroot
```

Requires the .NET 10 SDK.

## Projects

| Project | What it is |
|---|---|
| `src\MBRPN.Engine` | UI-independent calculator engine. `CalculatorEngine.Press(KeyId)` in, `DisplayState` out; interaction history with structured replay. |
| `src\MBRPN.Web` | Blazor WASM front end: dark-tech CSS faceplate, glowing SVG seven-segment display, history panel, keyboard mapping, localStorage continuous memory. |
| `tests\MBRPN.Engine.Tests` | xUnit suite (144 tests) driving the engine key-by-key and asserting on display text, using the handbook's worked examples, plus history recording/replay and rename-verification tests. |

## What's implemented

- **RPN stack** — X/Y/Z/T, LAST X, exact stack-lift rules (ENTER/CLx/Σ+ disable lift,
  T replicates on drop), 10-digit mantissa entry, EEX, CHS.
- **Display** — FIX 0–9 with thousands separators, scientific mode (f .), overflow
  `9.999999 99`, mantissa peek (f CLEAR PREFIX), RND.
- **Math** — + − × ÷, yˣ, 1/x, √x, LN, eˣ, n!, FRAC, INTG, %, Δ%, %T with HP domain errors.
- **Storage** — R0–R9, R.0–R.9, STO arithmetic (`STO + 4`), Error 1 on overflow.
- **TVM** — n, i, PV, PMT, FV with the store-vs-compute rule, BEGIN/END modes, odd periods
  (simple or compound via `STO EEX`, C annunciator), n rounded up as on the real unit,
  iterative i solver, 12× / 12÷, CLEAR FIN.
- **AMORT** — per-payment interest rounded to the current FIX setting (results depend on
  the display setting, exactly like the hardware); INT (360- and 365-day simple interest).
- **NPV / IRR** — CF₀/CFⱼ/Nⱼ stored in the real machine's registers, grouped discounting,
  bracketing solver, Errors 3/7.
- **Bonds** — f PRICE / f YTM, semiannual actual/actual with the final-period simple-interest case.
- **Depreciation** — SL, SOYD, DB with salvage floor.
- **Dates** — D.MY / M.DY, g DATE with weekday digit, g ΔDYS (actual + 30/360), Error 8.
- **Statistics** — Σ+/Σ− into R1–R6, x̄/ȳ, x̄w, s, linear estimates x̂,r / ŷ,r.
- **Programming** — P/R, 8 lines growing to 99 by converting registers (7 lines each),
  keycode display (`43,33 00`), R/S, SST/BST, GTO nn, GTO . nn editing, PSE, x≤y, x=0,
  g MEM (`P-08 r-20`), non-blocking run loop in the UI.
- **Continuous memory** — full state persists to localStorage; ON toggles the display off
  without losing anything.
- **Interactive history** — the panel beside the calculator records meaningful
  operations (coalesced number entry + operation, e.g. `100 ENTER`, `25 +` with its
  result). Every entry is stored structurally as its logical key sequence; clicking one
  replays exactly that operation against the calculator's **current** state (no snapshot
  restore) and records itself as a new entry. Clear button empties it; history is
  session-only. On narrow screens it moves below the calculator; in mobile-landscape
  full-screen mode it is hidden.

## Keyboard shortcuts

digits `0–9` `.` `Enter` `+ − × ÷` `%` — as printed. `f` `g` prefixes; `s` STO, `r` RCL,
`n` `i`, `p` PV, `m` PMT, `v` FV, `c` CHS, `e` EEX, `y` yˣ, `x` x≷y, `d` R↓, `q` 1/x,
`l` %T, `Space` R/S, `↓` SST, `Backspace`/`Delete` CLx, `Esc` ON.

## Fidelity notes

- Numbers are stored as a 10-significant-digit decimal mantissa plus exponent (±99),
  mirroring the HP-12C's BCD registers, so `1 ÷ 3 × 3 = 1.00` and FIX rounding behave
  like the hardware. Transcendentals compute in double precision and round back to 10 digits.
- Iterative solvers (i, IRR, YTM) bracket and bisect exactly the classes of roots the
  handbook documents, and report the handbook's error numbers when no single answer exists.
- The 7-segment display, teal/violet shifted legends, CLEAR rule, and tall ENTER key are
  drawn entirely with CSS/SVG — no images. The layout matches the HP-12C keyboard; the
  visual identity (graphite case, glowing segments) is MB-RPN's own.
- On touch devices the keyboard-shortcut hints are hidden and the calculator scales to
  the viewport; in landscape it fills the screen edge-to-edge like a real calculator
  (`mbrpn.fit()` in `wwwroot/js/interop.js`; append `?mobile=1` to preview on desktop).
