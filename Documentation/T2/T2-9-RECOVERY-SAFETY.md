# RDA T2-9 Vehicle Recovery and Safety

Status: IMPLEMENTED — included in the final T2 runtime gate.

## Goal
Prevent the learner from becoming stuck after a rollover, fall, or bad maneuver.

## Implemented
- Manual RESET button in the training HUD.
- Backspace keyboard shortcut for recovery.
- Last safe upright position is captured while the car is grounded and moving slowly.
- RESET returns the vehicle to the last safe pose with zero linear/angular velocity.
- Gear is returned to N.
- Handbrake is engaged automatically after reset.
- Steering, throttle, brake and clutch inputs are cleared.
- Turn indicators are cancelled after reset.
- Automatic recovery if the vehicle falls below the configured world height.
- Automatic recovery after remaining upside down for several seconds.

## Runtime acceptance
T2-9 passes if:
1. RESET works while parked and while stuck.
2. Car returns upright near the last safe location.
3. Speed returns to zero.
4. Gear becomes N.
5. Handbrake becomes ON.
6. Throttle/brake/steering are cleared.
7. Console remains at 0 errors.
