# UnityGradientBackground

Get a beautiful background gradient just by adding a simple script to your camera!

![](readme/example.gif)

## Features
+ Supports horizontal, vertical and radial gradients
+ Supports up to 6 colors
+ Multiple radial ratio policies
+ Invert direction
+ Custom radial origin
+ Editor preview

## Performance
+ Background is rendered after opaque objects to avoid unnecessary overdraw
+ Optimized shaders that avoid dynamic if conditions
+ Option to "bake" the background texture, thereby exchanging memory for performance

## Instructions

1. Import the `Packages/GradientBackground` to your project
2. Add the `GradientBackground` component to your camera.

