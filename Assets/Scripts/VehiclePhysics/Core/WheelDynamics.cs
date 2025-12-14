using UnityEngine;

namespace VehiclePhysics
{
    /// <summary>
    /// Integrates per-wheel angular speed (omega) from drive/brake torque and traction-limited tire reaction.
    /// Provides accurate slip ratio for SlipCalculator.
    /// Execution order should be after SuspensionSystem and PowertrainSystem.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class WheelDynamics : MonoBehaviour
    {
        public SuspensionSystem suspension;
        public PowertrainSystem powertrain;
        public BrakeSystem brakeSystem;
        public SlipCalculator slipCalc;

        [Header("Inertia")]
        [Tooltip("Approximate rotational inertia per wheel (kg·m²)")]
        public float wheelInertia = 0.9f;

        [Header("Stability")]
        [Tooltip("Clamp max angular acceleration (rad/s²) to avoid spikes")]
        public float maxAngularAccel = 500.0f;
        [Tooltip("Clamp max angular velocity (rad/s). ~200 rad/s ≈ 1900 RPM for 0.3m wheel, ~120 km/h")]
        public float maxOmega = 200f;

        [Header("Handbrake Lockup")]
        [Tooltip("When handbrake is applied, instantly reduce wheel omega by this factor for quick lockup.")]
        [Range(0f, 1f)] public float handbrakeInstantLockup = 0.7f;
        [Tooltip("Handbrake input threshold to trigger instant lockup.")]
        public float handbrakeThreshold = 0.5f;

        void Reset()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakeSystem == null) brakeSystem = GetComponentInParent<BrakeSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
        }

        void Awake()
        {
            if (suspension == null) suspension = GetComponentInParent<SuspensionSystem>();
            if (powertrain == null) powertrain = GetComponentInParent<PowertrainSystem>();
            if (brakeSystem == null) brakeSystem = GetComponentInParent<BrakeSystem>();
            if (slipCalc == null) slipCalc = GetComponentInParent<SlipCalculator>();
        }

        void FixedUpdate()
        {
            if (suspension == null || suspension.wheels == null || suspension.CurrentWheelStates == null) return;
            var states = suspension.CurrentWheelStates;
            int n = states.Length;
            float dt = Time.fixedDeltaTime;
            if (n == 0) return;

            // Check if vehicle is nearly stationary with no input
            Rigidbody rb = suspension.rb;
            float vehicleSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
            bool isStationary = vehicleSpeed < 0.5f;  // Increased threshold
            bool hasThrottleInput = powertrain != null && powertrain.throttle > 0.01f;
            bool hasBrakeInput = brakeSystem != null && (brakeSystem.brakeInput > 0.01f || brakeSystem.handbrakeInput > 0.01f);

            for (int i = 0; i < n; i++)
            {
                var w = suspension.wheels[i];
                var ws = states[i];
                float R = Mathf.Max(0.05f, w.wheelRadius);

                // If stationary with no input, zero omega and skip dynamics
                if (isStationary && !hasThrottleInput && !hasBrakeInput)
                {
                    ws.omega = 0f;
                    suspension.CurrentWheelStates[i] = ws;
                    w.state = ws;
                    suspension.wheels[i] = w;
                    continue;
                }

                // Handbrake instant lockup for rear wheels (enables drift initiation)
                bool isHandbrakeActive = brakeSystem != null && brakeSystem.handbrakeInput >= handbrakeThreshold;
                if (isHandbrakeActive && !w.isFront)
                {
                    // Rapidly reduce omega to simulate mechanical lockup
                    ws.omega *= (1f - handbrakeInstantLockup);
                    // If omega is very small, fully lock
                    if (Mathf.Abs(ws.omega) < 1f)
                    {
                        ws.omega = 0f;
                    }
                }

                // Drive and brake torques
                float T_drive = 0f;
                if (powertrain != null && powertrain.wheelFxRequested != null && powertrain.wheelFxRequested.Length == n && w.isPowered)
                {
                    // Use traction-assisted request if enabled in powertrain
                    float Fx_req = powertrain.wheelFxRequested[i];
                    T_drive = Fx_req * R;
                }
                
                // Brake torque: magnitude from brake system, direction opposes wheel rotation
                float T_brake_magnitude = 0f;
                if (brakeSystem != null && brakeSystem.wheelBrakeForce != null && brakeSystem.wheelBrakeForce.Length == n)
                {
                    T_brake_magnitude = Mathf.Abs(brakeSystem.wheelBrakeForce[i]) * R;
                }

                // Determine rotation direction for brake application
                // Use omega if spinning, otherwise use ground speed (Vx) to determine direction
                float rotationSign = 0f;
                if (Mathf.Abs(ws.omega) > 0.1f)
                {
                    rotationSign = Mathf.Sign(ws.omega);
                }
                else if (Mathf.Abs(ws.Vx) > 0.05f)
                {
                    // Wheel nearly stopped but car still moving - use ground speed
                    rotationSign = Mathf.Sign(ws.Vx / R);
                }
                // If both are near zero, rotationSign stays 0 and brake has no directional effect
                // (which is correct - car is stationary)

                // Tire reaction torque: this represents the road's resistance to wheel spin
                // When wheel is trying to spin faster than ground (acceleration), road pushes back
                // When wheel is trying to spin slower than ground (braking), road pushes forward
                float T_tire = 0f;
                if (slipCalc != null && slipCalc.muX != null && slipCalc.muX.Length == n)
                {
                    float N = ws.loadN;
                    
                    // Use actual μx from the tire model (already evaluated from the slip ratio curve)
                    // This ensures WheelDynamics and TireForcesApplier use consistent friction values
                    float muX = slipCalc.muX[i];
                    float Fx_max = muX * N;
                    
                    // Get slip ratio to determine direction of tire reaction
                    float slipRatio = (slipCalc.slipRatio != null && i < slipCalc.slipRatio.Length) 
                        ? slipCalc.slipRatio[i] : 0f;
                    
                    // Tire reaction torque = friction force * radius
                    // The tire model's μx curve already gives us the correct force magnitude for this slip ratio
                    // We just need to apply it in the correct direction:
                    // - Positive slip (wheel faster than ground) → negative reaction (slows wheel)
                    // - Negative slip (wheel slower than ground) → positive reaction (speeds up wheel)
                    float Fx_tire = -Mathf.Sign(slipRatio) * Fx_max;
                    T_tire = Fx_tire * R;
                }

                // Net torque on wheel: drive + tire reaction - brake
                // Drive accelerates, tire reaction depends on slip, brake opposes rotation
                float T_brake_signed = T_brake_magnitude * rotationSign;
                float T_net = T_drive + T_tire - T_brake_signed;
                float I = Mathf.Max(0.05f, wheelInertia);
                float alpha = Mathf.Clamp(T_net / I, -maxAngularAccel, maxAngularAccel);
                ws.omega += alpha * dt;

                // Clamp omega to prevent unrealistic wheel spin
                ws.omega = Mathf.Clamp(ws.omega, -maxOmega, maxOmega);

                // For non-powered wheels only, blend omega toward ground speed
                // Powered wheels should be able to spin freely based on drive torque
                if (!w.isPowered)
                {
                    float groundOmega = ws.Vx / R;
                    // Blend toward ground omega - wheel should spin with the road
                    float blendRate = 5f * dt; // How quickly unpowered wheels sync with ground
                    ws.omega = Mathf.Lerp(ws.omega, groundOmega, blendRate);
                }

                // Prevent omega drift at rest: if wheel and vehicle are nearly stationary, zero omega
                if (Mathf.Abs(ws.Vx) < 0.1f && Mathf.Abs(ws.omega) < 0.5f && Mathf.Abs(T_drive) < 1f && !hasThrottleInput)
                {
                    ws.omega = 0f;
                }

                // Write back into suspension state
                suspension.CurrentWheelStates[i] = ws;
                w.state = ws;
                suspension.wheels[i] = w;
            }
        }
    }
}
