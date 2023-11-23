using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    // 꿀팁! SerializedField 한다고해서 전부 인스펙터 창에 표시되지는 않음
    [SerializeField] private float walkSpeed;
    // 카메라Y축 민감도
    [SerializeField] private float mouseYLookSensitivity;
    // 카메라X축 민감도
    [SerializeField] private float mouseXLookSensitivity;
    // 카메라 각도 제한
    [SerializeField] private float cameraRotationLimit;
    // 카메라 초기값
    private float currentCameraRotationX = 0;
    
    private Rigidbody myRigid; // 플레이어의 육체
    [SerializeField] private Camera theCamera; // 카메라
    private void Start()
    {
        myRigid = GetComponent<Rigidbody>();
        // 이렇게 카메라를 들고올 수도 있으나, 효율이 떨어지고, 카메라가 여러개 있을 수 있음
        // theCamera = FindObjectOfType<Camera>();
    }

    void Update()
    {
        Move();
        CameraRotation();
        CharacterRotation();
    }

    private void Move()
    {
        // 유니티에선 X가 좌우, Z가 정면과 뒤, Y가 높이임
        float _moveDirX = Input.GetAxisRaw("Horizontal"); // 오른쪽방향키 1, 왼쪽방향키 -1, 가만히 0
        float _moveDirZ = Input.GetAxisRaw("Vertical"); // 위쪽방향키 1, 아래쪽방향키 -1, 가만히 0

        // Vector3 는 float 값을 3개 가지는 변수임
        Vector3 _moveHorizontal = transform.right * _moveDirX;
        Vector3 _moveVertical = transform.forward * _moveDirZ;

        // 이동 2개를 더해 속도(velocity) 설정, normalized 시킴. 
        // normalized는 정규화시키는 작업
        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * walkSpeed;

        // deltaTime으로 속도를 쪼개줌.
        // (Update가 매프레임 실행되기 때문에) 1초동안 velocity를 움직이게 만들겠다는 뜻임.
        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);
    }

    // 캐릭터 좌우 회전, 카메라도 같이 돌아감
    private void CharacterRotation()
    {
        float _yRotation = Input.GetAxisRaw("Mouse X");
        // rotation에 민감도 추가
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * mouseXLookSensitivity;
        // Euler값을 Quaternion으로 변환, 곱해줌
        myRigid.MoveRotation(myRigid.rotation * quaternion.Euler(_characterRotationY));
    }
    
    // 캐릭터 위아래 Cam Rotation
    private void CameraRotation()
    {
        // 마우스는 2차원임
        float _xRotation = Input.GetAxisRaw("Mouse Y");
        // 카메라 위아래 민감도
        float _cameraRotationX = _xRotation * mouseYLookSensitivity;
        currentCameraRotationX += _cameraRotationX;
        // Mathf.clamp(한계를 정할 값, 최소값, 최대값)
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);
        
        // 오일러앵글? Rotation X,Y,Z임 걍. 
        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }
    
}
