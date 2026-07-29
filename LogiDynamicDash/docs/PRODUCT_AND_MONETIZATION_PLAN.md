# Product and Monetization Plan

## Product Position

LogiDynamicDash should make the confirmed RS50 Dynamic OLED useful without
requiring users to understand HID++, report layouts, or telemetry internals.
The product promise is:

> Install, choose a racing discipline, preview the result, and get a safe,
> readable in-wheel dashboard that adapts to the current driving context.

RS50 is the only confirmed device. PRO remains a future compatibility target
until its identity and collection contract are physically verified.

The OLED exposes ten firmware-rendered layouts rather than an arbitrary
framebuffer. Product language must therefore say "layout and data
customization," not custom fonts, unrestricted graphics, or pixel drawing.

## Current GUI Milestone

The `LogiDynamicDash.Configurator` Windows application is hardware-free. It:

- applies reviewed Road and Oval recommendations;
- selects a layout for Normal, Brake Bias, Last Lap, and Connection Problem;
- edits speed unit, maximum RPM, and gauge maximum speed;
- renders a typed semantic preview;
- opens strict schema v1/v2 files and saves schema v2;
- never enumerates or opens HID devices.

The first GUI deliberately does not start telemetry, arm a physical trial,
manage licenses, sign users in, or write to the OLED.

## Discipline Recommendations

### Road

| Mode | Default | Rationale |
|---|---:|---|
| Normal | E | RPM gauge, speed indicator, prominent speed, and gear support frequent shifts and variable corner speeds. |
| Brake Bias | H | Two large rows make a temporary setup change easy to verify. |
| Last Lap | J | Four centered rows give lap identity and time maximum clarity. |
| Connection Problem | H | A large two-row warning is harder to confuse with live telemetry. |

Initial scales: 8,000 RPM and 300 km/h or 190 mph. They remain editable
because GT, prototype, formula, and road cars differ substantially.

### Oval

| Mode | Default | Rationale |
|---|---:|---|
| Normal | D | Keeps RPM and speed indicators visible while using compact text for gear/status; gear changes are less frequent than in Road. |
| Brake Bias | H | Makes an adjustment readable without a dense race page. |
| Last Lap | J | Lap time is central to pace and tire-run evaluation. |
| Connection Problem | H | Uses the same unmistakable warning page as Road. |

Initial scales: 9,000 RPM and 360 km/h or 225 mph. Short-track and stock-car
profiles will eventually override these values per car.

### Automatic Selection

Automatic Road/Oval selection is not implemented yet. It requires trustworthy
session metadata for the current car/category and a reviewed fallback policy:

1. read simulator-provided category metadata;
2. normalize it to `Road`, `Oval`, or `Unknown`;
3. select the matching profile only when confidence is exact;
4. retain the user's last manual choice for `Unknown`;
5. show the selected discipline in the GUI and local diagnostic;
6. allow a per-car override.

The application must never infer discipline from speed, steering, track name,
or other heuristics that could switch the OLED while driving.

## Recommended Free Scope

Free must be a complete and safe product, not a demo:

- confirmed RS50 support and all compatibility/safety fixes;
- live gear, speed, RPM, brake-bias, last-lap, and connection states;
- curated Road and Oval profiles;
- automatic Road/Oval selection once verified;
- KMH/MPH and gauge scale controls;
- one active Road profile and one active Oval profile;
- all firmware layouts A-J;
- offline preview, telemetry recording, and replay;
- local JSON import/export;
- configurator, updates, documentation, and community support;
- no account required for local operation.

Safety controls, device fixes, data portability, and the ability to use the
OLED should never be paywalled.

## Recommended Paid Scope

Paid value should come from depth, convenience, and profile management:

- unlimited named profiles and per-car overrides;
- custom assignment of supported telemetry fields to typed text slots;
- conditional rules such as qualifying/race, pit limiter, fuel warning, or
  timed temporary pages;
