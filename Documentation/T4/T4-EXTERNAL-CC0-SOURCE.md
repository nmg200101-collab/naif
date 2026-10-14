# T4 External Visual Source

The T4-5 vehicle silhouette and several exterior-detail proportions are adapted from the public-domain procedural source:

- Project: Spectral GT RS
- Author: JaronKBragg7337
- Source repository: https://github.com/JaronKBragg7337/spectral-gt-rs
- Source file: spectral_gt_car.py
- License: CC0 1.0 Universal
- Original authored units: meters
- Original front axis: +X

The RDA Unity implementation does not copy the Blender binary into the project. It ports the CC0 procedural geometry profile into the Unity editor builder and maps axes to the RDA vehicle convention.

Coordinate mapping:
- Spectral lateral Y -> Unity local X
- Spectral vertical Z -> Unity local Y
- Spectral longitudinal/front X -> Unity local Z

This keeps the RDA physics rig independent while providing a substantially higher-detail visual foundation.
