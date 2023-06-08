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


public class BuildCamera : MonoBehaviour
{
    struct Input
    {
        public bool desireLeft;
        public bool desireRight;
        public bool desireForward;
        public bool desireBack;

        public void Reset()
        {
            this = new Input();
        }
    }

    public float mouseSensivity = 0.01f;
    public float scrollSensivity = 0.001f;
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

    Transform anchor;
    Rotator rotator;
    Input input;

    public void CenterAt(Transform obj)
    {
        anchor = obj;
        CameraSettings settings = CameraSettings.Lerp(min, max, settingsLerp);
        float beta = Mathf.Deg2Rad * (90.0f -  transform.rotation.eulerAngles.x);
        float c = settings.height / Mathf.Cos(beta);
        Vector3 forward = transform.forward;
        transform.position = obj.transform.position - forward * c;
    }

    private void OnDrawGizmos()
    {
        rotator.OnDrawGizmos();
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
        // if (Keyboard.current[Key.D].isPressed)
        Vector3 dir = new();
        if(input.desireRight)
            dir += transform.right;
        if (input.desireLeft)
            dir -= transform.right;
        if (input.desireForward)
            dir += transform.forward;
        if (input.desireBack)
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
        if(anchor)
            pos.y = anchor.position.y + settings.height;
        else
            pos.y = settings.height;

        Vector3 rotation = transform.localRotation.eulerAngles;
        rotation.x = settings.rotation;
        transform.localRotation = Quaternion.Euler(rotation);
        speed = settings.movementSpeed;
        transform.position = pos;
    }

    public void OnFocusGain() { }

    public void OnFocusLost() { }

    public void OnMoveRight(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.desireRight = true;
        if(context.canceled)
            input.desireRight = false;
    }

    public void OnMoveLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.desireLeft = true;
        if (context.canceled)
            input.desireLeft = false;
    }

    public void OnMoveForward(InputAction.CallbackContext context)
    {
        if(context.performed)
            input.desireForward = true;
        if (context.canceled)
            input.desireForward = false;
    }

    public void OnMoveBack(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.desireBack = true;
        if (context.canceled)
            input.desireBack = false;
    }

    public void OnRotateClick(InputAction.CallbackContext context)
    {
        if (context.started)
            rotator.OnRotateStart();

        if (context.canceled)
            rotator.OnRotateEnd();
    }

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
            if(startedRotation)
            {
                passedRotateDelay += Time.deltaTime;
                if (passedRotateDelay < rotateDelay)
                    return;

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

            if (isRotating)
            {
                float delta = buildCamera.mouseSensivity * Mouse.current.delta.ReadValue().x;
                totalRotation += delta;

                buildCamera.transform.position = beforePosition;
                buildCamera.transform.rotation = beforeRotation;
                buildCamera.transform.RotateAround(rotationPoint, Vector3.up, totalRotation);
            }

        }

        public void OnDrawGizmos()
        {
            GetHitPoint(out Vector3 point);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(buildCamera.transform.position, point);
        }

        public void OnRotateStart()
        {
            startedRotation = true;
            passedRotateDelay = 0;
        }

        public void OnRotateEnd()
        {
            startedRotation = false;

            if (isRotating)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isRotating = false;
            }
        }

        private bool GetHitPoint(out Vector3 point)
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


        bool startedRotation = false;
        float rotateDelay = 1.0f;
        float passedRotateDelay = 0;
        readonly BuildCamera buildCamera;
        Quaternion beforeRotation;
        Vector3 beforePosition;
        bool canRotate = false;
        bool isRotating = false;
        Vector3 rotationPoint;
        float totalRotation = 0;
    }
}
