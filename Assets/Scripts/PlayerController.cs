using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float AnimatorSpeed = 1f;
    public float DistanceToGround = 0.05f;
    public LayerMask IKLayers;

    protected Animator animator => GetComponent<Animator>();
    private CharacterController characterController => GetComponent<CharacterController>();

    private float inputMultiplier = 0.5f;

    private bool groundedPlayer;
    private float gravityValue = -9.81f;
    private float velocityY = 0;

    // IK variables
    private Collider currentlyAimedCollider;

    public Transform transformLeftHand;
    public Transform transformRightHand;

    public Vector3 headTargetPosition;
    public Vector3 headCurrentPosition;

    public Vector3 leftHandTargetPosition;
    public Vector3 leftHandCurrentPosition;

    public Vector3 rightHandTargetPosition;
    public Vector3 rightHandCurrentPosition;

    private void Awake()
    {
        animator.speed = AnimatorSpeed;

        transformLeftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        transformRightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Is running?
        inputMultiplier = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 1f : 0.5f;

        animator.SetFloat("Horizontal", horizontalInput * inputMultiplier);
        animator.SetFloat("Vertical", verticalInput * inputMultiplier);

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput);
        float inputMagnitude = Mathf.Clamp01(movementDirection.magnitude);

        animator.SetBool("IsMoving", inputMagnitude >= 0.01f);

        groundedPlayer = characterController.isGrounded;
        if (groundedPlayer && velocityY < 0)
            velocityY = 0f;

        GetAimTarget();
    }

    private void OnAnimatorMove()
    {
        Vector3 velocity = animator.deltaPosition;
        velocity.y += gravityValue * Time.deltaTime;
        characterController.Move(velocity);
    }

    // --- IK METHODS ---

    private void GetAimTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
            currentlyAimedCollider = hit.collider.gameObject.GetComponent<IShootable>()?.TakeHit();
        else
            currentlyAimedCollider = null;
    }

    private void SetHeadIK(Vector3 targetPosition)
    {
        animator.SetLookAtWeight(1f);
        animator.SetLookAtPosition(targetPosition);
    }

    private void SetHandIKPos(AvatarIKGoal avatarIKHandGoal, Vector3 targetPosition, float weight)
    {
        animator.SetIKPositionWeight(avatarIKHandGoal, weight);
        animator.SetIKPosition(avatarIKHandGoal, targetPosition);
    }

    private void SetFootIKPos(AvatarIKGoal avatarIKFootGoal)
    {
        animator.SetIKPositionWeight(avatarIKFootGoal, 1f);
        animator.SetIKRotationWeight(avatarIKFootGoal, 1f);

        RaycastHit groundHit;
        Ray groundRay = new Ray(animator.GetIKPosition(avatarIKFootGoal) + Vector3.up, Vector3.down);

        if (Physics.Raycast(groundRay, out groundHit, DistanceToGround + 1f, IKLayers))
        {
            Vector3 footPos = groundHit.point;
            footPos.y += DistanceToGround;
            animator.SetIKPosition(avatarIKFootGoal, footPos);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
          // Foot IK on Base Layer (index 0) only
    if (layerIndex == 0)
    {
        SetFootIKPos(AvatarIKGoal.LeftFoot);
        SetFootIKPos(AvatarIKGoal.RightFoot);
        return; // stop here for base layer
    }

    // Everything below only runs on UpperBody layer (index 1)
    headTargetPosition = transform.position + transform.forward + new Vector3(0, 1.5f, 0);

    bool leftMouseHeld = Input.GetMouseButton(0);
    bool rightMouseHeld = Input.GetMouseButton(1);
       

        if (currentlyAimedCollider != null)
        {
            // Head looks at target if either button held
            if (leftMouseHeld || rightMouseHeld)
                headTargetPosition = currentlyAimedCollider.bounds.center;

            // Left hand reaches if Left Mouse held
            if (leftMouseHeld)
                leftHandTargetPosition = currentlyAimedCollider.bounds.center;
            else if (transformLeftHand != null)
                leftHandTargetPosition = transformLeftHand.position;

            // Right hand reaches if Right Mouse held
            if (rightMouseHeld)
                rightHandTargetPosition = currentlyAimedCollider.bounds.center;
            else if (transformRightHand != null)
                rightHandTargetPosition = transformRightHand.position;
        }
        else
        {
            // No target, hands return to animation position
            if (transformLeftHand != null)
                leftHandTargetPosition = transformLeftHand.position;
            if (transformRightHand != null)
                rightHandTargetPosition = transformRightHand.position;
        }

        // Hand weights — 0 when not aiming, hands fully follow animation
        float leftWeight = leftMouseHeld ? 1f : 0f;
        float rightWeight = rightMouseHeld ? 1f : 0f;

        // Smoothly move hands
        leftHandCurrentPosition = Vector3.Lerp(leftHandCurrentPosition, leftHandTargetPosition, Time.deltaTime * 10f);
        SetHandIKPos(AvatarIKGoal.LeftHand, leftHandCurrentPosition, leftWeight);

        rightHandCurrentPosition = Vector3.Lerp(rightHandCurrentPosition, rightHandTargetPosition, Time.deltaTime * 10f);
        SetHandIKPos(AvatarIKGoal.RightHand, rightHandCurrentPosition, rightWeight);

        // Feet follow ground
        SetFootIKPos(AvatarIKGoal.LeftFoot);
        SetFootIKPos(AvatarIKGoal.RightFoot);

        // Smoothly move head
        headCurrentPosition = Vector3.Lerp(headCurrentPosition, headTargetPosition, Time.deltaTime * 10f);
        SetHeadIK(headCurrentPosition);
    }
}