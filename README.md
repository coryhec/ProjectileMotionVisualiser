# 2D Projectile Motion Simulator

A C# Windows Forms project for simulating projectile motion and visualising a projectile's trajectory.

Displacement in Z axis (Height) against Displacement in Y Axis (Forward)

## Features

- Animate a projectile along its calculated path.
- Set the initial speed, launch angle, mass, and sphere radius.
- Choose between air, water, and vacuum.
- Model quadratic drag using `F_d = kv²`.
- Display the projectile's coordinates during the simulation.
- Click the trajectory to inspect a point.
- Drag the graph to pan around the plot.
- Show axis tick marks and coordinate values.

## Physics Model

The simulator calculates projectile motion numerically in small time steps. In a medium with quadratic drag, the drag force is modelled as

```text
F_d = ½ρC_dAv²
```

where:

- `ρ` is the density of the medium.
- `C_d` is the drag coefficient.
- `A` is the object's cross-sectional area facing the flow.
- `v` is the projectile's speed relative to the medium.

In the current implementation, the drag constant is calculated using the density of the selected medium, a sphere drag coefficient, and the sphere's cross-sectional area:

```text
k = ½ρC_dA,    A = πr²
```

This is a simplified model. Real drag depends on factors such as shape, orientation, surface characteristics, and flow conditions.

## TODO

### Physics and Calculation
- [ ] Improve drag calculations to account for the projectile's shape, orientation, and cross-sectional area.
- [ ] Allow different drag coefficients for different shapes instead of assuming a sphere.
- [ ] Consider surface roughness and other factors that affect drag.
- [ ] Make the drag model more realistic across different speeds and flow conditions.
- [ ] Add component force displays, including gravity, drag, and the net force.
- [ ] Add more accurate or configurable numerical integration methods and time-step settings.

### Graphs and Visualisation
- [ ] Add a displacement-versus-time graph.
- [ ] Add trajectory envelopes by plotting multiple trajectories on the same graph.
- [ ] Add options to compare different launch angles, speeds, masses, or drag settings.
- [ ] Improve graph scaling and navigation for a wider range of simulations.

### More Mediums
- [ ] Add more selectable mediums, with their densities and relevant drag properties.

Possible mediums to add:
- [ ] Different air conditions, such as cold air, hot air, or high-altitude air.
- [ ] Helium.
- [ ] Carbon dioxide.
- [ ] Fresh water and salt water.
- [ ] Oil.
- [ ] Other liquids, such as glycerine.
- [ ] Custom medium settings so users can enter density and other properties themselves.

### Possible 3D Version
- [ ] Explore moving the project to Unity for full 3D rendering.
- [ ] Add 3D projectile shapes and a camera that can orbit around the scene.
- [ ] Visualise the trajectory in three dimensions.
- [ ] Compare multiple 3D trajectories in the same scene.

## Project Files

- `Form1.cs` — Windows Forms interface, graph drawing, input controls, and animation.
- `MotionMath.cs` — numerical calculation of projectile motion.

## Notes

The current version is a 2D simulation and uses a simplified drag model. Results are approximate, especially when drag is significant. The medium list currently contains air, water, and vacuum.
