# RDA T2-8 Vehicle Systems and Driver Feedback

Status: IMPLEMENTED — awaiting Unity runtime/device gate.

## Goal
Make key vehicle systems visible to the learner without changing the stable driving physics.

## Implemented
- ABS runtime activity flag in RealisticCarController.
- TCS runtime activity flag in RealisticCarController.
- Physical prototype headlamps controlled by LIGHT.
- Spot-light low beams for visible forward illumination.
- Brake lamps driven by brake pedal input.
- Reverse lamps driven by R gear.
- Front/rear left and right indicator lamps with blinking.
- Hazard button (HAZ) using the existing hazard logic.
- System status HUD for HB / LEFT / RIGHT / HAZ / LIGHT / ABS / TCS.
- Existing drivetrain debug remains available with F3.

## Runtime acceptance gate
T2-8 passes only if:
1. LEFT blinks the left indicator lamps and HUD shows LEFT.
2. RIGHT blinks the right indicator lamps and HUD shows RIGHT.
3. HAZ blinks both sides and HUD shows HAZ.
4. LIGHT turns on front lamps and forward spot lights.
5. BRAKE illuminates rear brake lamps.
6. R illuminates the reverse lamps.
7. HB status follows the actual handbrake state.
8. ABS/TCS indicators can appear when those assists intervene.
9. CAM still switches correctly.
10. Driving, braking and steering remain unchanged from T2-6.
11. Console remains at 0 errors.

## Note
ABS/TCS activation depends on wheel slip, so normal gentle driving may not trigger them every time. Their code path and HUD feedback are nevertheless wired and testable under hard braking or wheel slip.
