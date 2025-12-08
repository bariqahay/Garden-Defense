using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 720f;

    [Header("Input Settings")]
    [Tooltip("Auto-detect input mode atau force manual")]
    public InputMode inputMode = InputMode.AutoDetect;

    [Tooltip("Deadzone untuk joystick (0-1)")]
    public float joystickDeadzone = 0.2f;

    [Tooltip("Sensitivity untuk rotation pake controller")]
    public float controllerRotationSpeed = 360f;

    [Tooltip("Kalau right stick idle, rotate ke arah movement")]
    public bool rotateToMovementWhenIdle = false;

    [Header("Boundary Settings")]
    [Tooltip("Enable/disable boundary limit")]
    public bool useBoundary = true;

    [Tooltip("Use camera view as boundary (recommended!)")]
    public bool useCameraBoundary = true;

    [Tooltip("Offset dari tepi camera (unit)")]
    public float cameraEdgeOffset = 1f;

    [Header("Manual Boundary (if not using camera)")]
    public float boundaryX = 50f;
    public float boundaryZ = 50f;

    [Header("Collision Settings")]
    [Tooltip("Enable collision detection (ga boleh pake CharacterController)")]
    public bool useCollisionDetection = true;

    [Tooltip("Radius collision check (sesuaikan ukuran player)")]
    public float collisionRadius = 0.5f;

    [Tooltip("Distance check untuk collision")]
    public float collisionCheckDistance = 0.1f;

    [Tooltip("Layer yang bakal di-detect sebagai obstacle")]
    public LayerMask collisionLayers = -1;

    [Header("Debug")]
    public bool showBoundaryGizmos = true;
    public bool showCollisionGizmos = true;
    public bool logBoundaryHits = false;
    public bool logCollisionHits = false;
    public bool showInputDebug = false;

    private Animator animator;
    private Camera mainCam;
    private Vector3 minBounds;
    private Vector3 maxBounds;
    private InputMode currentInputMode;
    private Vector3 lastAimDirection;
    private Vector3 currentMovementDirection;

    public enum InputMode
    {
        AutoDetect,
        KeyboardMouse,
        Controller
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        mainCam = Camera.main;

        if (mainCam == null)
        {
            Debug.LogError("⚠️ Main Camera not found!");
        }

        // Auto-setup collision radius kalo ada collider
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null && collisionRadius == 0.5f)
        {
            collisionRadius = capsule.radius;
        }

        // Initialize aim direction
        lastAimDirection = transform.forward;
        currentMovementDirection = Vector3.zero;

        // Set initial input mode
        currentInputMode = inputMode;
        if (inputMode == InputMode.AutoDetect)
        {
            currentInputMode = InputMode.KeyboardMouse; // Default
        }
    }

    void Update()
    {
        // Auto-detect input mode
        if (inputMode == InputMode.AutoDetect)
        {
            DetectInputMode();
        }

        // Update camera bounds setiap frame
        if (useCameraBoundary && mainCam != null)
        {
            UpdateCameraBounds();
        }

        HandleMovement();
        HandleRotation();

        if (showInputDebug)
        {
            Debug.Log($"🎮 Input Mode: {currentInputMode} | Aim: {lastAimDirection}");
        }
    }

    void DetectInputMode()
    {
        // Detect controller input (with safety check)
        float rightStickH = 0f;
        float rightStickV = 0f;

        try
        {
            rightStickH = Input.GetAxis("RightStickHorizontal");
            rightStickV = Input.GetAxis("RightStickVertical");
        }
        catch (System.ArgumentException)
        {
            // Axis belum di-setup, skip controller detection
            if (currentInputMode != InputMode.KeyboardMouse)
            {
                currentInputMode = InputMode.KeyboardMouse;
            }
            return;
        }

        // Kalo ada input dari right stick, switch ke controller
        if (Mathf.Abs(rightStickH) > joystickDeadzone || Mathf.Abs(rightStickV) > joystickDeadzone)
        {
            if (currentInputMode != InputMode.Controller)
            {
                currentInputMode = InputMode.Controller;
                if (showInputDebug) Debug.Log("🎮 Switched to CONTROLLER mode");
            }
        }
        // Kalo mouse gerak, switch ke keyboard/mouse
        else if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            if (currentInputMode != InputMode.KeyboardMouse)
            {
                currentInputMode = InputMode.KeyboardMouse;
                if (showInputDebug) Debug.Log("⌨️ Switched to KEYBOARD/MOUSE mode");
            }
        }
    }

    void UpdateCameraBounds()
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        // Bottom-left corner
        Ray rayBottomLeft = mainCam.ScreenPointToRay(new Vector3(0, 0, 0));
        if (groundPlane.Raycast(rayBottomLeft, out float distBL))
        {
            Vector3 pointBL = rayBottomLeft.GetPoint(distBL);
            minBounds.x = pointBL.x + cameraEdgeOffset;
            minBounds.z = pointBL.z + cameraEdgeOffset;
        }

        // Top-right corner
        Ray rayTopRight = mainCam.ScreenPointToRay(new Vector3(Screen.width, Screen.height, 0));
        if (groundPlane.Raycast(rayTopRight, out float distTR))
        {
            Vector3 pointTR = rayTopRight.GetPoint(distTR);
            maxBounds.x = pointTR.x - cameraEdgeOffset;
            maxBounds.z = pointTR.z - cameraEdgeOffset;
        }
    }

    void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        currentMovementDirection = movementDirection; // Save untuk rotation fallback

        if (movementDirection.magnitude > 0.1f)
        {
            // Hitung posisi baru
            Vector3 moveVector = movementDirection * speed * Time.deltaTime;
            Vector3 newPosition = transform.position + moveVector;

            // CEK COLLISION DULU SEBELUM GERAK
            if (useCollisionDetection)
            {
                newPosition = CheckAndResolveCollision(transform.position, moveVector, newPosition);
            }

            // Clamp berdasarkan mode (camera boundary)
            if (useBoundary)
            {
                if (useCameraBoundary && mainCam != null)
                {
                    newPosition = ClampToCameraBounds(newPosition);
                }
                else
                {
                    newPosition = ClampToManualBounds(newPosition);
                }
            }

            transform.position = newPosition;

            if (animator) animator.SetBool("IsMoving", true);
        }
        else
        {
            if (animator) animator.SetBool("IsMoving", false);
        }
    }

    void HandleRotation()
    {
        if (mainCam == null) return;

        Vector3 aimDirection = Vector3.zero;

        // PILIH INPUT MODE
        if (currentInputMode == InputMode.KeyboardMouse ||
            (inputMode == InputMode.AutoDetect && currentInputMode == InputMode.KeyboardMouse))
        {
            // ========== MOUSE AIM ==========
            aimDirection = GetMouseAimDirection();

            // Mouse ALWAYS rotate (kecuali mouse ga gerak sama sekali)
            if (aimDirection.sqrMagnitude > 0.001f)
            {
                lastAimDirection = aimDirection;
                Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // ========== CONTROLLER AIM (RIGHT STICK) ==========
            aimDirection = GetControllerAimDirection();

            if (aimDirection.sqrMagnitude > 0.001f)
            {
                // RIGHT STICK ADA INPUT → Rotate ke arah stick
                lastAimDirection = aimDirection;
                Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    controllerRotationSpeed * Time.deltaTime
                );
            }
            else if (rotateToMovementWhenIdle && currentMovementDirection.sqrMagnitude > 0.1f)
            {
                // RIGHT STICK IDLE + opsi rotateToMovement ON → Rotate ke arah movement
                Quaternion targetRotation = Quaternion.LookRotation(currentMovementDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    controllerRotationSpeed * Time.deltaTime
                );
                lastAimDirection = currentMovementDirection;
            }
            // Else: RIGHT STICK IDLE + rotateToMovement OFF → KEEP last rotation (twin-stick shooter style)
        }
    }

    Vector3 GetMouseAimDirection()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        Ray ray = mainCam.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 pointToLook = ray.GetPoint(rayDistance);
            Vector3 lookDirection = (pointToLook - transform.position).normalized;
            lookDirection.y = 0;
            return lookDirection;
        }

        return Vector3.zero;
    }

    Vector3 GetControllerAimDirection()
    {
        // Right Stick Input (untuk aiming)
        float rightH = 0f;
        float rightV = 0f;

        try
        {
            rightH = Input.GetAxis("RightStickHorizontal");
            rightV = Input.GetAxis("RightStickVertical");
        }
        catch (System.ArgumentException)
        {
            // Axis belum di-setup, return zero
            return Vector3.zero;
        }

        // Apply deadzone
        if (Mathf.Abs(rightH) < joystickDeadzone) rightH = 0;
        if (Mathf.Abs(rightV) < joystickDeadzone) rightV = 0;

        // Kalo ada input dari right stick
        if (Mathf.Abs(rightH) > 0.01f || Mathf.Abs(rightV) > 0.01f)
        {
            Vector3 aimDir = new Vector3(rightH, 0, rightV).normalized;
            return aimDir;
        }

        // Kalo ga ada input, return zero (bukan lastAimDirection)
        return Vector3.zero;
    }

    Vector3 CheckAndResolveCollision(Vector3 currentPos, Vector3 moveVector, Vector3 targetPos)
    {
        float distance = moveVector.magnitude + collisionCheckDistance;
        Vector3 direction = moveVector.normalized;
        Vector3 castOrigin = currentPos + Vector3.up * collisionRadius;

        if (Physics.SphereCast(castOrigin, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers))
        {
            if (logCollisionHits)
            {
                Debug.Log($"💥 COLLISION DETECTED with: {hit.collider.gameObject.name}");
            }

            // SLIDING
            Vector3 slideDirection = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
            Vector3 slideMove = slideDirection * moveVector.magnitude;
            Vector3 slideTarget = currentPos + slideMove;
            Vector3 slideCastOrigin = currentPos + Vector3.up * collisionRadius;

            if (!Physics.SphereCast(slideCastOrigin, collisionRadius, slideMove.normalized, out _, slideMove.magnitude + collisionCheckDistance, collisionLayers))
            {
                return slideTarget;
            }

            return currentPos;
        }

        return targetPos;
    }

    Vector3 ClampToCameraBounds(Vector3 pos)
    {
        Vector3 clampedPos = pos;
        Vector3 originalPos = pos;

        clampedPos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        clampedPos.z = Mathf.Clamp(pos.z, minBounds.z, maxBounds.z);

        if (logBoundaryHits && originalPos != clampedPos)
        {
            Debug.Log($"🎥 HIT CAMERA BOUNDARY! From {originalPos} → {clampedPos}");
        }

        return clampedPos;
    }

    Vector3 ClampToManualBounds(Vector3 pos)
    {
        Vector3 clampedPos = pos;
        Vector3 originalPos = pos;

        clampedPos.x = Mathf.Clamp(pos.x, -boundaryX, boundaryX);
        clampedPos.z = Mathf.Clamp(pos.z, -boundaryZ, boundaryZ);

        if (logBoundaryHits && originalPos != clampedPos)
        {
            Debug.Log($"🚧 HIT MANUAL BOUNDARY! From {originalPos} → {clampedPos}");
        }

        return clampedPos;
    }

    void OnDrawGizmos()
    {
        if (showBoundaryGizmos && useBoundary)
        {
            if (useCameraBoundary && Application.isPlaying && mainCam != null)
            {
                Gizmos.color = Color.cyan;

                Vector3 p1 = new Vector3(minBounds.x, 0, minBounds.z);
                Vector3 p2 = new Vector3(maxBounds.x, 0, minBounds.z);
                Vector3 p3 = new Vector3(maxBounds.x, 0, maxBounds.z);
                Vector3 p4 = new Vector3(minBounds.x, 0, maxBounds.z);

                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p4);
                Gizmos.DrawLine(p4, p1);

                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(p1, 0.3f);
                Gizmos.DrawSphere(p2, 0.3f);
                Gizmos.DrawSphere(p3, 0.3f);
                Gizmos.DrawSphere(p4, 0.3f);
            }
            else if (!useCameraBoundary)
            {
                Gizmos.color = Color.yellow;

                Vector3 p1 = new Vector3(-boundaryX, 0, -boundaryZ);
                Vector3 p2 = new Vector3(boundaryX, 0, -boundaryZ);
                Vector3 p3 = new Vector3(boundaryX, 0, boundaryZ);
                Vector3 p4 = new Vector3(-boundaryX, 0, boundaryZ);

                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p3);
                Gizmos.DrawLine(p3, p4);
                Gizmos.DrawLine(p4, p1);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(p1, 0.3f);
                Gizmos.DrawSphere(p2, 0.3f);
                Gizmos.DrawSphere(p3, 0.3f);
                Gizmos.DrawSphere(p4, 0.3f);
            }
        }

        if (showCollisionGizmos && useCollisionDetection && Application.isPlaying)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * collisionRadius, collisionRadius);

            // Draw aim direction (GREEN = current aim)
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, lastAimDirection * 2f);

            // Draw movement direction (YELLOW = movement)
            if (currentMovementDirection.sqrMagnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.3f, currentMovementDirection * 1.5f);
            }
        }
    }

    void OnValidate()
    {
        boundaryX = Mathf.Abs(boundaryX);
        boundaryZ = Mathf.Abs(boundaryZ);
        cameraEdgeOffset = Mathf.Max(0, cameraEdgeOffset);
        collisionRadius = Mathf.Max(0.1f, collisionRadius);
        collisionCheckDistance = Mathf.Max(0.01f, collisionCheckDistance);
        joystickDeadzone = Mathf.Clamp01(joystickDeadzone);
    }
}