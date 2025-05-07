using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System.IO;

public class EnvironmentController : MonoBehaviour {
    [System.Serializable]
    public class AgentInfo {
        public Agent agent;
        [HideInInspector]
        public Vector3 startingPosition;
        [HideInInspector]
        public Quaternion startingRotation;
        [HideInInspector]
        public Rigidbody rigidBody;
    }

    public GameObject spawnAreaObject;
    public Collider spawnArea;
    public LayerMask obstacleMask; 

    [Header("Spawn Settings")]
    public int maxSpawnTries = 100;
    public float agentRadius = 0.5f;

    public float predatorTranslationSpeed = 5f;
    public float predatorRotationSpeed    = 2f;
    public float preyTranslationSpeed     = 4f;
    public float preyRotationSpeed        = 1.5f;
    public bool placeRandomly = false;
    public float rnd_x_width = 2.5f;
    public float rnd_z_width = 2.5f;
    public float rotMin = 0f;
    public float rotMax = 360f;
    public bool inferenceEnable = false;
    public string inferenceLogDir = "inference_logs";
    private string inferenceLogPath;

    public List<AgentInfo> agentsList = new List<AgentInfo>();

    private int predatorsCount = 0;
    private int preysCount = 0;

    private List<Agent> killedPreys = new List<Agent>();
    private int killedPreysCount = 0;

    public int maxEnvironmentSteps = 25000;
    private int resetTimer = 0;

    public float soloCatchReward = 1f;
    public float teamCatchReward = 1.5f;
    public float catchRadius     = 15f;


    private int totalCaptures = 0;
    private int loneWolfCaptures = 0;

    void Start() {
        soloCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("solo_catch_reward", 1f);
        teamCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("team_catch_reward", 1.5f);
        catchRadius = Academy.Instance.EnvironmentParameters.GetWithDefault("catch_radius", 15f);
        
        Debug.Log("[EnvironmentController] soloCatchReward = " + soloCatchReward);
        Debug.Log("[EnvironmentController] teamCatchReward = " + teamCatchReward);
        Debug.Log("[EnvironmentController] catchRadius = " + catchRadius);

        spawnArea = spawnAreaObject.GetComponent<Collider>();
        
        foreach (var item in agentsList) {
            item.startingPosition = item.agent.transform.position;
            item.startingRotation = item.agent.transform.rotation;
            item.rigidBody = item.agent.GetComponent<Rigidbody>();

            if (item.agent.CompareTag("Predator"))
                predatorsCount += 1;
            else if (item.agent.CompareTag("Prey"))
                preysCount += 1;
        }

        if (inferenceEnable) {
            Directory.CreateDirectory(inferenceLogDir);
            inferenceLogPath = Path.Combine(inferenceLogDir, gameObject.name + ".txt");
            if (!File.Exists(inferenceLogPath))
                File.CreateText(inferenceLogPath);
        }
        ResetScene();
    }

    void FixedUpdate() {
        foreach (var item in agentsList) {
            if (item.agent.CompareTag("Predator")) {
                item.agent.AddReward(-1f / maxEnvironmentSteps);
            } else if (item.agent.CompareTag("Prey")) {
                item.agent.AddReward(1f / maxEnvironmentSteps);
                if (inferenceEnable) {
                    var preyAgent = (PreyAgent)item.agent;
                    preyAgent.survivedSteps += 1;
                }
            }
        }

        foreach (var item in agentsList)
            if (item.agent.transform.position.y < -10)
                item.agent.transform.position = item.startingPosition;

        resetTimer += 1;
        if (resetTimer >= maxEnvironmentSteps && maxEnvironmentSteps > 0) {
            if (inferenceEnable) {
                foreach (var item in agentsList) {
                    if (item.agent.CompareTag("Prey")) {   
                        var preyAgent = (PreyAgent)item.agent;
                        preyAgent.survivedSteps = 0;
                    }
                }
            }

            foreach (var item in agentsList) {
                if (item.agent is PredatorAgent predatorAgent)
                    predatorAgent.LogMetrics();
                item.agent.EndEpisode();
            }
            ResetScene();
        }
    }

    public void PredatorPreyCollision(PredatorAgent catcherPredator, Agent caughtPrey) {
        int participants = 0;
        foreach (var item in agentsList) {
            if (item.agent.CompareTag("Predator") && item.agent.gameObject.activeSelf) {
                float distance = Vector3.Distance(
                    item.agent.transform.position,
                    caughtPrey.transform.position
                );
                if (distance <= catchRadius) participants++;
            }
        }
        Academy.Instance.StatsRecorder.Add(
            // one key per arena; drop gameObject.name if you prefer global
            $"lone_wolf_rate/{gameObject.name}",
            participants == 1 ? 1f : 0f,          // 1 = solo, 0 = team
            StatAggregationMethod.Average);       // ML‑Agents averages inside each
                                                // summary window (e.g. 1000 steps)

        float preyReward = 0f;
        if (participants == 1) {
            catcherPredator.soloCapturesCount += 1;
            catcherPredator.AddReward(soloCatchReward);
            preyReward = -soloCatchReward;
        } else {
            foreach (var item in agentsList) {
                if (item.agent.CompareTag("Predator") &&
                    Vector3.Distance(
                        item.agent.transform.position,
                        caughtPrey.transform.position
                    ) <= catchRadius) {
                    ((PredatorAgent)item.agent).teamCapturesCount += 1;
                    item.agent.AddReward(teamCatchReward);
                }
            }
            preyReward = -teamCatchReward;
        }

        caughtPrey.AddReward(preyReward);
        KillAgent(caughtPrey);

        if (killedPreysCount == preysCount) {
            foreach (var item in agentsList) {
                if (item.agent is PredatorAgent predatorAgent)
                    predatorAgent.LogMetrics();
                item.agent.EndEpisode();
            }
            ResetScene();
        }
    }

    public void KillAgent(Agent agent) {
        killedPreys.Add(agent);
        agent.EndEpisode();
        agent.gameObject.SetActive(false);
        killedPreysCount += 1;
    }

    private Vector3 SampleSafePosition() {
        var bounds = spawnArea.bounds;
        for (int i = 0; i < maxSpawnTries; i++) {
            // Tirage aléatoire dans le XZ du spawnArea
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);
            // On positionne légèrement au-dessus du sol
            Vector3 candidate = new Vector3(x, bounds.center.y + 1f, z);

            // Vérifie qu’aucun obstacle n’est dans un rayon agentRadius
            if (!Physics.CheckSphere(candidate, agentRadius, obstacleMask)) {
                return candidate;
            }
        }
        // Fallback : on renvoie le centre (au pire)
        return bounds.center + Vector3.up;
    }

    private void ResetScene() {
        resetTimer = 0;

        foreach (var item in agentsList)
        {
            Vector3 spawnPos = placeRandomly 
                ? SampleSafePosition() 
                : item.startingPosition;
            item.agent.transform.SetPositionAndRotation(
                spawnPos, 
                placeRandomly
                    ? Quaternion.Euler(0, Random.Range(rotMin, rotMax), 0)
                    : item.startingRotation
            );
        }

        foreach (var item in killedPreys)
            item.gameObject.SetActive(true);

        killedPreys = new List<Agent>();
        killedPreysCount = 0;
    }
}