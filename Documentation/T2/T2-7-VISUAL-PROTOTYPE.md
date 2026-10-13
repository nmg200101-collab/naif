# RDA T2-7 Visual Prototype and Cameras

Status: PASSED — Unity runtime/device visual gate completed successfully.

## Goal
Improve the training sedan prototype visually without changing the physics that passed T2-5/T2-6.

## Implemented
- Cleaner body proportions for hood, roof, rear body, doors and bumpers.
- Added simple headlamps and tail lamps for clearer exterior shape.
- Cleaner dashboard and center console proportions.
- Large prototype shell pieces are hidden from the driver camera to reduce cockpit obstruction.
- Driver camera moved to a more natural head position.
- Driver FOV increased to 74 degrees and near clip adjusted to 0.10 m.
- Added a chase/exterior camera.
- Added CAM button and keyboard C to switch driver/chase views.
- Audio Listener follows the active camera to avoid duplicate listeners.
- Drivetrain debug HUD is hidden by default; F3 toggles it for diagnostics.

## Acceptance gate
T2-7 passes when:
1. Driver view no longer has the large roof/windshield/door surfaces blocking most of the screen.
2. Steering wheel, dashboard and road ahead are visible from the cockpit.
3. CAM switches cleanly between driver and chase camera.
4. Only one Audio Listener is active at a time.
5. Exterior prototype reads clearly as a car even though it is not the final 3D model.
6. Physics behavior from T2-6 remains unchanged.
7. Console remains at 0 errors.

## Important
This is still a functional prototype. The final high-detail vehicle model, licensed/owned art, driver hands and character animation are later visual-production stages.


## Runtime result
- Driver view is unobstructed enough for training.
- CAM successfully switches to chase/exterior view inside Game mode.
- Vehicle remains stable and drivable after camera/visual changes.
- Console remained at 0 errors and 0 warnings in the accepted test.
