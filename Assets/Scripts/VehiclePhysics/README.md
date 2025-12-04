Vehicle Physics: Task 1 (Suspension & Wheel States)

Setup (Editor):
1. Ensure your car GameObject has a `Rigidbody` (set mass and center of mass as needed).
2. Create axle data assets:
   - Right-click in Project window → Create → VehiclePhysics → Suspension Axle Data.
   - Make two assets: `FrontAxleData` and `RearAxleData` (tune springK/damperC/restLength/maxTravel/antiRollStiffness).
3. Add `SuspensionSystem` to the car root (same object or parent as `Rigidbody`).
4. Wheel attach transforms (CRITICAL):
   - Place each wheel's `attach` transform ABOVE the wheel center along chassis up.
   - At your desired static ride height, the vertical distance from `attach` → ground minus `wheelRadius` should equal (restLength - sag).
   - If you place `attach` at the wheel center, compression will start at `restLength` (too high) and car will float. Move it up.
5. In `SuspensionSystem` inspector per wheel:
   - Set `isFront` / `isLeft` flags (FL: front+left, FR: front+right, RL: rear+left, RR: rear+right).
   - Set `isPowered` if the wheel is driven.
   - Set `wheelRadius` (meters, matches your mesh/tyre model).
   - Normally leave `overrideLengths` off so axle asset values are used.
6. Assign axle data assets to `frontAxle` and `rearAxle`. Set `groundMask` to ground layers.
7. Enable `drawGizmos` for visualization (select object in Scene view).

Static geometry & numbers:
- Let: mass=1000 kg, g=9.81 → total weight ≈ 9810 N.
- Example front/rear split 53/47 → per wheel loads ≈ 2600 N front, 2300 N rear.
- If chosen `springK_front=36000`, static sag front ≈ 2600 / 36000 ≈ 0.072 m.
- If `springK_rear=33000`, static sag rear ≈ 2300 / 33000 ≈ 0.070 m.
- Pick `maxTravel ≈ sag + bumpReserve` (bumpReserve 0.05–0.08 m) → 0.12–0.15 m.
- Compute `restLength = length_static + sag`, where `length_static = (attach→ground) - wheelRadius` at desired ride height.

Useful debug:
- In Play mode select the car: check each wheel's `N` label. Large `N` (>> expected load) means attach too low or spring too stiff.
- Expected per-wheel normal force at rest ≈ wheel load (2600 N front, 2300 N rear). Adjust `springK` or `restLength` so compression = load / springK.

Common floating cause:
- Attach placed at wheel center → computed `length` = hit.distance - wheelRadius ≈ 0 → compression = restLength (full) → large spring force → car lifts.
- Fix: Raise the attach transform upward so `length_static` matches `restLength - sag`.

Notes:
- Suspension applies vertical force via `Rigidbody.AddForceAtPosition` only when grounded.
- Anti-roll couples L/R wheels per axle using `antiRollStiffness` (sign decides load transfer).
- Wheel local frame uses chassis up for steer axis and the wheel attach's forward/right.

Next Steps:
- After validating realistic `N`, `Vx`, `Vy` during motion, proceed to Task 2 (slip ratio/angle and μ curves).

Task 2 (Slips & Tire Curves) Setup:
- Create a `TireCurveData` asset (Right-click → Create → VehiclePhysics → Tire Curve Data). Adjust μ curves (x: slip ratio up to ~0.15; y: slip angle deg up to ~12). Peak near 0.1–0.15 slip ratio & 8–12° slip angle.
- Add `SimpleTireModel` to the car root and assign the `TireCurveData` asset.
- Add `SlipCalculator` component (assign `SuspensionSystem` + `SimpleTireModel`).
- Play: Observe HUD overlay values: `s` (slip ratio), `α` (slip angle deg), `μx`, `μy`.
- Adjust `sEpsilon` and `aEpsilon` if low-speed steering or slip stability needs tuning (larger eps = more stable numbers at crawl).
- Expected now: slip ratio ≈ 0 while coasting (no torque yet), slip angle responds to steering input. μ values rise from 0 toward curve peaks as slip increases.

Steering (for testing α):
- Add `SteeringInput` to the car root. Assign `SuspensionSystem` and `Rigidbody`.
- Parameters: `maxSteerDeg` (e.g., 30), `speedRef` (~25 m/s), `minFactor` (~0.35), `steerLerp` (~0.35).
- Use `Horizontal` input axis (A/D or arrows) to steer front wheels. Slip angle in the telemetry should change immediately, even at low speed (thanks to `aEpsilon`).

Debug Nudge (temporary motion):
- Add `DebugNudge` to the car root to generate gentle motion for telemetry.
Task 4 (Powertrain & Brakes) Setup:
- Create `EngineTorqueCurve` asset (Right-click → Create → VehiclePhysics → Engine Torque Curve). Adjust full-throttle curve (RPM vs torque), set idle/redline.
- Create `GearboxData` asset (Right-click → Create → VehiclePhysics → Gearbox Data). Set gear ratios, final drive, efficiency.
- Add `PowertrainSystem` to car root. Assign `SuspensionSystem`, `EngineTorqueCurve`, `GearboxData`, set starting `currentGear`.
- Add `BrakeSystem` to car root. Assign `SuspensionSystem` and `PowertrainSystem`; set `maxBrakeTorque`, `frontBias`.
- Add `VehicleInputController` (maps `Vertical` axis: positive = throttle, negative = brake). Enable `autoGear` for simple RPM-based shifting.
- In `TireForcesApplier`, ensure references to `PowertrainSystem` and `BrakeSystem` present; it will consume `wheelFxRequested` for longitudinal force and still use lateral stabilizer until real lateral force model.
- Play: Telemetry should show acceleration under positive Vertical input; brake reduces forward motion under negative input. Utilization `u` should rise near peak μ during strong accel/brake while steering.
- Tune sequence: Torque curve → gear ratios → brake torque → μ curves (to avoid constant saturation).

Notes:
- Engine braking torque applied automatically near zero throttle (fraction of full curve).
- Current implementation ignores wheel rotational inertia; wheel slip ratio will evolve once torque exceeds traction limits (added when real angular dynamics introduced).
- Lateral force still provisional (stabilizer). Will be replaced when slip angle drives Fy_req directly.
 - Hold `LeftShift` (default) to apply a small forward force. Change `nudgeKey` as needed.
 - `forwardReference`: assign a Transform (e.g., chassis) if your car's forward is different; otherwise it uses `suspension.transform.forward`.
 - `useWheelPositions`: if on, force is distributed via `AddForceAtPosition` at each wheel contact; otherwise applies at center.
 - `forceN`: tune (e.g., 1200–3000 N) to get a slow roll.
 - Enable `showHud` to see a small status box (“NUDGE: ON”) and speed.