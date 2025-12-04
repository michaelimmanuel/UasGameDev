using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Components")]
    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    public Transform meshFL, meshFR, meshRL, meshRR;

    [Header("Engine Settings")]
    [Tooltip("Motor torque for acceleration")]
    public float motorTorque = 3000f;
    [Tooltip("Braking torque force")]
    public float brakeTorque = 5000f;
    [Tooltip("Maximum speed in km/h")]
    public float maxSpeed = 150f;

    [Header("Steering Settings")]
    [Tooltip("Maximum steering angle in degrees")]
    public float steeringAngle = 30f;
    [Tooltip("Steering response smoothing")]
    public float steeringSmoothing = 0.1f;

    [Header("Friction Settings")]
    [Tooltip("Drag/friction force to slow the car")]
    public float dragForce = 500f;
    [Tooltip("Forward friction grip (higher = more grip, less spinning)")]
    public float forwardGrip = 1f;
    [Tooltip("Sideways friction grip (higher = more grip, less sliding)")]
    public float sidewaysGrip = 1f;

    [Header("Handbrake Settings")]
    [Tooltip("Handbrake input key")]
    public KeyCode handbrakeKey = KeyCode.Space;
    [Tooltip("Friction reduction when handbrake is applied (0-1, lower = less grip)")]
    public float handbrakeFrictionReduction = 0.2f;
    [Tooltip("Braking force applied to rear wheels")]
    public float handbrakeBrakeForce = 5000f;

    [Header("Suspension & Stability")]
    [Tooltip("Suspension spring force")]
    public float suspensionSpring = 35000f;
    [Tooltip("Suspension damper")]
    public float suspensionDamper = 4500f;
    [Tooltip("Anti-roll bar force for stability")]
    public float antiRollForce = 2000f;
    [Tooltip("Drift stability assistance")]
    public float driftStability = 0.3f;

    [Header("Center of Mass")]
    [Tooltip("Center of mass offset for the car")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    private float inputVertical;
    private float inputHorizontal;
    private float currentSpeed;
    private float smoothSteerAngle;
    private bool isHandbrakeActive;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Set center of mass from the inspector setting
            rb.centerOfMass = centerOfMassOffset;
        }

        // Configure all wheels
        ConfigureWheel(wheelFL);
        ConfigureWheel(wheelFR);
        ConfigureWheel(wheelRL);
        ConfigureWheel(wheelRR);
    }

    void ConfigureWheel(WheelCollider wheel)
    {
        // Set suspension for smooth handling
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = suspensionSpring;
        spring.damper = suspensionDamper;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;

        // Configure friction for better drifting
        WheelFrictionCurve friction = wheel.forwardFriction;
        friction.stiffness = forwardGrip;
        wheel.forwardFriction = friction;

        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = sidewaysGrip;
        wheel.sidewaysFriction = sidewaysFriction;
    }

    void Update()
    {
        // Get input for forward/backward movement and steering
        inputVertical = Input.GetAxis("Vertical");
        inputHorizontal = Input.GetAxis("Horizontal");
        
        // Check for handbrake input
        isHandbrakeActive = Input.GetKey(handbrakeKey);
        
        // Update wheel grip in real-time for tuning
        UpdateWheelGrip();
        
        // Update wheel visuals
        UpdateWheelMeshes();
    }

    void FixedUpdate()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Convert to km/h

        // Smooth steering for natural feel
        float targetSteerAngle = steeringAngle * inputHorizontal;
        smoothSteerAngle = Mathf.Lerp(smoothSteerAngle, targetSteerAngle, steeringSmoothing);

        // Apply smooth steering to front wheels
        wheelFL.steerAngle = smoothSteerAngle;
        wheelFR.steerAngle = smoothSteerAngle;

        // Apply motor torque and braking to rear wheels
        if (inputVertical > 0)
        {
            // Accelerating - apply motor torque only if below max speed
            if (currentSpeed < maxSpeed)
            {
                float torque = motorTorque * inputVertical;
                wheelRL.motorTorque = torque;
                wheelRR.motorTorque = torque;
                wheelRL.brakeTorque = 0;
                wheelRR.brakeTorque = 0;
            }
            else
            {
                // At max speed - maintain but don't accelerate further
                wheelRL.motorTorque = 0;
                wheelRR.motorTorque = 0;
                wheelRL.brakeTorque = 0;
                wheelRR.brakeTorque = 0;
            }
        }
        else if (inputVertical < 0)
        {
            // Braking/Reversing
            wheelRL.motorTorque = motorTorque * inputVertical; // Negative for reverse
            wheelRR.motorTorque = motorTorque * inputVertical;
            wheelRL.brakeTorque = brakeTorque * Mathf.Abs(inputVertical);
            wheelRR.brakeTorque = brakeTorque * Mathf.Abs(inputVertical);
        }
        else
        {
            // No input - coast with natural drag
            wheelRL.motorTorque = 0;
            wheelRR.motorTorque = 0;
            wheelRL.brakeTorque = 0;
            wheelRR.brakeTorque = 0;
            // Apply gentle drag for natural deceleration
            rb.AddForce(-rb.linearVelocity.normalized * dragForce * 0.5f);
        }

        // Apply handbrake
        if (isHandbrakeActive)
        {
            ApplyHandbrake();
        }
        else if (inputVertical >= 0)
        {
            // Only reset friction if not braking
            ResetRearWheelFriction();
        }

        // Apply anti-roll bar for better cornering stability
        ApplyAntiRoll(rb);
    }

    void ApplyAntiRoll(Rigidbody rb)
    {
        // Anti-roll bar simulation for better stability during cornering
        WheelHit hit;

        // Calculate lateral acceleration to determine how much anti-roll to apply
        Vector3 velocity = rb.linearVelocity;
        float forwardSpeed = Vector3.Dot(velocity, transform.forward);
        float sidewaysSpeed = Vector3.Dot(velocity, transform.right);
        
        // During high sideways movement (drifting), reduce anti-roll effect
        float driftFactor = Mathf.Clamp01(1f - (Mathf.Abs(sidewaysSpeed) / 30f)); // Reduce when drifting
        float adjustedAntiRoll = antiRollForce * Mathf.Lerp(driftFactor, 1f, driftStability);

        // Front wheels anti-roll
        float leftTravel = GetWheelTravel(wheelFL);
        float rightTravel = GetWheelTravel(wheelFR);
        float antiRollForceFront = (leftTravel - rightTravel) * adjustedAntiRoll;

        rb.AddForceAtPosition(wheelFL.transform.up * -antiRollForceFront, wheelFL.transform.position);
        rb.AddForceAtPosition(wheelFR.transform.up * antiRollForceFront, wheelFR.transform.position);

        // Rear wheels anti-roll
        leftTravel = GetWheelTravel(wheelRL);
        rightTravel = GetWheelTravel(wheelRR);
        float antiRollForceRear = (leftTravel - rightTravel) * adjustedAntiRoll * 0.6f; // Rear has less effect

        rb.AddForceAtPosition(wheelRL.transform.up * -antiRollForceRear, wheelRL.transform.position);
        rb.AddForceAtPosition(wheelRR.transform.up * antiRollForceRear, wheelRR.transform.position);
    }

    float GetWheelTravel(WheelCollider wheel)
    {
        WheelHit hit;
        if (wheel.GetGroundHit(out hit))
        {
            return (-wheel.transform.InverseTransformPoint(hit.point).y - wheel.radius) / wheel.suspensionDistance;
        }
        return 1f;
    }

    void ApplyHandbrake()
    {
        // Reduce friction on rear wheels to break traction
        WheelFrictionCurve rearFriction = wheelRL.sidewaysFriction;
        rearFriction.stiffness = handbrakeFrictionReduction;
        wheelRL.sidewaysFriction = rearFriction;
        wheelRR.sidewaysFriction = rearFriction;

        // Apply braking force to rear wheels
        wheelRL.brakeTorque = handbrakeBrakeForce;
        wheelRR.brakeTorque = handbrakeBrakeForce;

        // Stop motor torque during handbrake
        wheelRL.motorTorque = 0;
        wheelRR.motorTorque = 0;
    }

    void ResetRearWheelFriction()
    {
        // Restore normal friction to rear wheels
        WheelFrictionCurve rearFriction = wheelRL.sidewaysFriction;
        rearFriction.stiffness = sidewaysGrip;
        wheelRL.sidewaysFriction = rearFriction;
        wheelRR.sidewaysFriction = rearFriction;

        // Reset brake torque
        wheelRL.brakeTorque = 0;
        wheelRR.brakeTorque = 0;
    }

    void UpdateWheelGrip()
    {
        // Update forward grip for all wheels
        UpdateWheelFriction(wheelFL, forwardGrip, sidewaysGrip);
        UpdateWheelFriction(wheelFR, forwardGrip, sidewaysGrip);
        UpdateWheelFriction(wheelRL, forwardGrip, sidewaysGrip);
        UpdateWheelFriction(wheelRR, forwardGrip, sidewaysGrip);
    }

    void UpdateWheelFriction(WheelCollider wheel, float forward, float sideways)
    {
        WheelFrictionCurve fwdFriction = wheel.forwardFriction;
        fwdFriction.stiffness = forward;
        wheel.forwardFriction = fwdFriction;

        WheelFrictionCurve sideFriction = wheel.sidewaysFriction;
        sideFriction.stiffness = sideways;
        wheel.sidewaysFriction = sideFriction;
    }

    void UpdateWheelMeshes()
    {
        UpdateSingleWheel(wheelFL, meshFL, false);
        UpdateSingleWheel(wheelFR, meshFR, true);
        UpdateSingleWheel(wheelRL, meshRL, false);
        UpdateSingleWheel(wheelRR, meshRR, true);
    }

    void UpdateSingleWheel(WheelCollider col, Transform mesh, bool isRightWheel)
    {
        Vector3 pos;
        Quaternion rot;

        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        
        if (isRightWheel)
        {
            // Flip right wheels 180 degrees on Y axis
            mesh.rotation = rot * Quaternion.Euler(0, 180f, 90f);
        }
        else
        {
            // Left wheels with standard rotation
            mesh.rotation = rot * Quaternion.Euler(0, 0, 90f);
        }
    }

    // Visualize center of mass in the editor
    void OnDrawGizmos()
    {
        // Always draw the center of mass
        Gizmos.color = Color.white;
        Vector3 comWorldPos = transform.TransformPoint(centerOfMassOffset);
        Gizmos.DrawSphere(comWorldPos, 0.05f);
        
        // Draw lines to show the offset
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, comWorldPos);
    }

}
