# RDA T2-10 Final Vehicle Gate

Status: READY FOR FINAL UNITY RUNTIME TEST

## Scope completed in T2
- T2-5: chassis / WheelCollider stability.
- T2-6: drivability, steering, braking, safe R/N/D behavior and hybrid input.
- T2-7: cleaner prototype cockpit plus driver/chase cameras.
- T2-8: lights, indicators, hazards, brake/reverse lamps, ABS/TCS feedback.
- T2-9: manual and automatic vehicle recovery.
- T2-10: consolidated Unity editor and runtime gate.

## Editor gate
Run:

Real Driving Academy > T2 > Build & Validate Vehicle Stage

Expected result:

RDA T2 EDITOR VALIDATION PASSED

The validator checks:
- Training sedan and Rigidbody.
- VehicleConfig.
- Four WheelColliders and wheel meshes.
- Neutral start and handbrake ON.
- CarSystems and input.
- Driver/chase cameras.
- Exactly one active Audio Listener.
- Functional light visual controller.
- Headlamps, brake lamps, reverse lamps and turn indicators.
- RESET / CAM / HAZ / LIGHT / R / N / D controls.
- HUD and EventSystem.
- Stage2_TrainingGround registered in Build Settings.

## Final runtime checklist
1. Start in N with HB ON.
2. BELT then IGN.
3. Select D, release HB, accelerate and steer.
4. Brake to a controlled stop.
5. CAM switches cockpit <-> chase view.
6. LEFT and RIGHT blink the correct side.
7. HAZ blinks both sides.
8. LIGHT turns on the forward lamps.
9. BRAKE activates the rear brake lamps.
10. R activates reverse lamps and reverse driving remains controlled.
11. Unsafe D <-> R change while moving is rejected.
12. RESET stops the car, returns it upright, selects N and engages HB.
13. F3 can show/hide drivetrain diagnostics.
14. Console remains at 0 errors.

## Completion rule
T2 becomes RDA-T2-PASSED only after:
- Editor gate passes.
- Final runtime checklist passes.
- Console remains free of RDA-related errors.
