using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public float preyTranslationSpeed     = 3.5f;
    public float preyRotationSpeed        = 1.5f;
    public bool placeRandomly = false;
    public float rnd_x_width = 2.5f;
    public float rnd_z_width = 2.5f;
    public float rotMin = 0f;
    public float rotMax = 360f;

    public bool inferenceEnable = false;
    public string inferenceName = "Predator-Prey";
    public string inferenceLogDir = "inference_logs";
    private string inferenceLogPath;
    public int inferenceEpisode = 0;
    public int inferenceMaxEpisode = 1000;

    public float obstacleCollisionPenalty = -0.2f;

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

    public bool smellingEnable     = false;
    public float smellingDecayRate = 0.95f;
    public float smellingRadius    = 3.0f;  // 3m radius around agent = (2 * 3 + 1) * (2 * 3 + 1) = 49 cells
    public float smellingCellSize  = 1.0f;  // 1m x 1m cells
    public float smellingIntensityPredator = 1f;
    public float smellingIntensityPrey     = 1f;
    public SmellMap smellMap;

    private float interPredatorDistanceSum = 0f;
    private int interPredatorProximityCount = 0;

    private int totalCaptures = 0;
    private int loneWolfCaptures = 0;

    void Start() {
        Random.InitState(503);
        if (!inferenceEnable)
        {
            soloCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("solo_catch_reward", 1f);
            teamCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("team_catch_reward", 1.5f);
            catchRadius = Academy.Instance.EnvironmentParameters.GetWithDefault("catch_radius", 15f);

            smellingEnable = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_enable", 0f) == 1f;
            smellingDecayRate = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_decay_rate", 0.95f);
            smellingRadius = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_radius", 3.0f);
            smellingCellSize = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_cell_size", 1.0f);
        }
        Debug.Log("[EnvironmentController] soloCatchReward = " + soloCatchReward);
        Debug.Log("[EnvironmentController] teamCatchReward = " + teamCatchReward);
        Debug.Log("[EnvironmentController] catchRadius = " + catchRadius);
        Debug.Log("[EnvironmentController] smellingEnable = " + smellingEnable);
        Debug.Log("[EnvironmentController] smellingDecayRate = " + smellingDecayRate);
        Debug.Log("[EnvironmentController] smellingRadius = " + smellingRadius);
        Debug.Log("[EnvironmentController] smellingCellSize = " + smellingCellSize);
        Debug.Log("[EnvironmentController] smellingIntensityPredator = " + smellingIntensityPredator);
        Debug.Log("[EnvironmentController] smellingIntensityPrey = " + smellingIntensityPrey);   

        spawnArea = spawnAreaObject.GetComponent<Collider>();

        if (smellingEnable) {
            smellMap = new SmellMap(spawnArea.bounds, smellingCellSize, smellingDecayRate);
            Debug.Log("[EnvironmentController] SmellMap initialized.");
        }
        
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
            inferenceLogPath = Path.Combine(inferenceLogDir, inferenceName + ".txt");
            using (var writer = File.CreateText(inferenceLogPath)) {
                writer.WriteLine($"[Inference] {inferenceName}");
            }
            Time.timeScale = 10f;
        }
        ResetScene();
    }

    void FixedUpdate() {
        foreach (var item in agentsList) {
            if (item.agent.CompareTag("Predator")) {
                item.agent.AddReward(-1f / maxEnvironmentSteps);
                if(smellingEnable) {
                    smellMap.AddSmell(item.agent.transform.position, smellingIntensityPredator);
                }
            } else if (item.agent.CompareTag("Prey")) {
                item.agent.AddReward(1f / maxEnvironmentSteps);
                if (inferenceEnable) {
                    var preyAgent = (PreyAgent)item.agent;
                    preyAgent.survivedSteps += 1;
                }
                if(smellingEnable) {
                    smellMap.AddSmell(item.agent.transform.position, smellingIntensityPrey);
                }
            }
        }

        foreach (var item in agentsList)
            if (item.agent.transform.position.y < -10)
                item.agent.transform.position = item.startingPosition;

        Agent predator1 = null;
        Agent predator2 = null;
        foreach (var item in agentsList) {
            if (item.agent.CompareTag("Predator")){
                if (predator1 == null) predator1 = item.agent;
                else predator2 = item.agent;
            }
        }

        if (predator1 != null && predator2 != null) {
            float dist = Vector3.Distance(predator1.transform.position, predator2.transform.position);
            interPredatorDistanceSum += dist;
            if (dist <= catchRadius) interPredatorProximityCount++;
        }

        resetTimer += 1;
        if (resetTimer >= maxEnvironmentSteps && maxEnvironmentSteps > 0) {
            if (inferenceEnable) {
                int prey_survived_step = 0;
                foreach (var item in agentsList) {
                    if (item.agent.CompareTag("Prey")) {   
                        var preyAgent = (PreyAgent)item.agent;
                        prey_survived_step = preyAgent.survivedSteps;
                        preyAgent.survivedSteps = 0;
                    }
                }

                float avgPredatorDistance = interPredatorDistanceSum / resetTimer;
                float predatorProximityRate = (float)interPredatorProximityCount / resetTimer;

                using (StreamWriter writer = new StreamWriter(inferenceLogPath, true)) {
                    float score = 2 - (float)((1 * loneWolfCaptures) + (2 * (totalCaptures - loneWolfCaptures))) / (float)totalCaptures;
                    string line = $"[Episode {inferenceEpisode}] [Timeout] " +
                        $"total_captures = {totalCaptures}, " +
                        $"lone_wolf_captures = {loneWolfCaptures}, " +
                        $"prey_survived_step = {prey_survived_step}, " + 
                        $"avg_predator_distance = {avgPredatorDistance}, " +
                        $"predator_proximity_rate = {predatorProximityRate}, " +
                        $"score = {score}";
                    writer.WriteLine(line);
                    Debug.Log(line);
                }

                inferenceEpisode += 1;
                if (inferenceEpisode >= inferenceMaxEpisode) {
                    Debug.Log("[EnvironmentController] Inference finished.");
                    #if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                }
            }

            foreach (var item in agentsList) 
                item.agent.EndEpisode();
            ResetScene();
        }
    }

    public void predatorObstacleCollision(PredatorAgent predator) {
        predator.AddReward(obstacleCollisionPenalty);
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

        totalCaptures += 1;
        Academy.Instance.StatsRecorder.Add(
            // one key per arena; drop gameObject.name if you prefer global
            $"lone_wolf_rate/{gameObject.name}",
            participants == 1 ? 1f : 0f,          // 1 = solo, 0 = team
            StatAggregationMethod.Average);       // ML‑Agents averages inside each
                                                // summary window (e.g. 1000 steps)

        float preyReward = 0f;
        if (participants == 1) {
            loneWolfCaptures += 1;
            catcherPredator.AddReward(soloCatchReward);
            preyReward = -soloCatchReward;
        } else {
            foreach (var item in agentsList)
                if (item.agent.CompareTag("Predator") &&
                    Vector3.Distance(
                        item.agent.transform.position,
                        caughtPrey.transform.position
                    ) <= catchRadius)
                    item.agent.AddReward(teamCatchReward);
            preyReward = -teamCatchReward;
        }

        caughtPrey.AddReward(preyReward);
        KillAgent(caughtPrey);

        if (killedPreysCount == preysCount) {
            if (inferenceEnable) {
                int prey_survived_step = 0;
                float interPredatorDistance = -1f;
                foreach (var item in agentsList) {
                    if (item.agent.CompareTag("Prey")) {   
                        var preyAgent = (PreyAgent)item.agent;
                        prey_survived_step = preyAgent.survivedSteps;
                        preyAgent.survivedSteps = 0;
                    }
                    else if (item.agent.CompareTag("Predator") && item.agent != catcherPredator) {
                        interPredatorDistance = Vector3.Distance(
                            catcherPredator.transform.position,
                            item.agent.transform.position
                        );
                    }
                }

                float avgPredatorDistance = interPredatorDistanceSum / resetTimer;
                float predatorProximityRate = (float)interPredatorProximityCount / resetTimer;

                using (StreamWriter writer = new StreamWriter(inferenceLogPath, true)) {
                    float score = 2 - (float)((1 * loneWolfCaptures) + (2 * (totalCaptures - loneWolfCaptures))) / (float)totalCaptures;
                   string line = $"[Episode {inferenceEpisode}] [Timeout] " +
                        $"total_captures = {totalCaptures}, " +
                        $"lone_wolf_captures = {loneWolfCaptures}, " +
                        $"prey_survived_step = {prey_survived_step}, " + 
                        $"predator_distance = {interPredatorDistance}, " +
                        $"avg_predator_distance = {avgPredatorDistance}, " +
                        $"predator_proximity_rate = {predatorProximityRate}, " +
                        $"score = {score}";
                    writer.WriteLine(line);
                    Debug.Log(line);
                }

                inferenceEpisode += 1;
                if (inferenceEpisode >= inferenceMaxEpisode) {
                    Debug.Log("[EnvironmentController] Inference finished.");
                    #if UNITY_EDITOR
                        EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                }
            }

            foreach (var item in agentsList)
                item.agent.EndEpisode();
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

        foreach (var item in agentsList) {
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

        interPredatorDistanceSum = 0f;
        interPredatorProximityCount = 0;
    }
}