# Vehicle Physics System - Technical Reference

## Overview

This is a modular, physics-based vehicle simulation system for Unity. It implements realistic wheel dynamics, tire friction, suspension, powertrain, and braking systems with full support for drifting mechanics.

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Core Systems](#2-core-systems)
3. [Tire & Friction Systems](#3-tire--friction-systems)
4. [Powertrain System](#4-powertrain-system)
5. [Brake System](#5-brake-system)
6. [Controller Systems](#6-controller-systems)
7. [Physics Formulas](#7-physics-formulas)
8. [Drifting Mechanics](#8-drifting-mechanics)
9. [Tuning Guide](#9-tuning-guide)

---

## 1. System Architecture

### Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              INPUT LAYER                                     │
│  [VehicleInputController] → throttle/brake/handbrake/steering               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CONTROL LAYER                                      │
│  [SteeringInput] → wheel angles    [PowertrainSystem] → drive forces        │
│  [BrakeSystem] → brake forces                                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          PHYSICS LAYER                                       │
│  [SuspensionSystem] → wheel states, normal forces                           │
│  [SlipCalculator] → slip ratio, slip angle, friction coefficients           │
│  [WheelDynamics] → wheel angular velocity (omega)                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          OUTPUT LAYER                                        │
│  [TireForcesApplier] → applies forces to Rigidbody via AddForceAtPosition   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Execution Order

| Order | Component | Responsibility |
|-------|-----------|----------------|
| Default | SuspensionSystem | Raycast, spring/damper forces, wheel states |
| Default | PowertrainSystem | Engine torque, wheel drive forces |
| Default | BrakeSystem | Brake force distribution |
| Default | SlipCalculator | Slip ratio, slip angle, μ values |
| **20** | WheelDynamics | Integrate wheel angular velocity |
| Default | TireForcesApplier | Apply final tire forces to Rigidbody |

---

## 2. Core Systems

### 2.1 SuspensionSystem

**Location:** `Scripts/VehiclePhysics/Suspension/SuspensionSystem.cs`

**Purpose:** Raycast-based suspension with spring-damper forces and anti-roll bars.

#### Per-Axle Parameters (SuspensionAxleData)

| Parameter | Default | Unit | Description |
|-----------|---------|------|-------------|
| `springRate` | 30,000 | N/m | Spring stiffness |
| `damperRate` | 4,500 | N·s/m | Damping coefficient |
| `restLength` | 0.45 | m | Uncompressed suspension length |
| `maxTravel` | 0.25 | m | Maximum compression distance |
| `antiRollBarK` | 15,000 | N/m | Anti-roll bar stiffness |

#### Wheel Configuration (WheelDef)

| Field | Type | Description |
|-------|------|-------------|
| `attach` | Transform | Wheel attachment point |
| `wheelRadius` | float | Wheel radius in meters |
| `isFront` | bool | Front axle flag |
| `isLeft` | bool | Left side flag |
| `isPowered` | bool | Receives drive torque |

#### Output: WheelState

| Field | Type | Description |
|-------|------|-------------|
| `position` | Vector3 | Contact point world position |
| `loadN` | float | Normal force (Newtons) |
| `Vx` | float | Forward velocity (m/s) |
| `Vy` | float | Lateral velocity (m/s) |
| `omega` | float | Angular velocity (rad/s) |
| `grounded` | bool | Wheel touching ground |

---

### 2.2 WheelDynamics

**Location:** `Scripts/VehiclePhysics/Core/WheelDynamics.cs`

**Purpose:** Integrates per-wheel angular velocity from drive/brake torques and tire reactions.

#### Parameters

| Parameter | Default | Unit | Description |
|-----------|---------|------|-------------|
| `wheelInertia` | 0.9 | kg·m² | Rotational inertia per wheel |
| `maxAngularAccel` | 500 | rad/s² | Max angular acceleration clamp |
| `maxOmega` | 200 | rad/s | Max angular velocity (~1900 RPM) |
| `handbrakeInstantLockup` | 0.7 | - | Omega reduction factor (0-1) |
| `handbrakeThreshold` | 0.5 | - | Handbrake input to trigger lockup |

#### Wheel Angular Velocity Update

```
T_net = T_drive + T_tire - T_brake
α = T_net / I
ω_new = ω + α × Δt
ω_new = clamp(ω_new, -maxOmega, maxOmega)
```

#### Handbrake Lockup (Rear Wheels Only)

When `handbrakeInput ≥ threshold`:
```
ω = ω × (1 - handbrakeInstantLockup)
if |ω| < 1: ω = 0
```

---

## 3. Tire & Friction Systems

### 3.1 SlipCalculator

**Location:** `Scripts/VehiclePhysics/Tires/SlipCalculator.cs`

**Purpose:** Computes slip ratio and slip angle per wheel.

#### Parameters

| Parameter | Default | Unit | Description |
|-----------|---------|------|-------------|
| `sEpsilon` | 0.15 | m/s | Min speed for slip ratio denominator |
| `aEpsilon` | 2.0 | m/s | Min speed added for slip angle |

#### Slip Ratio Formula

```
s = (R × ω - Vx) / max(|Vx|, ε)
```

| Slip Ratio | Meaning |
|------------|---------|
| s = 0 | No slip (wheel matches ground) |
| s > 0 | Wheel spinning faster (acceleration wheelspin) |
| s < 0 | Wheel spinning slower (brake lockup) |
| s = -1 | Fully locked wheel |
| s > 0.3 | Significant wheelspin |

#### Slip Angle Formula

```
α = atan2(Vy, |Vx| + ε_a)
```

Slip angle represents how much the tire is sliding sideways relative to its heading.

---

### 3.2 SimpleTireModel

**Location:** `Scripts/VehiclePhysics/Tires/SimpleTireModel.cs`

**Purpose:** Evaluates friction coefficients with combined slip support for drifting.

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `globalMuScale` | 1.0 | Global friction multiplier |
| `enableCombinedSlip` | true | Enable friction circle behavior |
| `combinedSlipStrength` | 0.85 | Effect strength (0-1) |
| `combinedSlipThreshold` | 0.35 | Slip ratio below which no lateral reduction |
| `lockupSlipRatio` | 0.9 | Slip ratio for full lateral reduction |
| `lockedWheelLateralGrip` | 0.25 | Min lateral grip when locked (25%) |

#### Load Sensitivity

```
μ_scaled = μ_base × (1 - sensitivity × (load/load_ref - 1))
μ_final = clamp(μ_scaled, minMu, maxMu)
```

#### Combined Slip (Friction Circle)

When `|slip ratio| > threshold`:
```
lockupFactor = (|s| - threshold) / (lockupSlipRatio - threshold)
lateralReduction = lerp(1, lockedWheelLateralGrip, lockupFactor × strength)
μy = μy × lateralReduction
```

This is the key mechanism enabling handbrake drifts - locked rear wheels lose lateral grip.

---

### 3.3 TireCurveData (ScriptableObject)

**Location:** `Scripts/VehiclePhysics/Tires/TireCurveData.cs`

**Purpose:** Defines friction curves via AnimationCurves.

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `loadSensitivity` | 0.1 | How much μ decreases with higher load |
| `referenceLoad` | 2500 N | Reference load for scaling |
| `minMu` | 0.2 | Minimum friction coefficient |
| `maxMu` | 1.3 | Maximum friction coefficient |

#### Default Curve Characteristics

| Curve | Peak μ | Peak Slip | Plateau μ |
|-------|--------|-----------|-----------|
| μx Front | 1.10 | 12% slip ratio | 0.90 |
| μx Rear | 1.25 | 13% slip ratio | 1.00 |
| μy Front | 1.10 | 12° slip angle | 0.92 |
| μy Rear | 1.15 | 12° slip angle | 1.00 |

---

### 3.4 TireForcesApplier

**Location:** `Scripts/VehiclePhysics/Tires/TireForcesApplier.cs`

**Purpose:** Computes and applies tire forces using friction ellipse clamping.

#### Key Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `alphaDeadzoneDeg` | 1.5° | Slip angle deadzone |
| `lowSpeedLateralK` | 600 | Low-speed lateral damping |
| `lateralZeroSpeed` | 0.08 m/s | Suppress lateral force below this |
| `holdAtRest` | true | Boost damping near zero speed |
| `holdSpeed` | 0.05 m/s | Speed threshold for hold mode |
| `holdBoost` | 4.0 | Damping multiplier at rest |
| `enableFrontFxReserve` | true | Reduce front Fx during cornering |
| `frontFxReserveStrength` | 0.8 | Reserve strength (0-1) |

#### Friction Ellipse Clamping

```
u = sqrt((Fx/Fx_lim)² + (Fy/Fy_lim)²)
if u > 1:
    Fx = Fx / u
    Fy = Fy / u
```

This ensures total tire force doesn't exceed available grip.

---

## 4. Powertrain System

### 4.1 PowertrainSystem

**Location:** `Scripts/VehiclePhysics/Powertrain/PowertrainSystem.cs`

**Purpose:** Engine RPM, torque calculation, and wheel force distribution.

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `throttle` | 0-1 | Throttle input |
| `currentGear` | 1 | Current gear (1-based) |
| `enableTractionAssist` | true | Limit drive force to available grip |
| `tractionAssistStrength` | 1.0 | Assist blend factor |
| `engineBrakeSpeedEpsilon` | 0.3 m/s | Engine brake smoothing |
| `wheelRadius` | 0.34 m | Fallback wheel radius |

#### Engine RPM Calculation

```
RPM = (Vx / (2π × R)) × gear_ratio × final_drive × 60
RPM = max(idle_RPM, RPM)
```

#### Wheel Torque Distribution (Open Differential)

```
T_wheel = (T_engine - T_engineBrake × motionFactor) × ratio × efficiency
F_x = T_wheel / (n_powered × R)
```

---

### 4.2 EngineTorqueCurve (ScriptableObject)

**Location:** `Scripts/VehiclePhysics/Powertrain/EngineTorqueCurve.cs`

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `idleRpm` | 900 | Minimum engine RPM |
| `maxRpm` | 7000 | Maximum engine RPM |
| `globalTorqueScale` | 1.0 | Torque multiplier |
| `engineBrakeFraction` | 0.08 | Fraction of max for engine braking |
| `engineBrakeMinRpm` | 1000 | Min RPM for engine braking |

#### Preset Engines

- **BMW E30 M3 S14B23 Stock:** 192 hp @ 6750 RPM, 230 Nm @ 4750 RPM
- **BMW E30 M3 Evo2:** 220 hp @ 7000 RPM, 240 Nm @ 4750 RPM

---

### 4.3 GearboxData (ScriptableObject)

**Location:** `Scripts/VehiclePhysics/Powertrain/GearboxData.cs`

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `gearRatios` | [3.80, 2.20, 1.52, 1.22, 1.00] | Per-gear ratios |
| `finalDrive` | 3.73 | Differential ratio |
| `efficiency` | 0.92 | Drivetrain efficiency |

---

## 5. Brake System

**Location:** `Scripts/VehiclePhysics/Brakes/BrakeSystem.cs`

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `brakeInput` | 0-1 | Service brake input |
| `handbrakeInput` | 0-1 | Handbrake input |
| `maxBrakeTorque` | 3000 N·m | Max service brake torque |
| `frontBias` | 0.65 | Front brake bias (65% front) |
| `maxHandbrakeTorque` | 12000 N·m | Max handbrake torque |
| `handbrakeRearOnly` | true | Handbrake only on rear wheels |

#### Brake Distribution

```
T_front_total = T_max × input × frontBias
T_rear_total = T_max × input × (1 - frontBias)
T_per_wheel = T_share / n_wheels_on_axle
```

---

## 6. Controller Systems

### 6.1 VehicleInputController

**Location:** `Scripts/VehiclePhysics/Controller/VehicleInputController.cs`

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `throttleAxis` | "Vertical" | Input axis for throttle/brake |
| `handbrakeAxis` | "Jump" | Handbrake input (Space) |
| `enableProgressiveThrottle` | true | Smooth throttle response |
| `throttleExponent` | 1.6 | Non-linear response curve |
| `throttleRiseRate` | 2.0 | Max throttle increase rate |
| `throttleFallRate` | 6.0 | Max throttle decrease rate |
| `autoGear` | true | Automatic gear shifting |
| `upshiftRpm` | 6200 | RPM to upshift |
| `downshiftRpm` | 1800 | RPM to downshift |

---

### 6.2 SteeringInput

**Location:** `Scripts/VehiclePhysics/Controller/SteeringInput.cs`

#### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `maxSteerDeg` | 30° | Maximum steering angle |
| `speedRef` | 30 m/s | Speed for reduced steering |
| `minFactor` | 0.5 | Minimum steering at high speed |
| `steerSpeed` | 200 °/s | Steering input speed |
| `returnSpeed` | 300 °/s | Return-to-center speed |

#### Speed-Sensitive Steering

```
factor = 1 - (min(speed, speedRef) / speedRef) × (1 - minFactor)
steer_target = input × maxSteerDeg × factor
```

---

## 7. Physics Formulas

### Slip Ratio

$$s = \frac{R \cdot \omega - V_x}{\max(|V_x|, \epsilon)}$$

### Slip Angle

$$\alpha = \arctan\left(\frac{V_y}{|V_x| + \epsilon_a}\right)$$

### Spring-Damper Force

$$F_{suspension} = k \cdot compression + c \cdot \dot{compression}$$

### Friction Ellipse

$$\sqrt{\left(\frac{F_x}{F_{x,max}}\right)^2 + \left(\frac{F_y}{F_{y,max}}\right)^2} \leq 1$$

### Wheel Angular Acceleration

$$\alpha = \frac{T_{drive} + T_{tire} - T_{brake}}{I}$$

### Engine RPM

$$RPM = \frac{V_x}{2\pi R} \cdot ratio \cdot 60$$

---

## 8. Drifting Mechanics

The system is specifically designed to support realistic drifting through three key mechanisms:

### 8.1 Handbrake Instant Lockup

**Component:** WheelDynamics

When handbrake is pulled (≥50%), rear wheels rapidly lose angular velocity:
```
ω_new = ω × 0.3  (70% reduction per frame)
```

This instantly creates a high negative slip ratio on rear wheels.

### 8.2 Combined Slip (Friction Circle)

**Component:** SimpleTireModel

When rear wheels are locked (high slip ratio), their lateral grip is dramatically reduced:

| Slip Ratio | Lateral Grip |
|------------|--------------|
| 0 - 0.35 | 100% (no reduction) |
| 0.35 - 0.9 | Linear reduction |
| ≥ 0.9 | 25% (minimum) |

This allows the rear to slide out while front wheels maintain grip.

### 8.3 Front Fx Reserve

**Component:** TireForcesApplier

During cornering, front longitudinal force is reduced to preserve lateral grip:
```
reserve = slip_angle / 10°
Fx_front = Fx_front × (1 - 0.8 × reserve)
```

This maintains steering authority during drift.

### Drift Initiation Sequence

1. **Entry:** Pull handbrake while turning
2. **Rear locks:** ω drops to near zero, slip ratio goes negative
3. **Grip loss:** Combined slip reduces rear lateral grip to 25%
4. **Oversteer:** Rear slides out, car rotates
5. **Maintenance:** Modulate throttle and countersteer
6. **Exit:** Release handbrake, rear wheels spin up, grip returns

---

## 9. Tuning Guide

### For More Grip (Stable Handling)

| Parameter | Location | Adjust |
|-----------|----------|--------|
| `maxMu` | TireCurveData | Increase (1.3 → 1.5) |
| `combinedSlipThreshold` | SimpleTireModel | Increase (0.35 → 0.5) |
| `frontBias` | BrakeSystem | Increase (0.65 → 0.75) |
| `tractionAssistStrength` | PowertrainSystem | Increase to 1.0 |

### For Easier Drifting

| Parameter | Location | Adjust |
|-----------|----------|--------|
| `lockedWheelLateralGrip` | SimpleTireModel | Decrease (0.25 → 0.15) |
| `combinedSlipThreshold` | SimpleTireModel | Decrease (0.35 → 0.25) |
| `handbrakeInstantLockup` | WheelDynamics | Increase (0.7 → 0.85) |
| `maxHandbrakeTorque` | BrakeSystem | Increase (12000 → 15000) |

### For More Realistic Feel

| Parameter | Location | Adjust |
|-----------|----------|--------|
| `loadSensitivity` | TireCurveData | Increase (0.1 → 0.15) |
| `wheelInertia` | WheelDynamics | Increase (0.9 → 1.2) |
| `sEpsilon` | SlipCalculator | Decrease (0.15 → 0.1) |
| `enableProgressiveThrottle` | VehicleInputController | Enable |

### For Arcade-Style Physics

| Parameter | Location | Adjust |
|-----------|----------|--------|
| `maxMu` | TireCurveData | Increase (1.3 → 2.0) |
| `holdAtRest` | TireForcesApplier | Enable |
| `speedRef` | SteeringInput | Increase (30 → 50) |
| `minFactor` | SteeringInput | Increase (0.5 → 0.8) |

---

## Component Reference Quick List

| Component | File Location |
|-----------|---------------|
| SuspensionSystem | `Suspension/SuspensionSystem.cs` |
| WheelDynamics | `Core/WheelDynamics.cs` |
| SlipCalculator | `Tires/SlipCalculator.cs` |
| SimpleTireModel | `Tires/SimpleTireModel.cs` |
| TireCurveData | `Tires/TireCurveData.cs` |
| TireForcesApplier | `Tires/TireForcesApplier.cs` |
| PowertrainSystem | `Powertrain/PowertrainSystem.cs` |
| EngineTorqueCurve | `Powertrain/EngineTorqueCurve.cs` |
| GearboxData | `Powertrain/GearboxData.cs` |
| BrakeSystem | `Brakes/BrakeSystem.cs` |
| VehicleInputController | `Controller/VehicleInputController.cs` |
| SteeringInput | `Controller/SteeringInput.cs` |
| VehicleTelemetry | `Debug/VehicleTelemetry.cs` |

---

*Last Updated: December 2024*
