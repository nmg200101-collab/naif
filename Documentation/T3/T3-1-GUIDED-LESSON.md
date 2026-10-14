# RDA T3-1 Guided Driving Lesson

Status: IMPLEMENTED — static gate pending.

## Goal
Turn the training ground into a structured driving lesson instead of free driving only.

## Lesson flow
1. Fasten seat belt.
2. Start engine.
3. Select D.
4. Release handbrake.
5. Use left indicator.
6. Reach 20 km/h.
7. Pass the training checkpoint gate.
8. Stop inside the target stop zone.
9. Select N and engage handbrake.

## Added
- Ordered lesson state machine.
- Current instruction and progress API.
- Checkpoint trigger zone.
- Stop target trigger zone.
- Visible checkpoint gate.
- Visible stop target.
- HUD lesson progress (1/9 ... 9/9).
- Lesson completion event for later scoring/progress persistence.

## Gate
T3-1 is internally complete when the static check passes. Runtime testing will be grouped with later T3 stages into a single device gate.
