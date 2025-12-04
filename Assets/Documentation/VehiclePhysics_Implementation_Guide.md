# Vehicle Physics Implementation Guide (Drift-Ready)

This guide describes a clean architecture and step-by-step plan to implement realistic, tunable vehicle physics suitable for drifting. It’s designed to be practical, modular, and friendly to iteration in Unity.

---

## Goals & Principles
- Cohesive model: engine → drivetrain → tires → chassis should be consistent.
- Tire-first forces: Tires produce forces; engine/brakes request them.
- Load-sensitive grip: Friction scales with normal force and combines longitudinal/lateral via a friction ellipse.
- Modular tuning: All curves and constants should be editable (ScriptableObjects).
- Deterministic order: Compute loads → kinematics → tire forces → sum to rigidbody.

---

## Architecture Overview
- Powertrain: engine torque curve, gearing, differential, engine braking.
- Tire: slip ratio `s` (longitudinal), slip angle `α` (lateral), μ curves, friction ellipse.
- Suspension: spring-damper per wheel, anti-roll coupling, normal force `N`.
- Aero: drag (and optional downforce), rolling resistance.
- Brakes: per-wheel torque with bias, ABS optional.
- Controller: input shaping, mode switching (assist, drift, etc.).
- Telemetry: per-wheel debug (N, s, α, Fx, Fy, utilization).

---

## Data Assets (ScriptableObjects)
Create SOs to decouple tuning from code:
- EngineTorqueCurve: 1D curve of torque vs RPM, plus throttle shaping.
- GearboxData: gear ratios, final drive, efficiency.
- TireCurves: front and rear μx(s) and μy(α) AnimationCurves.
- SuspensionData: spring rate, damping, max travel, anti-roll stiffness per axle.
- AeroData: CdA, rho, downforce (optional) and Crr (rolling resistance coeff).

---

## Implementation Steps

### 1) Suspension and Normal Forces
Compute per-wheel normal force `N_i` from spring-damper and optionally anti-roll.
- Spring force: $F_s = k (x_0 - x)$
- Damper force: $F_d = c (\dot x_0 - \dot x)$
- Normal: $N = \max(0, F_s + F_d)$ per wheel
- Anti-roll (per axle): couple left/right spring deflections to transfer load.

Pseudocode:
```csharp
for each wheel i:
  hit = RaycastDown();
  if (hit) {
    compression = clamp(travel);
    springF = k * compression;
    damperF = c * (compression - prevCompression)/dt;
    Ni = max(0, springF + damperF) +/- antiRollTerm;
  } else {
    Ni = 0; // in air
  }
```

### 2) Tire Kinematics (per wheel)
Compute local wheel-frame velocities using `GetPointVelocity`:
```csharp
Vector3 v = rb.GetPointVelocity(wheelPos);
float Vx = Vector3.Dot(v, wheelForward);
float Vy = Vector3.Dot(v, wheelRight);
```
Track wheel angular speed `ω` if you simulate rotational inertia; otherwise you can infer via requested torque and clamp with traction (see Step 6/7).

### 3) Slips: Longitudinal `s` and Lateral `α`
Use small epsilons to avoid division by near zero.
- Longitudinal slip ratio:
  $s = \frac{R_w \omega - V_x}{\max(|V_x|, s_\epsilon)}$
- Lateral slip angle:
  $\alpha = \operatorname{atan2}(V_y, |V_x| + \alpha_\epsilon)$

Recommended: `s_epsilon = 0.5 m/s`, `alpha_epsilon = 0.5 m/s` initially.

### 4) Tire Curves (front/rear)
Define separate curves for longitudinal and lateral behavior:
- `μx_front(s)`, `μx_rear(s)` — S-shaped: near-linear at small `s`, peak at ~0.1–0.15, decay to plateau.
- `μy_front(α)`, `μy_rear(α)` — non-linear: flat near zero, sharp break around 8–12°, drift plateau.

Load sensitivity (optional but helpful):
- $\mu_{eff} = \mu_0 \cdot (1 - k_\text{load} \cdot (\frac{N}{N_{ref}} - 1))$
Typical `k_load`: 0.05–0.15; `N_ref`: static load per wheel.

### 5) Friction Ellipse (Combine Fx/Fy)
Compute requested longitudinal/lateral forces, then clamp to ellipse:
- Limits: $F_{x,lim} = \mu_x N$, $F_{y,lim} = \mu_y N$
- Ellipse check: $\left(\frac{F_x}{F_{x,lim}}\right)^2 + \left(\frac{F_y}{F_{y,lim}}\right)^2 \le 1$
- If > 1, scale `(Fx, Fy)` by the norm to sit on the boundary.
This unifies braking/accel/cornering and stabilizes drift behavior.

### 6) Powertrain → Requested Fx
- RPM mapping: $RPM = \frac{V_x}{R_w} \cdot gear \cdot final \cdot \frac{60}{2\pi}$ (clamp to idle → redline)
- Engine torque: `Te = TorqueCurve(RPM) * throttle`
- Wheel torque: `Tw = Te * gear * final * efficiency - T_engineBrake`
- Requested longitudinal force: $F_{x,req} = \frac{T_w - T_{brake}}{R_w}$ per powered wheel

Note: do not apply this directly; pass to Step 5 to clamp with μx·N and ellipse.

### 7) Brakes (with bias, optional ABS)
- `T_brake = input * bias * T_max` per wheel (front/rear distributions).
- Under braking, slip ratio will go negative; clamp by μx·N. If `|s|` exceeds threshold, reduce `T_brake` (ABS) to keep wheels near peak μ.

