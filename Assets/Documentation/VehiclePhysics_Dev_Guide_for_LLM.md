# Vehicle Physics Dev Guide for LLM

Purpose: Provide clear, structured instructions an LLM can follow to implement a realistic, drift-capable vehicle physics system in Unity. This guide defines modules, interfaces, constraints, and step-by-step tasks with acceptance criteria.

---

## Global Constraints
- Respect Unity FixedUpdate order and use `Rigidbody.AddForceAtPosition` per wheel.
- No hard speed cutoff; top speed emerges from drag and power.
- Tire forces are the ONLY mechanism that accelerates/turns/brakes the car.
- Use per-wheel normal force N to limit friction (μ·N), combine longitudinal/lateral via friction ellipse.
- Keep modules isolated; data-driven via ScriptableObjects.
- Preserve current input axes: `Horizontal` (steer), `Vertical` (throttle/brake).

---

## Modules & Responsibilities
1) Suspension
- Compute per-wheel normal force `N_i` via spring-damper + optional anti-roll.
- Provide `WheelState` with `N_i`, local velocities `Vx_i`, `Vy_i`, steering angle, and transforms.

2) TireModel
- Compute slip ratio `s_i` and slip angle `α_i`.
- Evaluate μ curves: `μx_i = f(s_i, load, isFront)`, `μy_i = f(α_i, load, isFront)`.
- Combine requested `(Fx_req_i, Fy_req_i)` with limits `(μx_i N_i, μy_i N_i)` using ellipse.

3) Powertrain
- Map throttle + RPM to engine torque. Apply gears + final to get wheel torque.
- Add engine braking torque at low/zero throttle.
- Output requested longitudinal force per powered wheel: `Fx_req_i = (Tw_i - Tbrake_i)/Rw`.

4) Brakes
- Compute per-wheel brake torque based on input and bias.
- Optional ABS: modulate torque if slip exceeds threshold.

5) Aero & Resistive
- Rolling resistance per wheel: `F_rr_i = Crr * N_i` along `-wheelForward`.
- Drag on body: `F_d = 0.5 * rho * CdA * v^2` opposite velocity.

6) Controller & Telemetry
- Map inputs to steer/throttle/brake; provide small speed assist only.
- Telemetry overlay: per-wheel N, s, α, Fx/Fy, ellipse utilization.

---

## Data Structures
```csharp
struct WheelState {
  Vector3 pos;
  Vector3 forward;
  Vector3 right;
  float   N;       // normal force
  float   Vx;      // local forward velocity
  float   Vy;      // local lateral velocity
  float   steerDeg;
  bool    isFront;
  bool    isPowered;
}

struct TireForces {
  float Fx; // along wheel forward
  float Fy; // along wheel right
}

interface ITireModel {
  float MuX(float slipRatio, float load, bool isFront);
  float MuY(float slipAngleDeg, float load, bool isFront);
}
```

---

## Curves & Parameters (ScriptableObjects)
- EngineTorqueCurve: `float Torque(float rpm, float throttle)`.
- Gearbox: `{float[] gearRatios; float finalDrive; float efficiency;}`.
- Tires: `AnimationCurve muXFront, muXRear, muYFront, muYRear`.
- Suspension: `{springK, damperC, maxTravel, antiRollStiffness}` per axle.
- Aero: `{rho, CdA, Crr}`.

---

## Step-by-Step Tasks (for the LLM)

### Task 1: Wheel State & Suspension
- Implement raycast suspension per wheel.
- Compute compression, spring-damper forces, and `N_i`.
- Compute local velocities `Vx, Vy` via `GetPointVelocity` and dot products.
- Populate `WheelState[]`.
- Acceptance: Non-zero `N_i` when grounded; gizmos show springs and wheel positions.

### Task 2: Slips & Tire Curves
- Implement slip ratio: `s = (Rw*omega - Vx) / max(|Vx|, s_eps)`.
- Implement slip angle: `alpha = atan2(Vy, |Vx| + a_eps)`.
- Evaluate μ curves per axle; add small load-sensitivity.
- Acceptance: Per-wheel s and α in telemetry; μ values vary realistically.

### Task 3: Friction Ellipse
- Compute `Fx_lim = μx*N`, `Fy_lim = μy*N`.
- Given requested `(Fx_req, Fy_req)`, project onto ellipse if outside.
- Acceptance: Utilization `u = sqrt((Fx/Fx_lim)^2 + (Fy/Fy_lim)^2)` ≤ 1.

### Task 4: Powertrain & Brakes
- RPM from `Vx` and gear ratio: `rpm = (Vx/Rw)*gear*final*60/(2π)`.
- Engine torque from torque curve, apply gear, final, efficiency.
- Engine braking torque proportional to rpm when throttle ≈ 0.
- Brake torque with bias per wheel; optional ABS if slip exceeds threshold.
- Convert torques to `Fx_req` and feed TireModel.
- Acceptance: Car accelerates/brakes realistically; wheel slip peaks near μ.

### Task 5: Steering & Controller
- Apply steering angle to front wheels; mild speed assist.
- Ensure steering works at low speed via epsilons in slip angle.
- Acceptance: Car responds to steering while coasting.

### Task 6: Resistive Forces
- Rolling resistance per wheel from `N_i`.
- Aerodynamic drag from body velocity.
- Acceptance: Natural top speed; coasting decel feels realistic.

### Task 7: Integration & Order
- In `FixedUpdate`: Inputs → Suspension/States → Slips → Powertrain/Brakes → Tire forces (ellipse) → Drag → `AddForceAtPosition`.
- Acceptance: Stable drift-capable handling, predictable thresholds.

### Task 8: Telemetry & Gizmos
- Draw Fx (blue), Fy (green) arrows per wheel.
- HUD: `N`, `s`, `α`, `μx`, `μy`, utilization.
- Acceptance: Tuning is straightforward via visible metrics.

---

## Acceptance Criteria (Global)
- No artificial forward forces on unpowered wheels.
- Top speed dictated by power vs drag; no hard cutoff.
- Steering effective at low speed; epsilons applied correctly.
- Longitudinal/lateral forces always bounded by μ·N and ellipse.
- Distinct front/rear behavior via separate μ curves.

---

## Common Pitfalls (avoid)
- Using vehicle speed to scale tire grip directly (use slip).
- Ignoring N when clamping forces (must be μ·N per wheel).
- Applying global max grip instead of per-wheel ellipse.
- Large thresholds that disable slip calculation at low speed.
- Pushing unpowered wheels to “carry momentum”.

---

## Implementation Notes
- Start simple (no wheel inertia): derive `omega` from driven torque and clamp by traction; upgrade later to physical wheel inertia.
- Use small eps: `s_eps = 0.5`, `a_eps = 0.5` (m/s) to keep steering at low speed.
- Tune μ curves first; then springs/dampers; then powertrain.

---

## Deliverables Checklist
- `WheelState` generator with suspension normal forces.
- `TireModel` with slip/μ curves and ellipse combining.
- `Powertrain` mapping to requested `Fx_req` + engine braking.
- `BrakeSystem` with bias and optional ABS.
- `AeroResist` applying rolling and drag.
- `VehicleController` orchestrating `FixedUpdate` order.
- Telemetry + Gizmos.

---

Ready for scaffolding? Generate the folders and class stubs under `Assets/Scripts/VehiclePhysics/` and wire to this plan.