using UnityEngine;
using Cinemachine;

public class CameraSwitch : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera cameraA = null!;
[SerializeField] private CinemachineVirtualCamera cameraB = null!;
[SerializeField] private KeyCode switchKey = KeyCode.C;

    private CinemachineBrain brain;
    private bool isCameraAActive = true;

    private void Start()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
        SetActiveCamera(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey) && !brain.IsBlending)
        {
            isCameraAActive = !isCameraAActive;
            SetActiveCamera(isCameraAActive);
        }
    }

    private void SetActiveCamera(bool useCameraA)
    {
        cameraA.Priority = useCameraA ? 20 : 10;
        cameraB.Priority = useCameraA ? 10 : 20;
    }
}