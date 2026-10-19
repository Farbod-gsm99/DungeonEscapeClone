using System.Numerics;
using UnityEngine;

public class DungeonEscapeAgent : Agent
{

    private Rigidbody m_AgentRb;
    private DungeonEscapeSettings m_DungeonEscapeSettings;
    private bool IHaveAKey;
    public GameObject MyKey;
    public DungeonEscapeEnvController m_GameController;
    public override void Initialize()
    {
        // Assign variables
        m_GameController = GetComponentInParent<DungeonEscapeEnvController>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_DungeonEscapeSettings = FindFirstObjectByType<m_DungeonEscapeSettings>();
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    public override void OnEpisodeBegin()
    {
        MyKey.SetActive(false);
        IHaveAKey = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAKey);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscereteActions);
    }

    public override void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var action = act[0];

        switch (action)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            case 3:
                rotateDir = transform.up * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                break;
            case 5:
                dirToGo = transform.right * -0.75f;
                break;
            case 6:
                dirToGo = transform.right * 0.75f;
                break;
        }

        transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * m_DungeonEscapeSettings.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.transform.CompareTag("dragon"))
        {
            m_GameController.KilledByBaddie(this, col);
            MyKey.SetActive(false);
            IHaveAKey = false;
        }

        if (col.transform.CompareTag("lock"))
        {
            if (IHaveAKey)
            {
                MyKey.SetActive(false);
                IHaveAKey = false;                
                m_GameController.UnlockDoor();
            }
        }
        if (col.transform.CompareTag("portal"))
        {
            m_GameController.TouchedHazard(this);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.transform.CompareTag("key") && col.transform.parent == transform.parent && gameObject.activeInHierarchy)
        {
            print("Picked up the key");
            IHaveAKey = true;
            MyKey.SetActive(true);
            col.gameObject.SetActive(false);
        }
    }

}
