# RDA T2-5 Vehicle Stability

Status: IMPLEMENTED — awaiting device/Unity runtime gate.

## Confirmed issues from T1 device test
1. Vehicle spawned too high above the road and dropped onto the suspension.
2. Driver camera used its own transform as its look anchor, causing rotation accumulation.
3. Parking brake was released automatically by keyboard polling on desktop.
4. Vehicle started in a forward gear instead of Neutral.
5. Prototype chassis had no physical body collider.

## Fixes
- Spawn height changed to 0.13 m for the current training-road geometry.
- Center of mass moved inside the chassis volume.
- Added a chassis BoxCollider.
- Increased angular damping and limited maximum angular velocity.
- Suspension spring/damper retuned for 1450 kg.
- Tire friction stiffness increased moderately.
- Wheel force application point set explicitly.
- Added a separate DriverHeadAnchor.
- Driver camera now follows the independent anchor.
- Near clip plane raised to reduce cockpit clipping.
- Parking brake keyboard control changed to toggle.
- Automatic car now starts in Neutral.

## Device acceptance test
T2-5 passes only when:
- Car remains upright and settled for 10 seconds while stationary.
- No visible bounce or large roll while parked.
- Driver camera does not rotate or drift by itself.
- Cockpit geometry no longer sweeps across the camera due to rotation accumulation.
- Releasing handbrake is deliberate.
- Low-speed acceleration and braking do not flip or launch the car.
- Console remains at 0 errors.
