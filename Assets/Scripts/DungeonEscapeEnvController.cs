using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class DungeonEscapeEnvController : MonoBehaviour
{
    [System.Serializable] public class PlayerInfo
    {
        public DungeonEscapeAgent Agent;
        [HideInInspector] public Vector3 StartingPos;
        [HideInInspector] public Quaternion StartingRot;
        [HideInInspector] public Rigidbody Rb;
        [HideInInspector] public Collider Col;
    }

    [System.Serializable] public class DragonInfo
    {
        public SimpleNPC Agent;
        [HideInInspector] public Vector3 StartingPos;
        [HideInInspector] public Quaternion StartingRot;
        [HideInInspector] public Rigidbody Rb;
        [HideInInspector] public Collider Col;
        public Transform T;
        public bool IsDead;
    }

    public GameObject ground;
    Renderer m_GroundRenderer;
    Material m_GroundMaterial;
    public Bounds areaBounds;
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    private SimpleMultiAgentGroup m_AgentGroup;
    private int m_NumberOfRemainingPlayers;
    public List<DragonInfo> DragonsList = new List<DragonInfo>();

    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;

    private DungeonEscapeSettings m_DungeonEscapeSettings;

    [Header("Max Environment Steps")] public int MaxEnvitonmentSteps = 25000;
    private int m_ResetTimer;

    public GameObject Key;
    public GameObject Tombstone;

    void Start()
    {
        areaBounds = ground.GetComponent<Collider>().bounds;
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_DungeonEscapeSettings = FindFirstObjectByType<DungeonEscapeSettings>();
        m_NumberOfRemainingPlayers = AgentsList.Count;
        m_AgentGroup = new SimpleMultiAgentGroup();

        Key.SetActive(false);
        
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();

            m_AgentGroup.RegisterAgent(item.Agent);
        }

        foreach (var item in DragonsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
            item.T = item.Agent.transform;
        }

        ResetScene();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer > MaxEnvitonmentSteps && MaxEnvitonmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_DungeonEscapeSettings.spawnAreaMarginMultiplier ,
            areaBounds.extents.x * m_DungeonEscapeSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_DungeonEscapeSettings.spawnAreaMarginMultiplier,
            areaBounds.extents.z * m_DungeonEscapeSettings.spawnAreaMarginMultiplier);

            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (!Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)))
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    private Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    public void UnlockDoor()
    {
        m_AgentGroup.AddGroupReward(1f);
        StartCoroutine(GoalScoredSwapGroundMaterial(m_DungeonEscapeSettings.goalScoredMaterial, 0.5f));

        print("Unlocked door");
        m_AgentGroup.EndGroupEpisode();
        ResetScene();
    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    public void KilledByBaddie(DungeonEscapeAgent agent, Collision baddieCol)
    {
        agent.gameObject.SetActive(false);
        m_NumberOfRemainingPlayers -= 1;

        baddieCol.gameObject.SetActive(false);
        print($"{baddieCol.gameObject.name} ate {agent.transform.name}");

        if (m_NumberOfRemainingPlayers == 0)
        {
            m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }

        // spawn tombstone
        Tombstone.transform.SetPositionAndRotation(agent.transform.position, agent.transform.rotation);
        Tombstone.SetActive(true);

        // activate the env's key at the location
        Key.transform.SetPositionAndRotation(baddieCol.collider.transform.position, baddieCol.collider.transform.rotation);
        Key.SetActive(true);
    }

    public void TouchedHazard(DungeonEscapeAgent agent)
    {
        m_NumberOfRemainingPlayers --;
        agent.gameObject.SetActive(false);
        if (m_NumberOfRemainingPlayers == 0 || agent.IHaveAKey)
        {
            m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }
    }

    public void BaddieTouchedHazard()
    {
        m_AgentGroup.EndGroupEpisode();
        StartCoroutine(GoalScoredSwapGroundMaterial(m_DungeonEscapeSettings.DragonEscapedMaterial, 0.5f));
        ResetScene();
    }


    void ResetScene()
    {
        m_ResetTimer = 0;
        m_NumberOfRemainingPlayers = AgentsList.Count;

        var randomMultiplier = Random.Range(0, 4);
        var rotationAngle = 90.0f * randomMultiplier;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.MyKey.SetActive(false);
            item.Agent.IHaveAKey = false;
            item.Agent.gameObject.SetActive(true);
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        Key.SetActive(false);
        Tombstone.SetActive(false);

        foreach (var item in DragonsList)
        {
            item.Agent.transform.SetPositionAndRotation(item.StartingPos, item.StartingRot);
            item.Agent.SetRandomWalkSpeed();
            item.Agent.gameObject.SetActive(true);
        }

    }
}
