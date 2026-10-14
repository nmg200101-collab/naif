# RDA T4-1 Vehicle Visual Layer

Status: IMPLEMENTED

- Existing physics/control hierarchy is preserved.
- Prototype renderers are grouped under VisualModel_Old and hidden.
- Sedan_Realistic becomes the active visual root.
- Smooth generated sedan body mesh replaces the blockout shell.
- New wheel visuals are rebound to the existing WheelColliders.
- Steering wheel visual is rebound to a smooth torus mesh.
- VehicleVisualLayer allows the prototype/realistic visual layer to be swapped without touching physics.
