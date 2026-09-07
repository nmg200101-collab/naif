# REAL DRIVING ACADEMY — RDA-V50 CLEAN REBUILD

## Scope
V50 is foundation only. Final vehicle art, academy environment, city, traffic, exam content and release polish are explicitly out of scope until their gated phases.

## Official baseline
- Repository: `nmg200101-collab/naif`
- Branch: `rda-v50-clean-rebuild`
- Unity: `2022.3.62f2 LTS`
- Baseline commit: `db7d725a3426da5fa2147a2a1af0cc3299a84e33`
- Safety archive: `RDA-V50-SAFE-BASE-KEEP-2026-09-07.zip` — immutable backup; never overwrite.

## Canonical source root
New foundation work lives under `Assets/RealDrivingAcademy/`. The older `Assets/RealDrivingAcademyV50/` root is treated as legacy candidate code until audited. It must not silently become the architecture of the clean rebuild.

## Required scene flow
`RDA_Boot` → `RDA_Login` → `RDA_MainMenu`

Login will support account/guest at V51 shell level; V50 establishes deterministic scene routing only.

## Engineering gates
No V50 freeze is valid without a real Unity compile, missing-reference validation, required-scene validation, Android build, and runtime smoke test. See `RDA_V50_ACCEPTANCE_CHECKLIST.md`.

## Version policy
Do not advance to V51 and do not create `RDA-V50-FOUNDATION-FROZEN` until all mandatory V50 gates pass.
