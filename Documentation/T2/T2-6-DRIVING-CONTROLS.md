# RDA T2-6 Driving Controls

Status: IMPLEMENTED — awaiting Unity runtime/device gate.

## Goal
Make the training sedan predictable and controllable at low and medium speeds before visual polish.

## Implemented
- Speed-adaptive steering: full steering angle at parking speeds, reduced steering authority as speed increases.
- Launch torque limiter to reduce sudden lurch from standstill.
- Additional reverse torque reduction and a reverse-speed cap.
- Brake input suppresses drive torque so throttle and brake do not fight each other.
- Handbrake suppresses drive torque until deliberately released.
- Light drivetrain coast drag when in gear and off-throttle.
- Signed vehicle speed used to distinguish forward and reverse travel.
- Unsafe R↔D direction changes are rejected while the vehicle is moving above the configured threshold.
- Handbrake UI latch now synchronizes with the actual input state.
- Tuning values are generated into the prototype VehicleConfig.

## Current tuning
- Full steering until: 18 km/h
- Reduced steering target: 80 km/h
- High-speed steering multiplier: 0.38
- Launch torque multiplier: 0.62
- Reverse torque multiplier: 0.55
- Reverse max speed: 25 km/h
- Safe direction-change threshold: 3 km/h
- Coast brake torque: 110

## Runtime acceptance gate
T2-6 passes only if:
1. Car starts in N with handbrake engaged.
2. Selecting D while nearly stopped works.
3. Releasing handbrake and applying light throttle produces a controlled launch.
4. Brake stops the car without a violent flip or launch.
5. Steering is responsive at parking speed but less aggressive at higher speed.
6. Selecting R while still moving forward faster than 3 km/h is rejected.
7. Reverse remains controllable and does not accelerate indefinitely.
8. Handbrake state remains synchronized between keyboard and UI.
9. Console stays at 0 errors.
