# RDA T3-2 Violations and Scoring

Status: IMPLEMENTED — included in the T3 static gate.

## Added
- Score starts at 100.
- Letter grade A/B/C/D/F derived from the live score.
- Speeding penalties use cooldowns to avoid frame-by-frame spam.
- Seat-belt violation is recorded once per session.
- Missing the required indicator at the training checkpoint deducts points.
- Collision penalty sensor deducts points for meaningful impacts.
- Existing red-light and speed-limit systems continue to feed the same score.
- HUD shows both Score and Grade.
- Score-change event is available for later result screens and persistence.

## Purpose
T3 training is now evaluated, not only guided.
