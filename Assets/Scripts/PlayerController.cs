using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    // 꿀팁! SerializedField 한다고해서 전부 인스펙터 창에 표시되지는 않음
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;
    private float applySpeed;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float jumpForce;

    // 카메라 민감도
    [SerializeField] private float mouseYLookSensitivity;
    [SerializeField] private float mouseXLookSensitivity;
    
    // 카메라 각도 제한
    [SerializeField] private float cameraRotationLimit;
    private float currentCameraRotationX = 0;
    
    // 상태 변수
    private bool isRun = false;
    private bool isGround = true;
    private bool isCrouch = false;
    
    // 앉았을 때 얼마나 앉을지 결정하는 변수
    [SerializeField] private float crouchPosY;
    private float originPosY;
    private float applyCrouchPosY; 
    
    // 필요한 컴포넌트
    private Rigidbody myRigid; // 플레이어의 육체
    private CapsuleCollider capsuleCollider; // 땅 착지 여부 체크를 위한 콜라이더
    [SerializeField] private Camera theCamera; // 카메라
    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        applySpeed = walkSpeed;
        myRigid = GetComponent<Rigidbody>();
        
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;
    }

    void Update()
    {
        IsGround();
        TryJump();
        TryRun();
        TryCrouch();
        Move();
        CameraRotation();
        CharacterRotation();
    }
    
    // 캐릭터가 바닥에 닿아있는지 체크
    private void IsGround()
    {
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);
    }

    // 앉기 실행
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }

    // 앉기 로직
    private void Crouch()
    {
        isCrouch = !isCrouch;

        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }

        StartCoroutine(CrouchCoroutine());
    }

    // 앉을 때 시야 부드럽게 하기
    IEnumerator CrouchCoroutine()
    {
        float _posY = theCamera.transform.localPosition.y;
        int count = 0; // 보간의 단점(무한루프)을 보완하기 위한 count 변수
        while (_posY != applyCrouchPosY)
        {
            count++;
            // 보간의 단점 : 정수로 딱 떨어지지 않음
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.3f);
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            if (count > 30)
                break;
            yield return null;
        }

        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
    }
    
    // 점프 시도
    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround)
        {
            Jump();
        }
    }

    // 점프 로직
    private void Jump()
    {
        if(isCrouch)
            Crouch();
        
        myRigid.velocity = transform.up * jumpForce;
    }
    
    // 달리기 제어 
    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isGround)
        {
            Running();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            RunningCancel();
        }
    }
    
    // 달리기 입력
    private void Running()
    {
        if (isCrouch)
            Crouch();
        isRun = true;
        applySpeed = runSpeed;
    }

    // 달리기 취소
    private void RunningCancel()
    {
        isRun = false;
        applySpeed = walkSpeed;
    }

    // 움직임 제어
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
        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed;

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
        currentCameraRotationX -= _cameraRotationX;
        // Mathf.clamp(한계를 정할 값, 최소값, 최대값)
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit);
        
        // 오일러앵글? Rotation X,Y,Z임 걍. 
        theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);
    }
    
}
