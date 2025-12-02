using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float groundDrag = 5f;
    [SerializeField] private float acceleration = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldown = 0.25f;
    [SerializeField] private float airMultiplier = 0.4f;
    private float jumpTimer = 0f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchScale = 0.5f;
    private Vector3 originalScale;
    private bool isCrouching = false;

    [Header("Sprint & Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 10f;
    [SerializeField] private float staminaRegenDelay = 2f;
    private float currentStamina;
    private float staminaRegenTimer = 0f;

    [Header("Vault")]
    [SerializeField] private float vaultHeight = 1.5f;
    [SerializeField] private float vaultDistance = 2f;
    [SerializeField] private float vaultDuration = 0.5f;
    private bool isVaulting = false;
    private float vaultTimer = 0f;
    private Vector3 vaultStartPos;
    private Vector3 vaultEndPos;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;
    private RaycastHit groundHit;

    private CharacterController characterController;
    private Vector3 velocity = Vector3.zero;
    private float currentSpeed;
    private bool isSprinting = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        originalScale = transform.localScale;
        currentStamina = maxStamina;
    }

    void Update()
    {
        // Ground check
        GroundCheck();

        // Handle input
        HandleMovementInput();
        HandleSprintInput();
        HandleJumpInput();
        HandleCrouchInput();
        HandleVaultInput();

        // Update stamina
        UpdateStamina();

        // Apply movement
        ApplyMovement();

        // Reset jump timer
        if (jumpTimer > 0)
            jumpTimer -= Time.deltaTime;
    }

    private void GroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, 
            playerHeight * 0.5f + 0.2f, groundLayer);

        Debug.DrawRay(transform.position, Vector3.down * (playerHeight * 0.5f + 0.2f), 
            isGrounded ? Color.green : Color.red);
    }

    private void HandleMovementInput()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        moveDirection.Normalize();

        if (isVaulting)
            return;

        // Determine current speed
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isSprinting && currentStamina > 0)
            currentSpeed = sprintSpeed;
        else
            currentSpeed = walkSpeed;

        // Apply movement with acceleration
        float targetSpeed = moveDirection.magnitude > 0 ? currentSpeed : 0;
        float speedDifference = targetSpeed - new Vector3(velocity.x, 0, velocity.z).magnitude;
        float accelRate = speedDifference > 0 ? acceleration : acceleration * 0.5f;

        velocity.x = Mathf.Lerp(velocity.x, moveDirection.x * currentSpeed, Time.deltaTime * accelRate);
        velocity.z = Mathf.Lerp(velocity.z, moveDirection.z * currentSpeed, Time.deltaTime * accelRate);

        // Apply drag
        if (isGrounded)
        {
            velocity.x *= (1f - groundDrag * Time.deltaTime);
            velocity.z *= (1f - groundDrag * Time.deltaTime);
        }
    }

    private void HandleSprintInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (!isCrouching && currentStamina > 0)
                isSprinting = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSprinting = false;
        }

        // Stop sprinting if stamina depleted
        if (currentStamina <= 0)
            isSprinting = false;
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && jumpTimer <= 0)
        {
            Jump();
            jumpTimer = jumpCooldown;
        }
    }

    private void Jump()
    {
        // Reset Y velocity
        velocity.y = 0;

        // Apply jump force
        velocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);

        isSprinting = false;
    }

    private void HandleCrouchInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!isCrouching)
            {
                isCrouching = true;
                transform.localScale = new Vector3(originalScale.x, originalScale.y * crouchScale, originalScale.z);
            }
            else
            {
                isCrouching = false;
                transform.localScale = originalScale;
            }

            isSprinting = false;
        }
    }

    private void HandleVaultInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isVaulting && !isCrouching)
        {
            if (CanVault())
            {
                StartVault();
            }
        }
    }

    private bool CanVault()
    {
        // Check for obstacle in front
        Vector3 checkPos = transform.position + transform.forward * 0.5f;

        // Check if there's a wall/object to vault over
        if (Physics.Raycast(checkPos + Vector3.up * 0.5f, transform.forward, out RaycastHit hitFront, vaultDistance))
        {
            // Check if there's space to land on
            Vector3 landCheckPos = hitFront.point + transform.forward * 0.3f;
            if (Physics.Raycast(landCheckPos + Vector3.up * vaultHeight, Vector3.down, out RaycastHit hitLand, vaultHeight + 1f))
            {
                return true;
            }
        }

        return false;
    }

    private void StartVault()
    {
        isVaulting = true;
        vaultTimer = 0;
        vaultStartPos = transform.position;

        // Calculate vault end position
        RaycastHit hitFront;
        Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hitFront, vaultDistance);
        vaultEndPos = hitFront.point + transform.forward * 0.5f;

        // Adjust height based on ground
        Physics.Raycast(vaultEndPos + Vector3.up * vaultHeight, Vector3.down, out RaycastHit hitLand, vaultHeight + 1f);
        vaultEndPos.y = hitLand.point.y;

        isSprinting = false;
        velocity.y = 0;
    }

    private void UpdateVault()
    {
        vaultTimer += Time.deltaTime;
        float vaultProgress = vaultTimer / vaultDuration;

        if (vaultProgress >= 1f)
        {
            vaultProgress = 1f;
            isVaulting = false;
        }

        // Smooth arc over obstacle
        Vector3 midPoint = (vaultStartPos + vaultEndPos) * 0.5f;
        midPoint.y = Mathf.Max(vaultStartPos.y, vaultEndPos.y) + vaultHeight;

        Vector3 newPos = BezierCurve(vaultStartPos, midPoint, vaultEndPos, vaultProgress);
        characterController.Move(newPos - transform.position);
    }

    private Vector3 BezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0 + 2 * u * t * p1 + tt * p2;
        return p;
    }

    private void UpdateStamina()
    {
        if (isSprinting && isGrounded)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            staminaRegenTimer -= Time.deltaTime;
            if (staminaRegenTimer <= 0 && currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
    }

    private void ApplyMovement()
    {
        if (isVaulting)
        {
            UpdateVault();
        }
        else
        {
            // Apply gravity
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            else
            {
                velocity.y += Physics.gravity.y * Time.deltaTime;
            }

            // Apply air resistance
            if (!isGrounded)
            {
                velocity.x *= (1f - groundDrag * airMultiplier * Time.deltaTime);
                velocity.z *= (1f - groundDrag * airMultiplier * Time.deltaTime);
            }

            // Move character
            characterController.Move(velocity * Time.deltaTime);
        }
    }

    // Public method to get current stamina (useful for UI)
    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    // Public method to check if sprinting
    public bool IsSprinting()
    {
        return isSprinting;
    }

    // Public method to check if crouching
    public bool IsCrouching()
    {
        return isCrouching;
    }
}
