using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DemoAgent : Agent
{
    private Transform tfAgent;              // Agent 위치정보
    private Rigidbody rbAgent;              // Agent 물리정보
    private Transform tfTarget;             // Target 위치정보
    // private Transform tfObstacles;          // 장애물 정보

    private float maxSpeed = 50.0f;         // 최대제한속도
    private float addForce = 25.0f;         // 행동 물리력

    private float distAfter;                // 이전 Step 거리
    private float distBefore;               // 현재 Step 거리
    
    private RaycastHit rayHit;              // Raycast 충돌정보
    private float rayAngle = 10.0f;         // LiDAR 전면 각도
    private float rayDistance = 10.0f;      // LiDAR 최대측정거리 상하좌우전
    private float rayDistance2 = 25.0f;     // LiDAR 최대측정거리 전면상하좌우

    //private Renderer renderGround;
    //private Renderer renderTarget;

    // 초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {
        MaxStep = 2000;
        tfAgent = GetComponent<Transform>();
        rbAgent = GetComponent<Rigidbody>();

        tfTarget = transform.parent.Find("Target").gameObject.GetComponent<Transform>();
        //tfObstacles = transform.parent.Find("Obstacles").gameObject.GetComponent<Transform>();

        // 컬러렌더링, 테스트환경에서 삭제되는 코드
        //renderGround = transform.parent.Find("Ground").gameObject.GetComponent<Renderer>();
        //renderTarget = transform.parent.Find("Target").gameObject.GetComponent<Renderer>();
    }

    // 에피소드가 시작할 때마다 호출
    public override void OnEpisodeBegin()
    {
        // Agent 물리력 및 회전력 초기화
        rbAgent.velocity = Vector3.zero;
        rbAgent.angularVelocity = Vector3.zero;
        tfAgent.eulerAngles = Vector3.zero;
        tfAgent.eulerAngles = new Vector3(0f, -90f, 0f);

        // 학습용 Agnet & Target 위치 초기화
        // tfAgent.localPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));
        // tfTarget.localPosition = new Vector3(0.0f, 50.0f, 1025.0f);

        // 테스트용 Agnet & Target 위치 초기화
        tfAgent.localPosition = new Vector3(225.0f, 0.0f, 0.0f);
        tfTarget.localPosition = new Vector3(Random.Range(-175f, 250f), Random.Range(25f, 100f), Random.Range(-50f, 200f));

        // 보상값 계산을 위한 Agent와 Target간 방향벡터 및 초기거리
        distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);

        /*
        // 학습용 Obstacle 위치 랜덤생성
        for (int i = 0; i < tfObstacles.childCount; i++)
        {
            int selectRotation = Random.Range(1, 4);
            while (true)
            {
                Vector3 randomPosition = new Vector3();
                if (selectRotation == 1)
                {
                    // 장애물이 수직일 때
                    tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(0, 0, 0);
                    randomPosition = new Vector3(Random.Range(-50.0f, 50.0f), 50.0f, Random.Range(0.0f, 900.0f));
                }
                if (selectRotation == 2)
                {
                    // 장애물이 전면수평일 때
                    tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(90, 0, 0);
                    randomPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(50.0f, 900.0f));
                }
                if (selectRotation == 3)
                {
                    // 장애물이 측면수평일 때
                    tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(0, 0, 90);
                    randomPosition = new Vector3(0.0f, Random.Range(0.0f, 100.0f), Random.Range(50.0f, 900.0f));
                }

                // Agent & Target과 겹침 방지
                float distance1 = Vector3.Distance(tfAgent.localPosition, randomPosition);
                float distance2 = Vector3.Distance(tfTarget.localPosition, randomPosition);
                if (distance1 > 10.0f && distance2 > 10.0f)
                {
                    tfObstacles.GetChild(i).transform.localPosition = randomPosition;
                    break;
                }
            }
        }
        */

        // 테스트환경에서 삭제되는 코드
        // Material 컬러 초기화, 육안확인을 위한 코루틴 딜레이, 
        //StartCoroutine(RevertMaterial());
    }

    // 환경정보 관측 및 수집, 정책결정을 위한 브레인에 전달
    public override void CollectObservations(VectorSensor sensor)
    {
        // 총 관측값 12 + 9 = 21개
        sensor.AddObservation(tfAgent.localPosition);       // 관측값 3개(x, y, z), Agent Position
        sensor.AddObservation(tfTarget.localPosition);      // 관측값 3개(x, y, z), Target Position

        sensor.AddObservation(rbAgent.velocity);            // 관측값 3개(x, y, z), Agent Velocity
        sensor.AddObservation(rbAgent.angularVelocity);     // 관측값 3개(x, y, z), Agent Angular Velocity

        sensor.AddObservation(RayObservation(-90f, 0f, 0f, rayDistance));          // 관측값 1개(float), Detected Obeject Distance for Upward
        sensor.AddObservation(RayObservation(90f, 0f, 0f, rayDistance));           // 관측값 1개(float), Detected Obeject Distance for Downward
        sensor.AddObservation(RayObservation(0f, -90f, 0f, rayDistance));          // 관측값 1개(float), Detected Obeject Distance for Leftward
        sensor.AddObservation(RayObservation(0f, 90f, 0f, rayDistance));           // 관측값 1개(float), Detected Obeject Distance for Rightward

        sensor.AddObservation(RayObservation(0f, 0f, 0f, rayDistance2));            // 관측값 1개(float), Detected Obeject Distance for Forward
        sensor.AddObservation(RayObservation(-rayAngle, 0f, 0f, rayDistance2));     // 관측값 1개(float), Detected Obeject Distance for ForwardU
        sensor.AddObservation(RayObservation(rayAngle, 0f, 0f, rayDistance2));      // 관측값 1개(float), Detected Obeject Distance for ForwardD
        sensor.AddObservation(RayObservation(0f, -rayAngle, 0f, rayDistance2));     // 관측값 1개(float), Detected Obeject Distance for ForwardL
        sensor.AddObservation(RayObservation(0f, rayAngle, 0f, rayDistance2));      // 관측값 1개(float), Detected Obeject Distance for ForwardR

    }

    // 브레인(정책)으로부터 전달받은 행동을 실행하는 메소드
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 이동 행동 선택
        float positionX = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float positionY = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);
        float positionZ = Mathf.Clamp(actions.ContinuousActions[2], -1.0f, 1.0f);
        //float rotationY = Mathf.Clamp(actions.ContinuousActions[2], -1.0f, 1.0f);

        // 이동 벡터 결정
        Vector3 directionP = Vector3.right * positionX + Vector3.up * positionY + Vector3.forward * positionZ;      // 월드좌표(월드기준)
        //Vector3 directionP = tfAgent.right * positionX + tfAgent.up * positionY + tfAgent.forward * positionZ;      // 로컬좌표(드론기준)

        // 이동 행동 수행(속도제한)
        //rbAgent.AddTorque(directionR.normalized * rotSpeed);
        if (rbAgent.velocity.magnitude < maxSpeed) rbAgent.AddForce(directionP.normalized * addForce);


        var eulerAngle1 = Quaternion.Euler(-90f, 0f, 0f);
        var eulerAngle2 = Quaternion.Euler(90f, 0f, 0f);
        var eulerAngle3 = Quaternion.Euler(0f, -90f, 0f);
        var eulerAngle4 = Quaternion.Euler(0f, 90f, 0f);

        var eulerAngle5 = Quaternion.Euler(0f, 0f, 0f);
        var eulerAngle6 = Quaternion.Euler(-rayAngle, 0f, 0f);
        var eulerAngle7 = Quaternion.Euler(rayAngle, 0f, 0f);
        var eulerAngle8 = Quaternion.Euler(0f, -rayAngle, 0f);
        var eulerAngle9 = Quaternion.Euler(0f, rayAngle, 0f);

        var direction1 = eulerAngle1 * tfAgent.forward;
        var direction2 = eulerAngle2 * tfAgent.forward;
        var direction3 = eulerAngle3 * tfAgent.forward;
        var direction4 = eulerAngle4 * tfAgent.forward;

        var direction5 = eulerAngle5 * tfAgent.forward;
        var direction6 = eulerAngle6 * tfAgent.forward;
        var direction7 = eulerAngle7 * tfAgent.forward;
        var direction8 = eulerAngle8 * tfAgent.forward;
        var direction9 = eulerAngle9 * tfAgent.forward;

        // 탐지된 장애물이 존재하는 경우
        if (RayObservation(-90f, 0f, 0f, rayDistance) != 0 ||
            RayObservation(90f, 0f, 0f, rayDistance) != 0 ||
            RayObservation(0f, -90f, 0f, rayDistance) != 0 ||
            RayObservation(0f, 90f, 0f, rayDistance) != 0 ||
            RayObservation(0f, 0f, 0f, rayDistance2) != 0 ||
            RayObservation(-rayAngle, 0f, 0f, rayDistance2) != 0 ||
            RayObservation(rayAngle, 0f, 0f, rayDistance2) != 0 ||
            RayObservation(0f, -rayAngle, 0f, rayDistance2) != 0 ||
            RayObservation(0f, rayAngle, 0f, rayDistance2) != 0)
        {
            // Detected Obeject Reward/Penalty for Upward
            if (Physics.Raycast(tfAgent.localPosition, direction1, out rayHit, rayDistance))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(-90f, 0f, 0f, rayDistance));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(-90f, 0f, 0f, rayDistance) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for Downward
            if (Physics.Raycast(tfAgent.localPosition, direction2, out rayHit, rayDistance))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(90f, 0f, 0f, rayDistance));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(90f, 0f, 0f, rayDistance) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for Leftward
            if (Physics.Raycast(tfAgent.localPosition, direction3, out rayHit, rayDistance))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(0f, -90f, 0f, rayDistance));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(0f, -90f, 0f, rayDistance) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for Rightward
            if (Physics.Raycast(tfAgent.localPosition, direction4, out rayHit, rayDistance))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(0f, 90f, 0f, rayDistance));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(0f, 90f, 0f, rayDistance) - 1f);
                }
            }

            // Detected Obeject Reward/Penalty for Forward
            if (Physics.Raycast(tfAgent.localPosition, direction5, out rayHit, rayDistance2))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(0f, 0f, 0f, rayDistance2));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(0f, 0f, 0f, rayDistance2) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for ForwardU
            if (Physics.Raycast(tfAgent.localPosition, direction6, out rayHit, rayDistance2))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(-rayAngle, 0f, 0f, rayDistance2));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(-rayAngle, 0f, 0f, rayDistance2) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for ForwardD
            if (Physics.Raycast(tfAgent.localPosition, direction7, out rayHit, rayDistance2))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(rayAngle, 0f, 0f, rayDistance2));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(rayAngle, 0f, 0f, rayDistance2) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for ForwardL
            if (Physics.Raycast(tfAgent.localPosition, direction8, out rayHit, rayDistance2))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(0f, -rayAngle, 0f, rayDistance2));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(0f, -rayAngle, 0f, rayDistance2) - 1f);
                }
            }
            // Detected Obeject Reward/Penalty for ForwardR
            if (Physics.Raycast(tfAgent.localPosition, direction9, out rayHit, rayDistance2))
            {
                if (rayHit.collider.gameObject.name.Equals("Target"))
                {
                    // 탐지된 객체가 목표지점인 경우 가까울수록 보상 증가
                    AddReward(1f - RayObservation(0f, rayAngle, 0f, rayDistance2));
                }
                else
                {
                    // 탐지된 객체가 장애물인 경우 가까울수록 페널티 증가
                    AddReward(RayObservation(0f, rayAngle, 0f, rayDistance2) - 1f);
                }
            }
        }
        // 탐지된 장애물이 존재하지 않는 경우
        else
        {
            distAfter = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);        // 행동수행후 Agent, Target간 거리
            AddReward((distBefore - distAfter) * 10f);                                          // 이전 Step과 거리의 차 = 목표지점으로 이동한 거리
            distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);       // 다음 Step을 위한 Agent, Target간 거리 업데이트
        }

        /*
        // 개발자 확인용 Debug 메세지, RayCast LiDAR 측정 확인
        Debug.Log("Up Distance = " + RayObservation(-90f, 0f, 0f, rayDistance) * rayDistance);
        Debug.Log("Down Distance = " + RayObservation(90f, 0f, 0f, rayDistance) * rayDistance);
        Debug.Log("Left Distance = " + RayObservation(0f, -90f, 0f, rayDistance) * rayDistance);
        Debug.Log("Right Distance = " + RayObservation(0f, 90f, 0f, rayDistance) * rayDistance);

        Debug.Log("Forward Distance = " + RayObservation(0f, 0f, 0f, rayDistance2) * rayDistance);
        Debug.Log("ForwardUp Distance = " + RayObservation(-rayAngle, 0f, 0f, rayDistance2) * rayDistance);
        Debug.Log("ForwardDown Distance = " + RayObservation(rayAngle, 0f, 0f, rayDistance2) * rayDistance);
        Debug.Log("ForwardLeft Distance = " + RayObservation(0f, -rayAngle, 0f, rayDistance2) * rayDistance);
        Debug.Log("ForwardRight Distance = " + RayObservation(0f, rayAngle, 0f, rayDistance2) * rayDistance);
        */
        
    }

    // 개발자가 직접행동 테스트
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        /*
        // Heuristic user Control Actions
        var ContinousActionsOut = actionsOut.ContinuousActions;

        // Heuristic User Control PositionX(Left Right)
        if (Input.GetKey(KeyCode.D))
            ContinousActionsOut[0] = 1.0f;
        if (Input.GetKey(KeyCode.A))
            ContinousActionsOut[0] = -1.0f;

        // Heuristic User Control PositionY(Up Down)
        if (Input.GetKey(KeyCode.E))
            ContinousActionsOut[1] = 1.0f;
        if (Input.GetKey(KeyCode.Q))
            ContinousActionsOut[1] = -1.0f;

        // Heuristic User Control PositionZ(Forth Back)
        if (Input.GetKey(KeyCode.W))
            ContinousActionsOut[2] = 1.0f;
        if (Input.GetKey(KeyCode.S))
            ContinousActionsOut[2] = -1.0f;


        // Heuristic User Control RotationX(Forth Back)
        if (Input.GetKey(KeyCode.Keypad8))
            ContinousActionsOut[3] = 1.0f;
        if (Input.GetKey(KeyCode.Keypad5))
            ContinousActionsOut[3] = -1.0f;

        // Heuristic User Control RotationY(Left Right)
        if (Input.GetKey(KeyCode.Keypad6))
            ContinousActionsOut[4] = 1.0f;
        if (Input.GetKey(KeyCode.Keypad4))
            ContinousActionsOut[4] = -1.0f;

        // Heuristic User Control RotationZ(Left Right)
        if (Input.GetKey(KeyCode.Keypad7))
            ContinousActionsOut[5] = 1.0f;
        if (Input.GetKey(KeyCode.Keypad9))
            ContinousActionsOut[5] = -1.0f;
        */

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Equals("Target"))
        {
            // 학습용 개발자 모니터링을 위한 UI 및 바닥컬러 변경
            //renderGround.material.color = Color.green;
            //GameObject.Find("LearningDirector").GetComponent<LearningDirector>().IncreaseSuccess();

            // 데모용 개발자 모니터링을 위한 UI
            GameObject.Find("TestDirector").GetComponent<TestDirector>().IncreaseSuccess();
            AddReward(10f);
            EndEpisode();
        }
        else
        {
            // 학습용 개발자 모니터링을 위한 UI 및 바닥컬러 변경
            //renderGround.material.color = Color.red;
            //GameObject.Find("LearningDirector").GetComponent<LearningDirector>().IncreaseFailed();

            // 데모용 개발자 모니터링을 위한 UI
            GameObject.Find("TestDirector").GetComponent<TestDirector>().IncreaseFailed();
            SetReward(-10f);
            EndEpisode();
        }
    }

    private float RayObservation(float angleX, float angleY, float angleZ, float limitDistance)
    {
        // RayCast LiDAR를 측정할 방향 결정
        var eulerAngle = Quaternion.Euler(angleX, angleY, angleZ);
        var direction = eulerAngle * tfAgent.forward;

        // RayCast LiDAR를 측정, 0-1 범위로 정규화
        Physics.Raycast(tfAgent.localPosition, direction, out rayHit, limitDistance);
        return rayHit.distance >= 0f ? rayHit.distance / limitDistance : -1f;
    }

    /*
    // 학습용 바닥 컬러 변경 코루틴
    IEnumerator RevertMaterial()
    {
        yield return new WaitForSeconds(0.3f);  // 딜레이 시간

        // Floor & Target & Obstacle Rendering Initialize
        renderGround.material.color = Color.white;
        renderTarget.material.color = Color.blue;
    }
    */

    void Start()
    {

    }

    
    void Update()
    {
        // 드론-목표지점 회전 및 Ray 표시
        tfAgent.LookAt(new Vector3(tfTarget.position.x, tfAgent.position.y, tfTarget.position.z));
        Debug.DrawRay(tfAgent.position, (tfTarget.localPosition - tfAgent.localPosition), Color.black);
        tfAgent.localPosition = Vector3.MoveTowards(tfAgent.localPosition, tfTarget.localPosition, 1f);
        if (tfAgent.localPosition.x < tfTarget.localPosition.x)
        {
            rbAgent.AddForce((tfTarget.localPosition - tfAgent.localPosition).normalized * addForce * 3);
            if (tfAgent.localPosition.y < tfTarget.localPosition.y)
                rbAgent.AddForce(Vector3.up * addForce * 3);
        }

        /*
        // RayCast Lidar Drawing for Developer
        Debug.DrawRay(tfAgent.position, tfAgent.forward * rayDistance, Color.blue);
        Debug.DrawRay(tfAgent.position, tfAgent.forward * -rayDistance, Color.blue);
        Debug.DrawRay(tfAgent.position, tfAgent.right * rayDistance, Color.red);
        Debug.DrawRay(tfAgent.position, tfAgent.right * -rayDistance, Color.red);
        Debug.DrawRay(tfAgent.position, tfAgent.up * rayDistance, Color.green);
        Debug.DrawRay(tfAgent.position, tfAgent.up * -rayDistance, Color.green);
        */
    }

    private void OnDrawGizmos()
    {   
        // Drawing RayCast Ray
        Gizmos.color = Color.green;
        var eulerAngleU = Quaternion.Euler(rayAngle, 0f, 0f) * transform.forward;
        var eulerAngleD = Quaternion.Euler(-rayAngle, 0f, 0f) * transform.forward;
        Gizmos.DrawRay(transform.position, transform.up * rayDistance);                 // Up
        Gizmos.DrawRay(transform.position, -transform.up * rayDistance);                // Down
        Gizmos.DrawRay(transform.position, eulerAngleU * rayDistance2);                 // ForwardUp
        Gizmos.DrawRay(transform.position, eulerAngleD * rayDistance2);                 // ForwardDown

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance2);           // Forward
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + transform.forward, rayDistance);     // LiDAR Measurment Area

        Gizmos.color = Color.red;
        var eulerAngleL = Quaternion.Euler(0f, -rayAngle, 0f) * transform.forward;
        var eulerAngleR = Quaternion.Euler(0f, rayAngle, 0f) * transform.forward;
        Gizmos.DrawRay(transform.position, -transform.right * rayDistance);             // Left
        Gizmos.DrawRay(transform.position, transform.right * rayDistance);              // Right
        Gizmos.DrawRay(transform.position, eulerAngleL * rayDistance2);                 // ForwardLeft
        Gizmos.DrawRay(transform.position, eulerAngleR * rayDistance2);                 // ForwardRight

    }
}
