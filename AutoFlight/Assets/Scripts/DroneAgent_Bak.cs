using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent_Bak : Agent
{
    private Transform tfAgent;
    private Rigidbody rbAgent;
    private Transform tfTarget;
    private Transform tfObstacles;

    private float addForce = 25.0f;
    private float rotSpeed = 10.0f;

    private float distAfter;
    private float distBefore;
    
    private RaycastHit rayHit;
    private float rayDistance = 15.0f;
    private float rayDiameter = 5.0f;

    private Renderer renderGround;
    private Renderer renderTarget;

    // 초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {
        MaxStep = 1000;
        tfAgent = GetComponent<Transform>();
        rbAgent = GetComponent<Rigidbody>();

        tfTarget = transform.parent.Find("Target").gameObject.GetComponent<Transform>();
        tfObstacles = transform.parent.Find("Obstacles").gameObject.GetComponent<Transform>();


        // 컬러렌더링, 테스트환경에서 삭제되는 코드
        renderGround = transform.parent.Find("Ground").gameObject.GetComponent<Renderer>();
        renderTarget = transform.parent.Find("Target").gameObject.GetComponent<Renderer>();
    }

    // 에피소드가 시작할 때마다 호출
    public override void OnEpisodeBegin()
    {
        // Agent 물리력 및 회전력 초기화
        rbAgent.velocity = Vector3.zero;
        rbAgent.angularVelocity = Vector3.zero;
        tfAgent.eulerAngles = Vector3.zero;

        // Agnet & Target 위치 랜덤생성
        tfAgent.localPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));
        tfTarget.localPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));

        // Obstacle 위치 랜덤생성
        int selectRotation = Random.Range(1, 4);
        for (int i = 0; i < tfObstacles.childCount; i++) {
            // 장애물이 수직일 때
            if (selectRotation == 1)
            {
                // Agent & Target과 겹침 방지
                while (true)
                {
                    Vector3 randomPosition = new Vector3(Random.Range(-50.0f, 50.0f), 50.0f, Random.Range(-50.0f, 50.0f));
                    float distance1 = Vector3.Distance(tfAgent.localPosition, randomPosition);
                    float distance2 = Vector3.Distance(tfTarget.localPosition, randomPosition);
                    if (distance1 > 10.0f && distance2 > 10.0f)
                    {
                        tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(0, 0, 0);
                        tfObstacles.GetChild(i).transform.localPosition = randomPosition;
                        break;
                    }
                }

            }
            // 장애물이 전면수평일 때
            if (selectRotation == 2)
            {
                // Agent & Target과 겹침 방지
                while (true)
                {
                    Vector3 randomPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), 0.0f);
                    float distance1 = Vector3.Distance(tfAgent.localPosition, randomPosition);
                    float distance2 = Vector3.Distance(tfTarget.localPosition, randomPosition);
                    if (distance1 > 10.0f && distance2 > 10.0f)
                    {
                        tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(90, 0, 0);
                        tfObstacles.GetChild(i).transform.localPosition = randomPosition;
                        break;
                    }
                }
            }
            // 장애물이 측면수평일 때
            if (selectRotation == 3)
            {
                // Agent & Target과 겹침 방지
                while (true)
                {
                    Vector3 randomPosition = new Vector3(0.0f, Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));
                    float distance1 = Vector3.Distance(tfAgent.localPosition, randomPosition);
                    float distance2 = Vector3.Distance(tfTarget.localPosition, randomPosition);
                    if (distance1 > 10.0f && distance2 > 10.0f)
                    {
                        tfObstacles.GetChild(i).transform.localEulerAngles = new Vector3(0, 0, 90);
                        tfObstacles.GetChild(i).transform.localPosition = randomPosition;
                        break;
                    }
                }
            }
        }

        // 보상값 계산을 위한 Agent와 Target간 방향벡터 및 초기거리
        distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);


        // 테스트환경에서 삭제되는 코드
        // Material 컬러 초기화, 육안확인을 위한 코루틴 딜레이, 
        StartCoroutine(RevertMaterial());
    }

    // 환경정보 관측 및 수집, 정책결정을 위한 브레인에 전달
    public override void CollectObservations(VectorSensor sensor)
    {
        // 총 관측값 12 + 6 = 18개
        sensor.AddObservation(tfAgent.localPosition);       // 관측값 3개(x, y, z), Agent Position
        sensor.AddObservation(tfTarget.localPosition);      // 관측값 3개(x, y, z), Target Position

        sensor.AddObservation(rbAgent.velocity);            // 관측값 3개(x, y, z), Agent Velocity
        sensor.AddObservation(rbAgent.angularVelocity);     // 관측값 3개(x, y, z), Agent Angular Velocity

        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.forward));      // 관측값 1개(float), Detected Obeject Distance for Forward
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.forward));     // 관측값 1개(float), Detected Obeject Distance for Backward
        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.right));        // 관측값 1개(float), Detected Obeject Distance for Right
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.right));       // 관측값 1개(float), Detected Obeject Distance for Left
        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.up));           // 관측값 1개(float), Detected Obeject Distance for Up
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.up));          // 관측값 1개(float), Detected Obeject Distance for Down
                
    }

    // 브레인(정책)으로부터 전달받은 행동을 실행하는 메소드
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 이동 행동 선택
        //float positionX = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float positionY = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float positionZ = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);
        float rotationY = Mathf.Clamp(actions.ContinuousActions[2], -1.0f, 1.0f);

        // 이동 벡터 결정
        Vector3 directionR = tfAgent.right * 0.0f + tfAgent.up * rotationY + tfAgent.forward * 0.0f;
        Vector3 directionP = tfAgent.right * 0.0f + tfAgent.up * positionY + tfAgent.forward * positionZ;
        //Vector3 directionP = Vector3.right * positionX + Vector3.up * positionY + Vector3.forward * positionZ;      // 월드좌표(월드기준)
        //Vector3 directionP = tfAgent.right * positionX + tfAgent.up * positionY + tfAgent.forward * positionZ;      // 로컬좌표(드론기준)

        // 이동 행동 수행
        rbAgent.AddForce(directionP.normalized * addForce);
        rbAgent.AddTorque(directionR.normalized * rotSpeed);


        // Lidar 센서측정
        //RayObservation(tfAgent.localPosition, tfAgent.forward);      // Detected Obeject Distance for Forward
        //RayObservation(tfAgent.localPosition, -tfAgent.forward);     // Detected Obeject Distance for Backward
        //RayObservation(tfAgent.localPosition, tfAgent.right);        // Detected Obeject Distance for Right
        //RayObservation(tfAgent.localPosition, -tfAgent.right);       // Detected Obeject Distance for Left
        //RayObservation(tfAgent.localPosition, tfAgent.up);           // Detected Obeject Distance for Up
        //RayObservation(tfAgent.localPosition, -tfAgent.up);          // Detected Obeject Distance for Down

        /*
        // Raycast Forward & Backward
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, tfAgent.forward, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, -tfAgent.forward, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }

        // Raycast Right & Left
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, tfAgent.right, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, -tfAgent.right, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }

        // Raycast Up & Down
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, tfAgent.up, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }
        if (Physics.SphereCast(tfAgent.position, rayDiameter / 2.0f, -tfAgent.up, out rayHit, rayDistance))
        {
            if (rayHit.collider.gameObject.name.Equals("Target"))
            {
                SetReward(1.0f - rayHit.distance / rayDistance);
            }
            else
            {
                SetReward(-1.0f + rayHit.distance / rayDistance);
            }
        }
        */

        // 이동 보상처리
        distAfter = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);        // 행동수행후 Agent, Target간 거리
        AddReward(distBefore - distAfter);
        distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);

        // 지속적인 행동선택을 위한 패널티
        AddReward(-0.01f);

    }

    // 개발자가 직접행동 테스트
    public override void Heuristic(in ActionBuffers actionsOut)
    {
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

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Equals("Target"))
        {
            GameObject.Find("LearningDirector").GetComponent<LearningDirector>().IncreaseSuccess();
            renderGround.material.color = Color.green;
            AddReward(10.0f);
            EndEpisode();
        }
        else
        {
            GameObject.Find("LearningDirector").GetComponent<LearningDirector>().IncreaseFailed();
            renderGround.material.color = Color.red;
            SetReward(-10.0f);
            EndEpisode();
        }
    }

    private float RayObservation(Vector3 position, Vector3 direction)
    {
        //Physics.SphereCast(position, rayDiameter / 2.0f, direction, out rayHit, rayDistance);
        //return rayHit.collider.gameObject.transform.localPosition;

        Physics.SphereCast(position, rayDiameter / 2.0f, direction, out rayHit, rayDistance);
        return rayHit.distance >= 0 ? rayHit.distance / rayDistance : -1.0f;

    }

    IEnumerator RevertMaterial()
    {
        yield return new WaitForSeconds(0.3f);  // 딜레이 시간

        // Floor & Target & Obstacle Rendering Initialize
        renderGround.material.color = Color.white;
        renderTarget.material.color = Color.blue;

    }

    void Start()
    {
        
    }

    
    void Update()
    {
        // 드론-목표지점 회전 및 Ray 표시
        //tfAgent.LookAt(new Vector3(tfTarget.position.x, tfAgent.position.y, tfTarget.position.z));
        Debug.DrawRay(tfAgent.position, (tfTarget.localPosition - tfAgent.localPosition), Color.black);

        /*
        // RayCast Lidar Drawing
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
        // Drawing SphereCast Ray
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
        Gizmos.DrawRay(transform.position, transform.forward * -rayDistance);
        Gizmos.DrawWireSphere(transform.position + transform.forward * rayDistance, rayDiameter / 2.0f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * -rayDistance, rayDiameter / 2.0f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * rayDistance);
        Gizmos.DrawRay(transform.position, transform.right * -rayDistance);
        Gizmos.DrawWireSphere(transform.position + transform.right * rayDistance, rayDiameter / 2.0f);
        Gizmos.DrawWireSphere(transform.position + transform.right * -rayDistance, rayDiameter / 2.0f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * rayDistance);
        Gizmos.DrawRay(transform.position, transform.up * -rayDistance);
        Gizmos.DrawWireSphere(transform.position + transform.up * rayDistance, rayDiameter / 2.0f);
        Gizmos.DrawWireSphere(transform.position + transform.up * -rayDistance, rayDiameter / 2.0f);
    }
}
