using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rot : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 150f;
    [SerializeField] private bool invertVertical = true;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float yRotation = mouseX * rotationSpeed * Time.deltaTime;
            float xRotation = mouseY * rotationSpeed * Time.deltaTime * (invertVertical ? -1f : 1f);

            transform.Rotate(0f, yRotation, 0f, Space.World);
            transform.Rotate(xRotation, 0f, 0f, Space.Self);
        }
    }
}
