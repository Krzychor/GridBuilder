using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public class CameraSettings
{
    public float rotation = 45;
    public float movementSpeed = 1;
    public float height = 0;

    public static CameraSettings Lerp(CameraSettings A, CameraSettings B, float t)
    {
        CameraSettings result = new CameraSettings();
        result.rotation = Mathf.Lerp(A.rotation, B.rotation, t);
        result.movementSpeed = Mathf.Lerp(A.movementSpeed, B.movementSpeed, t);
        result.height = Mathf.Lerp(A.height, B.height, t);

        return result;
    }
}


[RequireComponent(typeof(Camera))]
public class BuildCamera : MonoBehaviour
{
    public float mouseSensivity = 0.01f;
    public float scrollSensivity = 0.01f;
    public float maxRange = 30;
    public float speed = 1.0f;

    new Camera camera;
    [SerializeField]
    LayerMask mask;
    [SerializeField]
    CameraSettings max = new CameraSettings();
    [SerializeField]
    CameraSettings min = new CameraSettings();
    [SerializeField, Range(0, 1)]
    float settingsLerp;


    Rotator rotator;
    public void CenterAt(Transform obj)
    {
        transform.position = obj.position + new Vector3(0, min.height, -2);
    }


    void OnValidate()
    {
        //ApplySettings(CameraSettings.Lerp(min, max, settingsLerp));
    }

    void Awake()
    {
        if (camera == null)
            camera = Camera.main;
        rotator = new Rotator(this);
    }


    void Update()
    {
        rotator.Update();

        if(rotator.IsRotating() == false)
            Move();

    }

    void Move()
    {
        Vector3 dir = new();
        if (Keyboard.current[Key.D].isPressed)
            dir += transform.right;
        if (Keyboard.current[Key.A].isPressed)
            dir -= transform.right;
        if (Keyboard.current[Key.W].isPressed)
            dir += transform.forward;
        if (Keyboard.current[Key.S].isPressed)
            dir -= transform.forward;
        dir.y = 0;
        dir *= speed * Time.deltaTime;
        transform.position += dir;

        
        float scroll = -Mouse.current.scroll.ReadValue().y;
        settingsLerp += scroll * scrollSensivity;   
        settingsLerp = Mathf.Clamp01(settingsLerp);
        ApplySettings(CameraSettings.Lerp(min, max, settingsLerp));
    }

    void ApplySettings(CameraSettings settings)
    {
        Vector3 pos = transform.position;
    //    if(anchor)
     //       pos.y = anchor.position.y + settings.height;
     //   else
            pos.y = settings.height;

        Vector3 rotation = transform.localRotation.eulerAngles;
        rotation.x = settings.rotation;
        transform.localRotation = Quaternion.Euler(rotation);
        speed = settings.movementSpeed;
        transform.position = pos;
    }

    public void OnFocusGain() { }

    public void OnFocusLost() { }


    public class Rotator
    {
        public Rotator(BuildCamera buildCamera_)
        {
            buildCamera = buildCamera_;
        }

        public bool IsRotating()
        {
            return isRotating;
        }

        public void Update()
        {

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                beforeRotation = buildCamera.transform.rotation;
                beforePosition = buildCamera.transform.position;
                canRotate = GetHitPoint(out Vector3 point);
                if (canRotate)
                {
                    isRotating = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                rotationPoint = point;
                totalRotation = 0;
            }

            if (Mouse.current.rightButton.isPressed && canRotate)
            {
                float delta = buildCamera.mouseSensivity * Mouse.current.delta.ReadValue().x;
                totalRotation += delta;

                buildCamera.transform.position = beforePosition;
                buildCamera.transform.rotation = beforeRotation;
                buildCamera.transform.RotateAround(rotationPoint, Vector3.up, totalRotation);
            }

            if (Mouse.current.rightButton.wasReleasedThisFrame && canRotate)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                canRotate = false;
                isRotating = false;
            }
        }

        bool GetHitPoint(out Vector3 point)
        {
            Vector2 middle = new Vector2(buildCamera.camera.pixelWidth / 2,
                buildCamera.camera.pixelHeight / 2);

            Ray ray = buildCamera.camera.ScreenPointToRay(middle);
            float rayLenght = 1000.0f;

            if (Physics.Raycast(ray, out RaycastHit hit, rayLenght, buildCamera.mask))
            {
                point = hit.point;
                return true;
            }

            point = new Vector3();
            return false;
        }


        BuildCamera buildCamera;
        Quaternion beforeRotation;
        Vector3 beforePosition;
        bool canRotate = false;
        bool isRotating = false;
        Vector3 rotationPoint;
        float totalRotation = 0;
    }
}
