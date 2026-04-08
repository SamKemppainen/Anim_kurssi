using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float AnimatorSpeed = 1f;
    public float DistanceToGround = 0.05f;
    public float IKWeightBlendSpeed = 8f;
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

    private float leftHandIKWeight;
    private float rightHandIKWeight;
    private float headIKWeight;

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

        animator.SetFloat("Horizontal", horizontalInput * inputMultiplier, 0.1f, Time.deltaTime);
        animator.SetFloat("Vertical", verticalInput * inputMultiplier, 0.1f, Time.deltaTime);

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
        {
            // Reject target if it is behind the player.
            // Dot product is positive when the target is in the forward hemisphere.
            Vector3 directionToTarget = (hit.point - transform.position).normalized;
            bool isInFront = Vector3.Dot(transform.forward, directionToTarget) > 0f;

            if (isInFront)
                currentlyAimedCollider = hit.collider.gameObject.GetComponent<IShootable>()?.TakeHit();
            else
                currentlyAimedCollider = null;
        }
        else
        {
            currentlyAimedCollider = null;
        }
    }

    private void SetHeadIK(Vector3 targetPosition, float weight)
    {
        animator.SetLookAtWeight(weight);
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

        // Hand IK only activates when a shootable target is under the cursor.
        bool hasShootableTarget = currentlyAimedCollider != null;

        // Blend IK weights to avoid snapping when releasing mouse buttons.
        float leftTargetWeight = (leftMouseHeld && hasShootableTarget) ? 1f : 0f;
        float rightTargetWeight = (rightMouseHeld && hasShootableTarget) ? 1f : 0f;
        float headTargetWeight = (leftMouseHeld || rightMouseHeld) ? 1f : 0f;

        leftHandIKWeight = Mathf.MoveTowards(leftHandIKWeight, leftTargetWeight, IKWeightBlendSpeed * Time.deltaTime);
        rightHandIKWeight = Mathf.MoveTowards(rightHandIKWeight, rightTargetWeight, IKWeightBlendSpeed * Time.deltaTime);
        headIKWeight = Mathf.MoveTowards(headIKWeight, headTargetWeight, IKWeightBlendSpeed * Time.deltaTime);

        // Smoothly move hands
        leftHandCurrentPosition = Vector3.Lerp(leftHandCurrentPosition, leftHandTargetPosition, Time.deltaTime * 10f);
        SetHandIKPos(AvatarIKGoal.LeftHand, leftHandCurrentPosition, leftHandIKWeight);

        rightHandCurrentPosition = Vector3.Lerp(rightHandCurrentPosition, rightHandTargetPosition, Time.deltaTime * 10f);
        SetHandIKPos(AvatarIKGoal.RightHand, rightHandCurrentPosition, rightHandIKWeight);

        // Feet follow ground
        SetFootIKPos(AvatarIKGoal.LeftFoot);
        SetFootIKPos(AvatarIKGoal.RightFoot);

        // Smoothly move head
        headCurrentPosition = Vector3.Lerp(headCurrentPosition, headTargetPosition, Time.deltaTime * 10f);
        SetHeadIK(headCurrentPosition, headIKWeight);
    }
}