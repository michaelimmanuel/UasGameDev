using UnityEngine;

public class Demo_script : MonoBehaviour
{
    [SerializeField] private Transform[] tires = new Transform[4];
    [SerializeField] private float suspensionDistance = 0.5f;  // Raycast distance (always points down)
    [SerializeField] private LayerMask groundLayer;
    
    // Front suspension settings
    [SerializeField] private float frontSuspensionMaxTravel = 0.3f;
    [SerializeField] private float frontSuspensionStrength = 50000f;
    [SerializeField] private float frontSuspensionDampingCompression = 500f;
    [SerializeField] private float frontSuspensionDampingExtension = 300f;
    [SerializeField] private float frontMaxSuspensionForce = 5000f;
    
    // Rear suspension settings
    [SerializeField] private float rearSuspensionMaxTravel = 0.3f;
    [SerializeField] private float rearSuspensionStrength = 50000f;
    [SerializeField] private float rearSuspensionDampingCompression = 500f;
    [SerializeField] private float rearSuspensionDampingExtension = 300f;
    [SerializeField] private float rearMaxSuspensionForce = 5000f;
    [SerializeField] private float maxSteeringAngle = 45f;
    [SerializeField] private float steeringReturnTime = 0.2f;
    [SerializeField] private float steeringSpeedSensitivity = 1.5f;  // Steering gets heavier at speed
    [SerializeField] private float steeringLoadSensitivity = 0.5f;   // How much tire load affects steering
    [SerializeField] private float engineForce = 10000f;
    [SerializeField] private float engineBraking = 1500f;  // Passive decel torque when coasting
    [SerializeField] private float maxSpeed = 50f;
    [SerializeField] private float brakeForce = 20000f;
    [SerializeField] private float brakeBiasFront = 0.6f;  // 60% front, 40% rear
    [SerializeField] private AnimationCurve brakeForceCurve = new AnimationCurve(
        new Keyframe(0f, 0f),        // No braking when stopped
        new Keyframe(0.1f, 0.5f),    // 50% force at 10% speed
        new Keyframe(0.3f, 1f),      // Full 100% force at 30% speed and above
        new Keyframe(1f, 1f)         // Full force at max speed
    );
    [SerializeField] private AnimationCurve enginePowerCurve = new AnimationCurve(
        new Keyframe(0f, 1f),        // 0% speed: 100% power (full torque from start)
        new Keyframe(0.3f, 1f),      // 30% speed: still 100% power
        new Keyframe(0.6f, 0.8f),    // 60% speed: 80% power (power starts dropping)
        new Keyframe(0.9f, 0.5f),    // 90% speed: 50% power (falling off)
        new Keyframe(1f, 0.2f)       // 100% speed: 20% power (very little at max speed)
    );
    [SerializeField] private float tireGripLateral = 2000f;
    [SerializeField] private float tireGripForward = 2000f;
    [SerializeField] private float frontTireGripLateral = 1800f;  // Front: slightly less lateral grip
    [SerializeField] private float frontTireGripForward = 1800f;  // Front: slightly less forward grip
    [SerializeField] private float rearTireGripLateral = 2200f;   // Rear: slightly more lateral grip
    [SerializeField] private float rearTireGripForward = 2200f;   // Rear: slightly more forward grip
    [SerializeField] private float maxGripMagnitude = 50000f;  // Max combined grip force (clamping)
    [SerializeField] private float slipAngleThreshold = 10f;   // Degrees - when tire starts to slip
    [SerializeField] private float maxSlipAngle = 45f;         // Degrees - full slide at this angle
    [SerializeField] private float lockupThreshold = 25f;      // Degrees - when tire temp starts building
    [SerializeField] private float lockupBuildRate = 0.3f;     // How quickly tire heats up (0-1 per second)
    [SerializeField] private float lockupRecoveryRate = 0.5f;  // How quickly tire cools down
    [SerializeField] private float maxTireTemperature = 1f;    // Max tire temp (100% heat)
    [SerializeField] private AnimationCurve tireTemperatureGripBoost = new AnimationCurve(
        new Keyframe(0f, 0.9f),     // Cold tire (0% temp): 90% grip
        new Keyframe(0.3f, 1f),     // Warming up (30% temp): 100% grip
        new Keyframe(0.6f, 1.05f),  // Hot tire (60% temp): 105% grip (peak)
        new Keyframe(0.9f, 1f),     // Very hot (90% temp): 100% grip
        new Keyframe(1f, 0.7f)      // Overheated (100% temp): 70% grip (locked up)
    );
    [SerializeField] private float angularDamping = 0.5f;      // Rotation damping (prevents spinning)
    [SerializeField] private float rollingResistance = 100f;   // Rolling friction from tires (always active)
    [SerializeField] private float aerodynamicDrag = 5f;       // Air resistance (speed^2 dependent)
    [SerializeField] private AnimationCurve tireGripLateralCurve = new AnimationCurve(
        new Keyframe(0f, 0.6f),      // At 0 speed: 60% grip (low grip when stationary)
        new Keyframe(0.2f, 1f),      // At 20% speed: peak 100% grip
        new Keyframe(0.5f, 0.95f),   // At 50% speed: still 95% grip
        new Keyframe(0.8f, 0.85f),   // At 80% speed: 85% grip (starts to decrease)
        new Keyframe(1f, 0.6f)       // At max speed: 60% grip (significant loss at high speed)
    );
    [SerializeField] private AnimationCurve frontTireGripLateralCurve = new AnimationCurve(
        new Keyframe(0f, 1f),        // Front: maximum grip at low speed
        new Keyframe(0.15f, 1f),     // Front: peaks earlier
        new Keyframe(0.5f, 0.98f),   // Front: good sustained grip
        new Keyframe(0.8f, 0.88f),   // Front: loses grip more at high speed
        new Keyframe(1f, 0.55f)      // Front: drops more at max speed (understeers)
    );
    [SerializeField] private AnimationCurve rearTireGripLateralCurve = new AnimationCurve(
        new Keyframe(0f, 1f),        // Rear: maximum grip at low speed
        new Keyframe(0.25f, 1f),     // Rear: peaks later
        new Keyframe(0.5f, 0.92f),   // Rear: lower sustained grip
        new Keyframe(0.8f, 0.80f),   // Rear: drops faster at speed
        new Keyframe(1f, 0.5f)       // Rear: less grip at max speed (easier to drift)
    );
    [SerializeField] private AnimationCurve tireSlipCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),                      // 0° slip: 100% grip (locked in, flat)
        new Keyframe(5f, 0.99f, 0f, 0f),                   // 5° slip: 99% grip (nearly locked, still flat)
        new Keyframe(8f, 0.95f, -0.1f, -0.1f),             // 8° slip: 95% grip (SHARP threshold starting)
        new Keyframe(12f, 0.65f, -0.15f, -0.15f),          // 12° slip: 65% grip (steep drop - breaking point!)
        new Keyframe(18f, 0.45f, -0.05f, -0.05f),          // 18° slip: 45% grip (drifting zone)
        new Keyframe(30f, 0.3f, -0.015f, -0.015f),         // 30° slip: 30% grip (deep drift)
        new Keyframe(60f, 0.15f, -0.005f, -0.005f),        // 60° slip: 15% grip (heavy slide)
        new Keyframe(90f, 0.1f, 0f, 0f)                    // 90° slip: 10% grip (spinning, flat)
    );
    [SerializeField] private AnimationCurve frontTireSlipCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),                      // Front: same base
        new Keyframe(5f, 0.99f, 0f, 0f),
        new Keyframe(8f, 0.96f, -0.08f, -0.08f),           // Front: gradual drop (predictable understeer)
        new Keyframe(12f, 0.75f, -0.08f, -0.08f),          // Front: slower drop
        new Keyframe(18f, 0.6f, -0.04f, -0.04f),           // Front: maintains more grip (resists sliding)
        new Keyframe(30f, 0.45f, -0.01f, -0.01f),
        new Keyframe(60f, 0.3f, -0.003f, -0.003f),
        new Keyframe(90f, 0.2f, 0f, 0f)                    // Front: keeps more grip even at high slip
    );
    [SerializeField] private AnimationCurve rearTireSlipCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),                      // Rear: same base
        new Keyframe(5f, 0.98f, -0.02f, -0.02f),           // Rear: early gentle decline
        new Keyframe(8f, 0.92f, -0.15f, -0.15f),           // Rear: SHARP drop (sudden oversteer)
        new Keyframe(12f, 0.50f, -0.2f, -0.2f),            // Rear: steep cliff (dramatic slide)
        new Keyframe(18f, 0.28f, -0.08f, -0.08f),          // Rear: less grip in drift zone
        new Keyframe(30f, 0.15f, -0.02f, -0.02f),          // Rear: minimum grip quickly
        new Keyframe(60f, 0.08f, -0.001f, -0.001f),
        new Keyframe(90f, 0.05f, 0f, 0f)                   // Rear: minimal grip when spinning
    );
    [SerializeField] private AnimationCurve tireGripForwardCurve = new AnimationCurve(
        new Keyframe(0f, 1f),        // At 0 speed: maximum grip
        new Keyframe(0.15f, 1f),     // At 15% speed: peak 100% grip (faster peak than lateral)
        new Keyframe(0.5f, 0.98f),   // At 50% speed: still very grippy 98%
        new Keyframe(0.8f, 0.90f),   // At 80% speed: 90% grip
        new Keyframe(1f, 0.65f)      // At max speed: 65% grip
    );
    
    public enum DrivetrainType { FWD, RWD, AWD }
    [SerializeField] private DrivetrainType drivetrainType = DrivetrainType.AWD;
    
    private float[] suspensionForces = new float[4];
    private float[] previousCompression = new float[4];
    private float[] suspensionCompression = new float[4];
    private Vector3[] tireForces = new Vector3[4];
    private float[] tireLockupAmount = new float[4];  // 0-1: how locked up each tire is
    private Rigidbody rb;
    private float currentSteeringAngle = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        UpdateSteering();
        UpdateAcceleration();
        UpdateBraking();
        CastSuspensionRays();
        ApplySuspensionForces();
        ApplyTireForces();
        ApplyTireGrip();
        ApplyRotationDamping();
        ApplyDragForces();  // Apply rolling resistance and aerodynamic drag
    }

    void UpdateSteering()
    {
        float steeringInput = Input.GetAxis("Horizontal"); // A/D keys
        
        float steeringSpeed = maxSteeringAngle / steeringReturnTime;
        
        // Calculate steering resistance based on speed
        float currentSpeed = rb.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float speedResistance = Mathf.Lerp(0.3f, 1.5f, speedRatio);  // Heavier at high speed
        
        // Calculate steering resistance based on front tire load
        float frontLeftLoad = Mathf.Clamp01(suspensionCompression[0] / frontSuspensionMaxTravel);
        float frontRightLoad = Mathf.Clamp01(suspensionCompression[1] / frontSuspensionMaxTravel);
        float frontTireLoad = (frontLeftLoad + frontRightLoad) / 2f;
        float loadResistance = Mathf.Lerp(0.5f, 1.5f, frontTireLoad * steeringLoadSensitivity);  // Heavier when loaded
        
        // Combine resistances
        float totalResistance = speedResistance * loadResistance;
        float effectiveSteeringSpeed = steeringSpeed / totalResistance;
        
        if (steeringInput != 0)
        {
            // Update steering angle when input is pressed
            currentSteeringAngle += steeringInput * effectiveSteeringSpeed * Time.deltaTime;
            currentSteeringAngle = Mathf.Clamp(currentSteeringAngle, -maxSteeringAngle, maxSteeringAngle);
        }
        else
        {
            // Return to center when no input (faster return at high speed)
            float returnSpeed = steeringSpeed * speedResistance;
            currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, 0, returnSpeed * Time.deltaTime / maxSteeringAngle);
        }
        
        // Rotate front tires (tires[0] and tires[1])
        if (tires[0] != null)
            tires[0].localRotation = Quaternion.Euler(0, currentSteeringAngle, 0);
        if (tires[1] != null)
            tires[1].localRotation = Quaternion.Euler(0, currentSteeringAngle, 0);
    }

    void UpdateAcceleration()
    {
        float accelerationInput = Input.GetAxis("Vertical"); // W/S keys
        
        // Only accelerate forward, not reverse
        if (accelerationInput < 0)
            accelerationInput = 0;
        
        // Remove hard speed cut; rely on aero/rolling drag and power curve instead
        
        // Get drivetrain power distribution
        float[] powerDistribution = GetDrivetrainPowerDistribution();
        
        // Count how many wheels are powered
        float poweredWheelCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (powerDistribution[i] > 0)
                poweredWheelCount++;
        }
        
        // Get current car speed and calculate speed ratio
        float carSpeed = rb.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(carSpeed / maxSpeed);
        
        // Apply power curve for realistic acceleration
        float powerMultiplier = enginePowerCurve.Evaluate(speedRatio);
        
        // Scale engine force based on number of powered wheels to keep power consistent across drivetrains
        float scaledEngineForce = engineForce * (4f / poweredWheelCount);
        float engineForcePerWheel = (scaledEngineForce / 4f) * accelerationInput * powerMultiplier;
        
        // Reset tire forces
        for (int i = 0; i < 4; i++)
        {
            tireForces[i] = Vector3.zero;
        }
        
        Vector3 carForward = rb.transform.forward;
        
        // Step 1: Calculate base longitudinal force for each tire (drive/engine braking)
        float[] baseForce = new float[4];
        for (int i = 0; i < 4; i++)
        {
            // Default: no drive force on unpowered wheels
            baseForce[i] = 0f;

            if (powerDistribution[i] > 0)
            {
                if (accelerationInput > 0.001f)
                {
                    // Apply drive force proportionally to power distribution
                    baseForce[i] = engineForcePerWheel * powerDistribution[i];
                }
                else if (Mathf.Abs(accelerationInput) < 0.001f)
                {
                    // Coasting: apply passive engine braking on powered wheels
                    float poweredCount = Mathf.Max(1f, poweredWheelCount);
                    float engineBrakePerWheel = (engineBraking / poweredCount) * powerDistribution[i];
                    baseForce[i] = -engineBrakePerWheel;
                }
                // Note: explicit braking (S key) handled in UpdateBraking
            }
        }
        
        // Step 2: Apply steering to redirect forces
        // Front tires (0, 1) are affected by steering angle
        // Rear tires (2, 3) follow the car's natural direction
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] == null) continue;
            
            if (i < 2)
            {
                // Front tires - apply force in their steering direction
                tireForces[i] = tires[i].forward * baseForce[i];
            }
            else
            {
                // Rear tires - apply force in car's forward direction
                tireForces[i] = carForward * baseForce[i];
            }
        }
    }

    void UpdateBraking()
    {
        float accelerationInput = Input.GetAxis("Vertical"); // W/S keys
        
        // Only brake if S is pressed (negative input)
        if (accelerationInput >= 0)
            return;
        
        Vector3 carForward = rb.transform.forward;
        Vector3 carVelocity = rb.linearVelocity;
        
        // Get current speed and calculate brake force multiplier from curve
        float currentSpeed = carVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        float brakeForceCurveMultiplier = brakeForceCurve.Evaluate(speedRatio);
        
        // Calculate actual brake force with curve applied
        float actualBrakeForce = brakeForce * brakeForceCurveMultiplier;
        
        // Front brakes (tires 0 and 1) - get more braking force
        float frontBrakeForcePerWheel = (actualBrakeForce * brakeBiasFront) / 2f;
        
        // Rear brakes (tires 2 and 3) - get less braking force
        float rearBrakeForcePerWheel = (actualBrakeForce * (1f - brakeBiasFront)) / 2f;
        
        // Brake direction opposes current velocity direction for more realistic feel
        Vector3 brakeDirection = carVelocity.magnitude > 0.1f ? -carVelocity.normalized : -carForward;
        
        // Apply front brakes (stronger)
        for (int i = 0; i < 2; i++)
        {
            if (tires[i] == null) continue;
            tireForces[i] += brakeDirection * frontBrakeForcePerWheel;
        }
        
        // Apply rear brakes (weaker)
        for (int i = 2; i < 4; i++)
        {
            if (tires[i] == null) continue;
            tireForces[i] += brakeDirection * rearBrakeForcePerWheel;
        }
    }

    float[] GetDrivetrainPowerDistribution()
    {
        float[] distribution = new float[4];
        
        switch (drivetrainType)
        {
            case DrivetrainType.FWD:
                // Front-wheel drive
                distribution[0] = 1f;  // Front left
                distribution[1] = 1f;  // Front right
                distribution[2] = 0f;  // Rear left
                distribution[3] = 0f;  // Rear right
                break;
            
            case DrivetrainType.RWD:
                // Rear-wheel drive
                distribution[0] = 0f;  // Front left
                distribution[1] = 0f;  // Front right
                distribution[2] = 1f;  // Rear left
                distribution[3] = 1f;  // Rear right
                break;
            
            case DrivetrainType.AWD:
                // All-wheel drive
                distribution[0] = 1f;  // Front left
                distribution[1] = 1f;  // Front right
                distribution[2] = 1f;  // Rear left
                distribution[3] = 1f;  // Rear right
                break;
        }
        
        return distribution;
    }

    void CastSuspensionRays()
    {
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] == null) continue;
            
            // Determine if front (0,1) or rear (2,3) tire
            bool isFront = (i < 2);
            float maxTravel = isFront ? frontSuspensionMaxTravel : rearSuspensionMaxTravel;
            float strength = isFront ? frontSuspensionStrength : rearSuspensionStrength;
            float dampingComp = isFront ? frontSuspensionDampingCompression : rearSuspensionDampingCompression;
            float dampingExt = isFront ? frontSuspensionDampingExtension : rearSuspensionDampingExtension;
            float maxForce = isFront ? frontMaxSuspensionForce : rearMaxSuspensionForce;
            
            if (Physics.Raycast(tires[i].position, Vector3.down, out RaycastHit hit, suspensionDistance, groundLayer))
            {
                // Distance from tire to ground
                float distanceToGround = hit.distance;
                
                // Compression = how much suspension is compressed from its max travel point
                float compression = Mathf.Max(0, maxTravel - distanceToGround);
                float compressionVelocity = (compression - previousCompression[i]) / Time.deltaTime;
                previousCompression[i] = compression;
                
                // Store compression for weight transfer calculation
                suspensionCompression[i] = Mathf.Max(0, compression);
                
                float springForce = strength * Mathf.Max(0, compression);
                float dampingCoefficient = compressionVelocity > 0 ? dampingComp : dampingExt;
                float dampingForce = dampingCoefficient * compressionVelocity;
                
                suspensionForces[i] = Mathf.Clamp(springForce + dampingForce, 0, maxForce);
            }
            else
            {
                previousCompression[i] = 0f;
                suspensionForces[i] = 0f;
                suspensionCompression[i] = 0f;
            }
        }
    }

    void ApplySuspensionForces()
    {
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] != null)
            {
                rb.AddForceAtPosition(new Vector3(0, suspensionForces[i], 0), tires[i].position, ForceMode.Force);
            }
        }
    }

    void ApplyTireForces()
    {
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] != null)
            {
                rb.AddForceAtPosition(tireForces[i], tires[i].position, ForceMode.Force);
            }
        }
    }

    void ApplyTireGrip()
    {
        Vector3 carForward = rb.transform.forward;
        Vector3 carRight = rb.transform.right;
        
        // Get current speed ratio (0-1)
        float currentSpeed = rb.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeed);
        
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] == null) continue;
            
            bool isFront = (i < 2);
            
            // Select grip curves based on front/rear
            AnimationCurve gripLateralCurve = isFront ? frontTireGripLateralCurve : rearTireGripLateralCurve;
            AnimationCurve slipCurve = isFront ? frontTireSlipCurve : rearTireSlipCurve;
            float gripLateral = isFront ? frontTireGripLateral : rearTireGripLateral;
            float gripForward = isFront ? frontTireGripForward : rearTireGripForward;
            
            // Get grip multipliers from speed curve
            float lateralGripMultiplier = gripLateralCurve.Evaluate(speedRatio);
            float forwardGripMultiplier = tireGripForwardCurve.Evaluate(speedRatio);
            
            // Weight transfer
            float maxTravel = isFront ? frontSuspensionMaxTravel : rearSuspensionMaxTravel;
            float compressionMultiplier = Mathf.Clamp01(suspensionCompression[i] / maxTravel);
            
            // Get tire velocity and direction
            Vector3 tireVelocity = rb.GetPointVelocity(tires[i].position);
            Vector3 tireForwardDir = (i < 2) ? tires[i].forward : carForward;
            Vector3 tireRightDir = (i < 2) ? tires[i].right : carRight;
            
            // Calculate SIGNED slip angle
            float signedSlipAngleDegrees = 0f;
            float velocityMagnitude = tireVelocity.magnitude;
            
            if (velocityMagnitude > 0.1f)  // Allow steering even at very low speeds (0.1 m/s threshold)
            {
                float forwardComponent = Vector3.Dot(tireVelocity, tireForwardDir);
                float rightComponent = Vector3.Dot(tireVelocity, tireRightDir);
                signedSlipAngleDegrees = Mathf.Atan2(rightComponent, forwardComponent) * Mathf.Rad2Deg;
            }
            
            float absSlipAngleDegrees = Mathf.Abs(signedSlipAngleDegrees);
            
            // Update tire temperature
            if (absSlipAngleDegrees > lockupThreshold)
            {
                tireLockupAmount[i] += lockupBuildRate * Time.deltaTime;
            }
            else
            {
                tireLockupAmount[i] -= lockupRecoveryRate * Time.deltaTime;
            }
            tireLockupAmount[i] = Mathf.Clamp01(tireLockupAmount[i]);
            
            // Get slip multiplier from front/rear specific curve
            float slipGripMultiplier = slipCurve.Evaluate(absSlipAngleDegrees);
            
            // Apply tire temperature (sustains drift)
            float temperatureBoost = tireTemperatureGripBoost.Evaluate(tireLockupAmount[i]);
            slipGripMultiplier *= temperatureBoost;
            
            // Calculate lateral grip
            float lateralVelocity = Vector3.Dot(tireVelocity, tireRightDir);
            float lateralGripForce = -lateralVelocity * gripLateral * lateralGripMultiplier * compressionMultiplier * slipGripMultiplier;
            Vector3 lateralGripVector = tireRightDir * lateralGripForce;
            
            // Calculate forward grip
            float forwardVelocity = Vector3.Dot(tireVelocity, tireForwardDir);
            float forwardGripForce = -forwardVelocity * gripForward * forwardGripMultiplier * compressionMultiplier * slipGripMultiplier;
            Vector3 forwardGripVector = tireForwardDir * forwardGripForce;
            
            // Combine grip forces
            Vector3 combinedGripVector = lateralGripVector + forwardGripVector;
            
            // Clamp combined grip
            float gripMagnitude = combinedGripVector.magnitude;
            if (gripMagnitude > maxGripMagnitude)
            {
                combinedGripVector = combinedGripVector.normalized * maxGripMagnitude;
            }
            
            rb.AddForceAtPosition(combinedGripVector, tires[i].position, ForceMode.Force);
        }
    }

    void ApplyRotationDamping()
    {
        // Apply damping to angular velocity to prevent uncontrolled spinning
        Vector3 angularVelocity = rb.angularVelocity;
        
        // Reduce rotation around all axes
        rb.angularVelocity = angularVelocity * (1f - angularDamping * Time.deltaTime);
    }

    void ApplyDragForces()
    {
        Vector3 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        
        if (speed < 0.01f) return;  // No drag if barely moving
        
        Vector3 dragDirection = -velocity.normalized;
        
        // Rolling resistance - constant friction from tires
        float rollingDragForce = rollingResistance;
        
        // Aerodynamic drag - proportional to speed squared
        float aerodynamicDragForce = aerodynamicDrag * speed * speed;
        
        // Total drag force
        float totalDragForce = rollingDragForce + aerodynamicDragForce;
        
        // Apply drag force opposite to velocity
        rb.AddForce(dragDirection * totalDragForce, ForceMode.Force);
    }

    void OnDrawGizmos()
    {
        // Draw tire positions and suspension rays
        for (int i = 0; i < 4; i++)
        {
            if (tires[i] == null) continue;
            
            Vector3 tirePos = tires[i].position;
            bool isFront = (i < 2);
            float maxTravel = isFront ? frontSuspensionMaxTravel : rearSuspensionMaxTravel;
            
            // Draw tire sphere (yellow for all tires)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tirePos, 0.15f);
            
            // Draw suspension raycast (blue line downward - always full distance)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tirePos, tirePos + Vector3.down * suspensionDistance);
            
            // Draw suspension max travel point (green for front, cyan for rear)
            Gizmos.color = isFront ? Color.green : Color.cyan;
            Gizmos.DrawLine(tirePos, tirePos + Vector3.down * maxTravel);
            Gizmos.DrawWireSphere(tirePos + Vector3.down * maxTravel, 0.08f);
        }
        
        // Visualize weight transfer - show compression per tire
        if (Application.isPlaying)
        {
            float avgCompression = 0;
            for (int i = 0; i < 4; i++)
            {
                avgCompression += suspensionCompression[i];
            }
            avgCompression /= 4f;
            
            for (int i = 0; i < 4; i++)
            {
                if (tires[i] == null) continue;
                
                Vector3 tirePos = tires[i].position;
                
                // Calculate compression ratio for weight transfer visualization
                float compressionRatio = avgCompression > 0.01f ? suspensionCompression[i] / avgCompression : 1f;
                compressionRatio = Mathf.Clamp(compressionRatio, 0.5f, 1.5f);
                
                // Draw compression bar above each tire (red = high compression, blue = low compression)
                float barHeight = 0.5f;
                float barThickness = 0.1f;
                Vector3 barTop = tirePos + Vector3.up * barHeight * compressionRatio;
                
                // Color gradient: blue (low) -> green (normal) -> red (high)
                if (compressionRatio < 1f)
                    Gizmos.color = Color.Lerp(Color.blue, Color.green, compressionRatio);
                else
                    Gizmos.color = Color.Lerp(Color.green, Color.red, compressionRatio - 1f);
                
                Gizmos.DrawLine(tirePos, barTop);
                Gizmos.DrawWireSphere(barTop, barThickness);
            }
        }
        
        // Draw front tire directions (steering visualization)
        for (int i = 0; i < 2; i++)
        {
            if (tires[i] == null) continue;
            
            Vector3 tirePos = tires[i].position;
            Vector3 tireDirection = tires[i].forward;
            Vector3 tireRight = tires[i].right;
            
            // Forward direction line (cyan)
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(tirePos, tirePos + tireDirection * 0.5f);
            Gizmos.DrawWireSphere(tirePos + tireDirection * 0.5f, 0.08f);
            
            // Right direction line (magenta)
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(tirePos, tirePos + tireRight * 0.5f);
            Gizmos.DrawWireSphere(tirePos + tireRight * 0.5f, 0.08f);
        }
        
        // Draw car center of mass (red sphere)
        if (GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            Vector3 comPosition = transform.position + rb.centerOfMass;
            
            // Draw center of mass as a larger red sphere
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(comPosition, 0.4f);
            Gizmos.DrawWireSphere(comPosition, 0.3f);
            
            // Draw crosshairs through center of mass
            Gizmos.color = Color.red;
            Gizmos.DrawLine(comPosition - Vector3.right * 0.5f, comPosition + Vector3.right * 0.5f);
            Gizmos.DrawLine(comPosition - Vector3.up * 0.5f, comPosition + Vector3.up * 0.5f);
            Gizmos.DrawLine(comPosition - Vector3.forward * 0.5f, comPosition + Vector3.forward * 0.5f);
        }
        
        // Draw car forward direction (white line from center)
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
        
        // Draw car right direction (gray line from center)
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + transform.right * 0.8f);
    }
}
