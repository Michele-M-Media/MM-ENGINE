# Hardware test log — v0.1-alpha

## Purpose

This log records the staged isolation sequence used to move from a generic `CE-108255-1` failure toward a specific subsystem boundary.

## Boot probe sequence

| Probe | Added behavior | Result | Interpretation |
|---|---|---|---|
| R | extremely reduced runtime-only shape | `CE-108255-1` | too artificial to represent the normal application route |
| S | reduced services-only shape | `CE-108255-1` | superseded diagnostically by normal-path Probe 0 passing |
| 0 | SharpProspero display | PASS | normal app bootstrap + display/frame route works |
| 1 | SharpProspero pad | PASS | pad route works in staged app |
| 2 | MM ENGINE canvas | PASS | engine canvas integration works |
| 3 | MM ENGINE Platform 2D | PASS | engine platform 2D frame route works |
| 4 | aggregate assets/modules/font/PNG | `CE-108255-1` | useful boundary begins in asset block |

## Asset split

The asset block was split so that failures could not be attributed to an entire subsystem at once.

The follow-up sequence advanced through:

- PngDec module loading;
- Font + FontFt module loading;
- direct `/app0/assets` file reads;
- further staged setup immediately before font construction.

Those stages passed. The next useful failure occurs at the TrueType font construction path, centered on `TrueTypeFont.Load`.

## Current next diagnostic target

Keep all stages that already passed unchanged. Instrument/validate the minimal TrueType font-load operation before adding glyph draw, PNG decode, UI composition, or the full Hello2D scene back into the test.

Do not regress to broad theories about package structure, display initialization, or basic pad initialization unless new evidence contradicts the passing staged probes.
