using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class cam : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCameraBase cameraA;
    [SerializeField] private CinemachineVirtualCameraBase cameraB;
    [SerializeField] private KeyCode switchKey = KeyCode.C;

    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 10;

    private bool isCameraAActive = true;

    private void Start()
    {
        SetActiveCamera(isCameraAActive);
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            isCameraAActive = !isCameraAActive;
            SetActiveCamera(isCameraAActive);
        }
    }

    private void SetActiveCamera(bool useCameraA)
    {
        if (cameraA == null || cameraB == null)
        {
            Debug.LogWarning("Assign both virtual cameras in the Inspector.", this);
            return;
        }

        cameraA.Priority = useCameraA ? activePriority : inactivePriority;
        cameraB.Priority = useCameraA ? inactivePriority : activePriority;
    }
}
