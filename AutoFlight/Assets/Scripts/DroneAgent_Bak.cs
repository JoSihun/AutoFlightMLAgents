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

    // �ʱ�ȭ �۾��� ���� �ѹ� ȣ��Ǵ� �޼ҵ�
    public override void Initialize()
    {
        MaxStep = 1000;
        tfAgent = GetComponent<Transform>();
        rbAgent = GetComponent<Rigidbody>();

        tfTarget = transform.parent.Find("Target").gameObject.GetComponent<Transform>();
        tfObstacles = transform.parent.Find("Obstacles").gameObject.GetComponent<Transform>();


        // �÷�������, �׽�Ʈȯ�濡�� �����Ǵ� �ڵ�
        renderGround = transform.parent.Find("Ground").gameObject.GetComponent<Renderer>();
        renderTarget = transform.parent.Find("Target").gameObject.GetComponent<Renderer>();
    }

    // ���Ǽҵ尡 ������ ������ ȣ��
    public override void OnEpisodeBegin()
    {
        // Agent ������ �� ȸ���� �ʱ�ȭ
        rbAgent.velocity = Vector3.zero;
        rbAgent.angularVelocity = Vector3.zero;
        tfAgent.eulerAngles = Vector3.zero;

        // Agnet & Target ��ġ ��������
        tfAgent.localPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));
        tfTarget.localPosition = new Vector3(Random.Range(-50.0f, 50.0f), Random.Range(0.0f, 100.0f), Random.Range(-50.0f, 50.0f));

        // Obstacle ��ġ ��������
        int selectRotation = Random.Range(1, 4);
        for (int i = 0; i < tfObstacles.childCount; i++) {
            // ��ֹ��� ������ ��
            if (selectRotation == 1)
            {
                // Agent & Target�� ��ħ ����
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
            // ��ֹ��� ��������� ��
            if (selectRotation == 2)
            {
                // Agent & Target�� ��ħ ����
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
            // ��ֹ��� ��������� ��
            if (selectRotation == 3)
            {
                // Agent & Target�� ��ħ ����
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

        // ���� ����� ���� Agent�� Target�� ���⺤�� �� �ʱ�Ÿ�
        distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);


        // �׽�Ʈȯ�濡�� �����Ǵ� �ڵ�
        // Material �÷� �ʱ�ȭ, ����Ȯ���� ���� �ڷ�ƾ ������, 
        StartCoroutine(RevertMaterial());
    }

    // ȯ������ ���� �� ����, ��å������ ���� �극�ο� ����
    public override void CollectObservations(VectorSensor sensor)
    {
        // �� ������ 12 + 6 = 18��
        sensor.AddObservation(tfAgent.localPosition);       // ������ 3��(x, y, z), Agent Position
        sensor.AddObservation(tfTarget.localPosition);      // ������ 3��(x, y, z), Target Position

        sensor.AddObservation(rbAgent.velocity);            // ������ 3��(x, y, z), Agent Velocity
        sensor.AddObservation(rbAgent.angularVelocity);     // ������ 3��(x, y, z), Agent Angular Velocity

        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.forward));      // ������ 1��(float), Detected Obeject Distance for Forward
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.forward));     // ������ 1��(float), Detected Obeject Distance for Backward
        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.right));        // ������ 1��(float), Detected Obeject Distance for Right
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.right));       // ������ 1��(float), Detected Obeject Distance for Left
        sensor.AddObservation(RayObservation(tfAgent.localPosition, tfAgent.up));           // ������ 1��(float), Detected Obeject Distance for Up
        sensor.AddObservation(RayObservation(tfAgent.localPosition, -tfAgent.up));          // ������ 1��(float), Detected Obeject Distance for Down
                
    }

    // �극��(��å)���κ��� ���޹��� �ൿ�� �����ϴ� �޼ҵ�
    public override void OnActionReceived(ActionBuffers actions)
    {
        // �̵� �ൿ ����
        //float positionX = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float positionY = Mathf.Clamp(actions.ContinuousActions[0], -1.0f, 1.0f);
        float positionZ = Mathf.Clamp(actions.ContinuousActions[1], -1.0f, 1.0f);
        float rotationY = Mathf.Clamp(actions.ContinuousActions[2], -1.0f, 1.0f);

        // �̵� ���� ����
        Vector3 directionR = tfAgent.right * 0.0f + tfAgent.up * rotationY + tfAgent.forward * 0.0f;
        Vector3 directionP = tfAgent.right * 0.0f + tfAgent.up * positionY + tfAgent.forward * positionZ;
        //Vector3 directionP = Vector3.right * positionX + Vector3.up * positionY + Vector3.forward * positionZ;      // ������ǥ(�������)
        //Vector3 directionP = tfAgent.right * positionX + tfAgent.up * positionY + tfAgent.forward * positionZ;      // ������ǥ(��б���)

        // �̵� �ൿ ����
        rbAgent.AddForce(directionP.normalized * addForce);
        rbAgent.AddTorque(directionR.normalized * rotSpeed);


        // Lidar ��������
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

        // �̵� ����ó��
        distAfter = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);        // �ൿ������ Agent, Target�� �Ÿ�
        AddReward(distBefore - distAfter);
        distBefore = Vector3.Distance(tfAgent.localPosition, tfTarget.localPosition);

        // �������� �ൿ������ ���� �г�Ƽ
        AddReward(-0.01f);

    }

    // �����ڰ� �����ൿ �׽�Ʈ
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
        yield return new WaitForSeconds(0.3f);  // ������ �ð�

        // Floor & Target & Obstacle Rendering Initialize
        renderGround.material.color = Color.white;
        renderTarget.material.color = Color.blue;

    }

    void Start()
    {
        
    }

    
    void Update()
    {
        // ���-��ǥ���� ȸ�� �� Ray ǥ��
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