### 8) Steering
- Wheel steer angle from input with optional speed assist (small).
- Ackermann (optional) for better low-speed behavior.
- Self-align: either rely on Fy and caster/trail, or add a mild aligning damper toward center.

### 9) Resistive Forces
- Rolling resistance per wheel: $F_{rr,i} = C_{rr} \cdot N_i$ along `-forward`.
- Aero drag: $F_d = \tfrac{1}{2}\rho C_d A v^2$ opposite velocity.
- Downforce (optional): $F_{df} = \tfrac{1}{2}\rho C_l A v^2$ distributed by axle.

### 10) Update Order (per FixedUpdate)
1. Read inputs → steering angle, throttle, brake.
2. Suspension raycasts → `N_i` per wheel.
3. Tire kinematics per wheel → `Vx_i, Vy_i` (and `ω_i` if modeled).
4. Compute slips `s_i`, `α_i`.
5. Powertrain → `F_x,req,i`; Brakes → `T_brake,i` → adjust `F_x,req,i`.
6. μ from curves → limits `Fx_lim`, `Fy_lim`.
7. Combine via ellipse → final `(Fx_i, Fy_i)`.
8. Add rolling + aero drag.
9. Apply `AddForceAtPosition` per wheel (Fx along wheel forward, Fy along wheel right).

---

## Migration From Current Code
- Remove artificial forward push on unpowered wheels (already done).
- Keep engine braking (small negative torque on powered wheels while coasting).
- Replace forward/lateral “grip multipliers” with μx/μy curves driven by slip, not speed.
- Replace global `maxGripMagnitude` clamp with per-wheel μ·N and friction ellipse.
- Compute rolling resistance from `N`, not a constant; reduce global drags.
- Lower steering low-speed threshold; use `alpha_epsilon` instead.

---

## Tuning Baselines
- Mass: 1000–1400 kg; CG height realistic (0.4–0.6 m).
- Tires:
  - Peak μx: 1.1–1.3 (street), 1.5–1.8 (sport), 2.0+ (slick).
  - Peak at `|s|` ≈ 0.1–0.15; drift plateau ~0.3–0.5.
  - Lateral break at 8–12°; drift plateau ~20–35°.
- Suspension: set spring rates to get 60–80 mm static compression; damping critically damped ±.
- Rolling resistance: `Crr` 0.008–0.015; distribute per wheel via `N`.
- Aero: CdA 0.5–0.8 m² road car; `ρ=1.225 kg/m³`.
- Engine braking: small torque that scales with RPM; start at 5–10% of peak torque.

---

## Minimal Code Interfaces (suggested)
```csharp
interface ITireModel {
  float MuX(float slipRatio, float load, bool front);
  float MuY(float slipAngleDeg, float load, bool front);
}

struct WheelState {
  Vector3 position;
  Vector3 forward;
  Vector3 right;
  float   loadN;   // Normal force
  float   Vx;      // Forward velocity in wheel frame
  float   Vy;      // Lateral velocity in wheel frame
  float   omega;   // Angular speed (optional)
  float   steerDeg;
}

struct PowertrainOutput {
  float FxRequested[4]; // per wheel requested longitudinal force before traction limits
}
```

---

## Telemetry & Gizmos
- Draw per-wheel force vectors (Fx in blue, Fy in green).
- Display HUD bars: `N`, `|s|`, `|α|`, utilization `u = sqrt((Fx/Fx_lim)^2+(Fy/Fy_lim)^2)`.
- Show current points on μ curves for quick tuning feedback.

---

## Optional Extensions
- Differential models: open, clutch LSD, Torsen (torque biasing).
- Assist systems: TCS, ABS, ESC (yaw moment control).
- Heat cycles: tire temperature model feeding μ curves.
- Ackermann steering geometry; caster/trail for aligning torque.

---

## Checklist
- [ ] Springs/dampers compute per-wheel `N`.
- [ ] Kinematics: `Vx`, `Vy` (and `ω` if used).
- [ ] Slips: `s`, `α` with epsilons.
- [ ] Curves: `μx(s)`, `μy(α)` front/rear.
- [ ] Friction ellipse scaling of `(Fx, Fy)`.
- [ ] Powertrain mapping to `Fx_req` (gears, torque, engine braking).
- [ ] Brakes per wheel with bias and optional ABS.
- [ ] Rolling and aero drag.
- [ ] Gizmos + HUD for tuning.

---

## Quick Formulas (for Notion)
- $s = \dfrac{R\,\omega - V_x}{\max(|V_x|, s_\epsilon)}$
- $\alpha = \operatorname{atan2}(V_y, |V_x| + \alpha_\epsilon)$
- $F_{x,lim} = \mu_x(s)\,N$, $F_{y,lim} = \mu_y(\alpha)\,N$
- $\left(\dfrac{F_x}{F_{x,lim}}\right)^2 + \left(\dfrac{F_y}{F_{y,lim}}\right)^2 \le 1$
- $RPM = \dfrac{V_x}{R}\,gear\,final\,\dfrac{60}{2\pi}$
- $F_d = \tfrac{1}{2}\rho C_d A v^2$, \; $F_{rr} = C_{rr}\,N$

---

If you want, I can scaffold stubs for these modules in `Assets/Scripts/VehiclePhysics/` and a sample set of curves as ScriptableObjects to get you started.