- advanced thresholds and alerts;
- profile duplication, comparison, version history, and backup;
- community profile library and signed profile distribution;
- multi-simulator profile synchronization;
- advanced replay analysis and configuration recommendations;
- priority support and early access to newly confirmed devices;
- commercial venue licensing and managed multi-seat deployment.

Custom field assignment must remain constrained by each confirmed layout's
field lengths and semantic types. Paid status cannot unlock unknown HID
functions, raw reports, arbitrary graphics, firmware access, or unsafe rates.

## What Not to Put Behind the Paywall

- OLED connectivity and basic telemetry;
- Road/Oval automatic selection;
- layouts A-J themselves;
- stationary/movement safety gates;
- bug fixes and new confirmed device compatibility;
- local configuration export;
- privacy controls and diagnostic access;
- recovery from a paid-plan downgrade.

This boundary keeps free users safe and useful while making Pro attractive to
racers who manage many cars and want deep automation.

## Open-Source Licensing Reality

The repository is MIT licensed. Anyone may use, modify, sublicense, or sell
the published source while retaining the license notice. A hard paywall inside
the same public source can therefore be removed by a fork.

Recommended business model:

- keep the protocol, safety layer, standard dashboard, and local configuration
  open;
- sell signed convenience builds, automatic updates, advanced profile tools,
  hosted synchronization/library features, and priority support;
- keep licensing/account code in a separately reviewed commercial service or
  distribution layer if the project chooses that direction;
- avoid DRM inside the HID transport or safety-critical path.

Do not implement billing until physical production validation and free-product
retention demonstrate real demand.

## Pricing Hypothesis, Not a Decision

Current sim-racing products validate both approaches:

- RaceLab offers a free tier, a €4.90 monthly Pro plan, annual pricing, a
  high-priced lifetime option, and commercial seats.
- Lovely Sim Racing advertises a free start with membership from €1/month.
- SimHub sells licenses and a separate paid Motion add-on, demonstrating that
  hardware-adjacent advanced capability can be a one-time purchase.

Recommended experiment after beta:

- Free: complete core described above;
- Pro Individual: low one-time early-adopter license, including one year of
  updates;
- optional annual renewal for new Pro features and hosted services;
- separate commercial per-seat license;
- no subscription requirement for basic local OLED operation.

Pricing should be tested with a waitlist and survey before payment code.

Official market references:

- RaceLab plans: https://racelab.app/?anchor=membership
- Lovely Sim Racing membership positioning: https://store.lsr.gg/
- SimHub licensing terms: https://www.simhubdash.com/terms-and-conditions/
- SimHub paid Motion add-on: https://www.simhubdash.com/simhub-motion-addon-licence/

## Release Plan

### 0.2 Technical Preview

- typed RS50 protocol and bounded hardware route;
- offline preview, simulation, recorder, and replay;
- hardware-free configurator;
- manual Road/Oval recommendations;
- stationary production smoke test still required.

### 0.3 Hardware Beta

- successful stationary production gate;
- explicitly authorized moving validation;
- installer and signed release candidate;
- automatic discipline metadata investigation;
- local crash/fault reporting with explicit opt-in.

### 0.4 Profile Beta

- automatic Road/Oval selection with `Unknown` fallback;
- per-car identity model;
- free profile activation;
- experimental Pro profile editor without billing enforcement;
- user research on customization demand.

### 1.0 Free

- stable RS50 live use;
- complete Road/Oval defaults;
- documented PRO status;
- updater and migration guarantees;
- accessible GUI and onboarding.

### Pro Launch

Only after 1.0 stability:

- per-car profiles and conditional rules;
- advanced typed-field editor;
- licensing and entitlement service outside the safety path;
- commercial deployment option.

## Next Feature Decisions

Before implementing automatic profiles or Pro entitlements, decide:

1. whether the commercial distribution will remain MIT or use an open-core
   split for new commercial components;
2. whether Pro is one-time, subscription, or one-time plus update renewal;
3. which simulator supplies the first trusted Road/Oval car metadata;
4. whether community profiles require hosted accounts;
5. whether user telemetry always remains local by default;
6. which supported telemetry fields are safe and legible in every layout.
