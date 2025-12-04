using UnityEngine;

public class CarControllerRaycast : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float accelerationForce = 80f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float brakingForce = 60f;
    [SerializeField] private float coastingDeceleration = 6f;
    
    [Header("Steering")]
    [SerializeField] private float steerAngle = 45f;
    [SerializeField] private float steerSmoothness = 5f;
    [SerializeField] private float speedSteerSensitivity = 0.85f;
    
    [Header("Wheels")]
    [SerializeField] private float wheelRadius = 0.4f;
    [SerializeField] private Vector3 frontLeftOffset = new Vector3(-0.7f, 0.3f, 1.0f);
    [SerializeField] private Vector3 frontRightOffset = new Vector3(0.7f, 0.3f, 1.0f);
    [SerializeField] private Vector3 rearLeftOffset = new Vector3(-0.7f, 0.3f, -1.0f);
    [SerializeField] private Vector3 rearRightOffset = new Vector3(0.7f, 0.3f, -1.0f);
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Grip")]
    [SerializeField] private float tireGripCoefficient = 1.0f;
    [SerializeField] private float driftGripCoefficient = 0.3f;
    [SerializeField] private float maxGripForce = 50f;
    
    [Header("Drift")]
    [SerializeField] private float driftThreshold = 20f;
    [SerializeField] private float driftSpeed = 3f;
    [SerializeField] private float driftExitThreshold = 5f;
    [SerializeField] private float driftSpeedPreservation = 0.998f;

    [Header("Debug")]
    [SerializeField] private bool showWheelDebug = true;
    [SerializeField] private float debugForceScale = 0.1f; // Scale for visualizing forces

    private Vector3 moveForce = Vector3.zero;
    private float steerInput = 0f;
    private float currentSteerAngle = 0f;
    private bool isDrifting = false;
    private float driftTimer = 0f;
    
    private Rigidbody rb;
    
    // Store wheel data for debugging
    private WheelData debugFrontLeft, debugFrontRight, debugRearLeft, debugRearRight;
    private Vector3 debugFrontLeftForce, debugFrontRightForce, debugRearLeftForce, debugRearRightForce;

    private struct WheelData
    {
        public Vector3 position;
        public float groundDistance;
        public bool isGrounded;
        public Vector3 surfaceNormal;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("CarControllerRaycast requires a Rigidbody component!");
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleSteering();
        DetectDrift();
    }

    private void FixedUpdate()
    {
        ApplyWheelPhysics();
        ApplyDragAndLimit();
        UpdatePosition();
    }

    private void HandleMovement()
    {
        float accelerationInput = Input.GetAxis("Vertical");
        
        if (accelerationInput > 0)
        {
            moveForce += transform.forward * accelerationForce * Time.deltaTime;
        }
        else if (accelerationInput < 0)
        {
            moveForce -= transform.forward * brakingForce * Time.deltaTime;
        }
        else
        {
            if (moveForce.magnitude > 0.1f)
            {
                moveForce = moveForce.normalized * Mathf.Max(moveForce.magnitude - coastingDeceleration * Time.deltaTime, 0f);
            }
        }
    }

    private void HandleSteering()
    {
        steerInput = Input.GetAxis("Horizontal");
        
        float targetSteerAngle = steerInput * steerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, steerSmoothness * Time.deltaTime);
        
        float speedInfluence = Mathf.Clamp01(moveForce.magnitude / maxSpeed);
        float steerResponsiveness = Mathf.Lerp(1f, speedSteerSensitivity, speedInfluence);
        
        float steerMagnitude = currentSteerAngle * moveForce.magnitude * steerResponsiveness * Time.deltaTime;
        transform.Rotate(Vector3.up * steerMagnitude);
    }

    private void ApplyWheelPhysics()
    {
        // Get individual wheel data
        debugFrontLeft = GetWheelData(frontLeftOffset, true);
        debugFrontRight = GetWheelData(frontRightOffset, true);
        debugRearLeft = GetWheelData(rearLeftOffset, false);
        debugRearRight = GetWheelData(rearRightOffset, false);

        // Calculate grip for each wheel
        float gripCoeff = isDrifting ? driftGripCoefficient : tireGripCoefficient;
        
        Vector3 totalGripForce = Vector3.zero;
        
        // Front wheels - affected by steering
        if (debugFrontLeft.isGrounded)
        {
            debugFrontLeftForce = CalculateWheelGripForce(debugFrontLeft, gripCoeff, true);
            totalGripForce += debugFrontLeftForce;
        }
        else
            debugFrontLeftForce = Vector3.zero;
            
        if (debugFrontRight.isGrounded)
        {
            debugFrontRightForce = CalculateWheelGripForce(debugFrontRight, gripCoeff, true);
            totalGripForce += debugFrontRightForce;
        }
        else
            debugFrontRightForce = Vector3.zero;
        
        // Rear wheels - no steering
        if (debugRearLeft.isGrounded)
        {
            debugRearLeftForce = CalculateWheelGripForce(debugRearLeft, gripCoeff, false);
            totalGripForce += debugRearLeftForce;
        }
        else
            debugRearLeftForce = Vector3.zero;
            
        if (debugRearRight.isGrounded)
        {
            debugRearRightForce = CalculateWheelGripForce(debugRearRight, gripCoeff, false);
            totalGripForce += debugRearRightForce;
        }
        else
            debugRearRightForce = Vector3.zero;

        // Apply grip force to movement
        if (totalGripForce.magnitude > 0.01f)
        {
            moveForce = Vector3.Lerp(moveForce.normalized, transform.forward, gripCoeff * Time.fixedDeltaTime) * moveForce.magnitude;
        }

        // Speed preservation during drift
        if (isDrifting)
        {
            moveForce *= driftSpeedPreservation;
        }
    }

    private WheelData GetWheelData(Vector3 offset, bool isFrontWheel)
    {
        WheelData data = new WheelData();
        data.position = transform.position + transform.TransformDirection(offset);
        
        RaycastHit hit;
        if (Physics.Raycast(data.position, Vector3.down, out hit, wheelRadius + 0.5f, groundLayer))
        {
            data.isGrounded = true;
            data.groundDistance = hit.distance;
            data.surfaceNormal = hit.normal;
        }
        else
        {
            data.isGrounded = false;
            data.groundDistance = wheelRadius + 0.5f;
        }
        
        return data;
    }

    private Vector3 CalculateWheelGripForce(WheelData wheel, float gripCoeff, bool isFrontWheel)
    {
        Vector3 gripForce = Vector3.zero;
        
        // Calculate slip angle
        Vector3 wheelForward = transform.forward;
        if (isFrontWheel)
        {
            wheelForward = Quaternion.Euler(0, currentSteerAngle, 0) * transform.forward;
        }
        
        // Grip force in the direction the wheel is pointing
        float speedMagnitude = moveForce.magnitude;
        gripForce = wheelForward * Mathf.Clamp(speedMagnitude * gripCoeff, -maxGripForce, maxGripForce);
        
        return gripForce;
    }

    private void ApplyDragAndLimit()
    {
        float currentSpeed = moveForce.magnitude;
        float dragFactor = 0.975f - (currentSpeed / maxSpeed * 0.003f);
        dragFactor = Mathf.Clamp(dragFactor, 0.85f, 1f);
        moveForce *= dragFactor;
        
        moveForce = Vector3.ClampMagnitude(moveForce, maxSpeed);
    }

    private void UpdatePosition()
    {
        transform.position += moveForce * Time.fixedDeltaTime;
    }

    private void DetectDrift()
    {
        float steerMagnitude = Mathf.Abs(currentSteerAngle);
        float currentSpeed = moveForce.magnitude;
        
        if (steerMagnitude > driftThreshold && currentSpeed > driftSpeed)
        {
            isDrifting = true;
            driftTimer += Time.deltaTime;
        }
        else if (steerMagnitude < driftExitThreshold || currentSpeed < driftSpeed * 0.3f)
        {
            isDrifting = false;
            driftTimer = 0f;
        }
        else if (isDrifting)
        {
            driftTimer += Time.deltaTime;
        }
    }

    public float GetCurrentSpeed() => moveForce.magnitude;
    public float GetSteeringInput() => steerInput;
    public Vector3 GetVelocity() => moveForce;
    public bool IsMoving() => moveForce.magnitude > 0.1f;
    public bool IsDrifting() => isDrifting;
    public float GetDriftTimer() => driftTimer;

    private void OnDrawGizmos()
    {
        if (!showWheelDebug) return;

        Vector3 carPos = transform.position;
        
        // Draw wheel positions as circles
        DrawWheelGizmo(carPos, frontLeftOffset, debugFrontLeft, debugFrontLeftForce, "FL");
        DrawWheelGizmo(carPos, frontRightOffset, debugFrontRight, debugFrontRightForce, "FR");
        DrawWheelGizmo(carPos, rearLeftOffset, debugRearLeft, debugRearLeftForce, "RL");
        DrawWheelGizmo(carPos, rearRightOffset, debugRearRight, debugRearRightForce, "RR");
        
        // Draw total velocity
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(carPos, carPos + moveForce * debugForceScale);
    }

    private void DrawWheelGizmo(Vector3 carPos, Vector3 offset, WheelData wheelData, Vector3 forceVector, string label)
    {
        Vector3 wheelPos = carPos + transform.TransformDirection(offset);
        
        // Draw wheel contact point
        if (wheelData.isGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wheelPos, 0.1f);
            
            // Draw X axis (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wheelPos, wheelPos + Vector3.right * forceVector.x * debugForceScale);
            
            // Draw Y axis (Green)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(wheelPos, wheelPos + Vector3.up * forceVector.y * debugForceScale);
            
            // Draw Z axis (Blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wheelPos, wheelPos + Vector3.forward * forceVector.z * debugForceScale);
            
            // Draw total force vector (White)
            Gizmos.color = Color.white;
            Gizmos.DrawLine(wheelPos, wheelPos + forceVector * debugForceScale);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wheelPos, 0.1f);
        }
    }
}
