using UnityEngine;

public class V2_CarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float accelerationForce = 80f; // How fast car accelerates
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float baseDrag = 0.975f; // Base friction/air resistance at low speed (higher = less drag)
    [SerializeField] private float speedDragMultiplier = 0.003f; // Additional drag at high speeds
    [SerializeField] private float brakingForce = 60f; // How fast car slows down when reversing
    [SerializeField] private float coastingDeceleration = 6f; // Natural slowdown when no input
    
    [Header("Steering")]
    [SerializeField] private float steerAngle = 45f;
    [SerializeField] private float steerSmoothness = 5f; // How quickly steering responds (higher = faster)
    [SerializeField] private float speedSteerSensitivity = 0.85f; // How speed affects steering (lower = less steering at high speed)
    
    [Header("Traction")]
    [SerializeField] private float traction = 0.15f; // How much velocity aligns to car direction (0-1, lower = more slide)
    
    [Header("Drift")]
    [SerializeField] private float driftThreshold = 20f; // Steering angle needed to trigger drift
    [SerializeField] private float driftTraction = 0.01f; // Reduced traction while drifting (more slide)
    [SerializeField] private float driftSpeed = 3f; // Minimum speed to drift
    [SerializeField] private float driftExitThreshold = 5f; // Steering angle to exit drift
    [SerializeField] private float driftSpeedPreservation = 0.998f; // Maintain speed during drift (higher = more momentum)

    private Vector3 moveForce = Vector3.zero; // Accumulated movement force
    private float steerInput = 0f; // Current steering input (-1 to 1)
    private float currentSteerAngle = 0f; // Smoothed steering angle
    private bool isDrifting = false; // Is car currently drifting
    private float driftTimer = 0f; // How long car has been drifting

    private void Update()
    {
        HandleMovement();
        HandleSteering();
        DetectDrift();
        ApplyDragAndLimit();
        ApplyTraction();
    }

    private void HandleMovement()
    {
        float accelerationInput = Input.GetAxis("Vertical");
        
        if (accelerationInput > 0)
        {
            // Forward acceleration (limited by max speed)
            moveForce += transform.forward * accelerationForce * Time.deltaTime;
        }
        else if (accelerationInput < 0)
        {
            // Braking/reversing
            moveForce -= transform.forward * brakingForce * Time.deltaTime;
        }
        else
        {
            // Coasting deceleration (natural slowdown)
            if (moveForce.magnitude > 0.1f)
            {
                moveForce = moveForce.normalized * Mathf.Max(moveForce.magnitude - coastingDeceleration * Time.deltaTime, 0f);
            }
        }
    }

    private void HandleSteering()
    {
        // Store steering input for drift detection later
        steerInput = Input.GetAxis("Horizontal");
        
        // Smoothly interpolate current steering angle toward target
        float targetSteerAngle = steerInput * steerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSmoothness * Time.deltaTime);
        
        // Steering response decreases at high speeds (realistic weight transfer)
        float speedInfluence = Mathf.Clamp01(moveForce.magnitude / maxSpeed);
        float steerResponsiveness = Mathf.Lerp(1f, speedSteerSensitivity, speedInfluence);
        
        // Steering is proportional to velocity magnitude
        float steerMagnitude = currentSteerAngle * moveForce.magnitude * steerResponsiveness * Time.deltaTime;
        transform.Rotate(Vector3.up * steerMagnitude);
    }

    private void ApplyDragAndLimit()
    {
        // During drift, maintain more speed for fun, continuous sliding
        float dragReduction = isDrifting ? driftSpeedPreservation : 1f;
        
        // Speed-dependent drag (more drag at higher speeds)
        float currentSpeed = moveForce.magnitude;
        float dragFactor = baseDrag - (currentSpeed / maxSpeed * speedDragMultiplier);
        dragFactor = Mathf.Clamp(dragFactor, 0.85f, 1f); // Clamp to prevent extreme drag
        dragFactor *= dragReduction; // Apply drift speed preservation
        moveForce *= dragFactor;
        
        // Limit to max speed
        moveForce = Vector3.ClampMagnitude(moveForce, maxSpeed);
        
        // Apply movement to position
        transform.position += moveForce * Time.deltaTime;
    }

    private void ApplyTraction()
    {
        // Traction: align velocity direction toward where car is facing
        // This prevents the car from moving purely sideways (adds grip)
        if (moveForce.magnitude > 0.01f)
        {
            // Use lower traction while drifting for longer, slidier drifts
            float currentTraction = isDrifting ? driftTraction : traction;
            moveForce = Vector3.Lerp(moveForce.normalized, transform.forward, currentTraction * Time.deltaTime) * moveForce.magnitude;
        }
    }

    private void DetectDrift()
    {
        float steerMagnitude = Mathf.Abs(currentSteerAngle);
        float currentSpeed = moveForce.magnitude;
        
        // Start drifting when steering is aggressive and speed is sufficient
        if (steerMagnitude > driftThreshold && currentSpeed > driftSpeed)
        {
            isDrifting = true;
            driftTimer += Time.deltaTime;
        }
        // Stop drifting when steering is relaxed or speed drops significantly
        else if (steerMagnitude < driftExitThreshold || currentSpeed < driftSpeed * 0.3f)
        {
            isDrifting = false;
            driftTimer = 0f;
        }
        else if (isDrifting)
        {
            // Keep drifting if moderately steering
            driftTimer += Time.deltaTime;
        }
    }

    // Getters for other systems (UI, effects, etc.)
    public float GetCurrentSpeed()
    {
        return moveForce.magnitude;
    }

    public float GetSteeringInput()
    {
        return steerInput;
    }

    public Vector3 GetVelocity()
    {
        return moveForce;
    }

    public bool IsMoving()
    {
        return moveForce.magnitude > 0.1f;
    }

    public bool IsDrifting()
    {
        return isDrifting;
    }

    public float GetDriftTimer()
    {
        return driftTimer;
    }
}