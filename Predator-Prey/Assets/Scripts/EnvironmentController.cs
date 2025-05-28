using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
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
    public float preyTranslationSpeed     = 4f;
    public float preyRotationSpeed        = 2f;
    public bool placeRandomly = true;
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

    public float obstacleCollisionPenalty = -0.5f;

    public List<AgentInfo> agentsList = new List<AgentInfo>();

    private int predatorsCount = 0;
    private int preysCount = 0;

    private List<Agent> killedPreys = new List<Agent>();
    private int killedPreysCount = 0;

    public int maxEnvironmentSteps = 10000;
    public int resetTimer = 0;

    // Rewards
    public float soloCatchReward = 1f;
    public float teamCatchReward = 20f;
    public float catchRadius     = 12f;
    public float visionReward    = 0.06f;
    public float timeReward      = 0.02f;

    // Sensors
    public float sensorRayLength  = 30.0f;
    public float sensorHalfFOV    = 125.0f;
    public int raysPerDirection   = 30;

    // Smelling
    public bool smellingEnable  = false;
    public float smellingRadius = 9.0f;

    // Metrics
    private float interPredatorDistanceSum = 0f;
    private int interPredatorProximityCount = 0;
    private int totalCaptures = 0;
    private int loneWolfCaptures = 0;

    void Start() {
        Random.InitState(503);
        if (!inferenceEnable) {
            soloCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("solo_catch_reward", 1.0f);
            teamCatchReward = Academy.Instance.EnvironmentParameters.GetWithDefault("team_catch_reward", 20.0f);
            catchRadius     = Academy.Instance.EnvironmentParameters.GetWithDefault("catch_radius", 12.0f);
            visionReward    = Academy.Instance.EnvironmentParameters.GetWithDefault("vision_reward", 0.06f);

            smellingEnable  = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_enable", 0f) == 1f;
            smellingRadius  = Academy.Instance.EnvironmentParameters.GetWithDefault("smelling_radius", 3.0f);

            sensorRayLength = Academy.Instance.EnvironmentParameters.GetWithDefault("sensor_ray_length", 30.0f);
            sensorHalfFOV   = Academy.Instance.EnvironmentParameters.GetWithDefault("sensor_half_fov", 125.0f);

            maxEnvironmentSteps = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("max_environment_steps", 10000);

            Debug.Log("[EnvironmentController] Updating sensor objects");
            RayPerceptionSensorComponent3D[] sensors = GetComponentsInChildren<RayPerceptionSensorComponent3D>();
            foreach (var sensor in sensors) {
                sensor.RayLength         = sensorRayLength;
                sensor.MaxRayDegrees     = sensorHalfFOV;
                sensor.RaysPerDirection  = raysPerDirection;
            }
        } 

        Debug.Log("[EnvironmentController] soloCatchReward = " + soloCatchReward);
        Debug.Log("[EnvironmentController] teamCatchReward = " + teamCatchReward);
        Debug.Log("[EnvironmentController] catchRadius = " + catchRadius);
        Debug.Log("[EnvironmentController] visionReward = " + visionReward);
        Debug.Log("[EnvironmentController] smellingEnable = " + smellingEnable);
        Debug.Log("[EnvironmentController] smellingRadius = " + smellingRadius);
        Debug.Log("[EnvironmentController] sensorRayLength = " + sensorRayLength);
        Debug.Log("[EnvironmentController] sensorHalfFOV = " + sensorHalfFOV);
        Debug.Log("[EnvironmentController] raysPerDirection = " + raysPerDirection);
        Debug.Log("[EnvironmentController] maxEnvironmentSteps = " + maxEnvironmentSteps);


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
                item.agent.AddReward(-timeReward);
                // Vision reward: check if predator can see any prey
                if (PredatorCanSeePrey(item.agent.transform)) {
                    item.agent.AddReward(visionReward);
                }
            } else if (item.agent.CompareTag("Prey")) {
                item.agent.AddReward(timeReward);
                if (inferenceEnable) {
                    var preyAgent = (PreyAgent)item.agent;
                    preyAgent.survivedSteps += 1;
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
                    string line = $"[Episode {inferenceEpisode}] [timeout] " +
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

            foreach (var item in agentsList) {
                if (item.agent.CompareTag("Predator"))
                    item.agent.AddReward(-10.0f);
                item.agent.EndEpisode();
            }
            ResetScene();
        }
    }

    public void predatorObstacleCollision(PredatorAgent predator) {
        predator.AddReward(obstacleCollisionPenalty);
    }
    
    public void preyObstacleCollision(PreyAgent prey) {
        prey.AddReward(obstacleCollisionPenalty);
    }

    public void PredatorPreyCollision(PredatorAgent catcherPredator, Agent caughtPrey)
    {
        int participants = 0;
        foreach (var item in agentsList)
        {
            if (item.agent.CompareTag("Predator") && item.agent.gameObject.activeSelf)
            {
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
        if (participants == 1)
        {
            loneWolfCaptures += 1;
            catcherPredator.AddReward(soloCatchReward);
            preyReward = -soloCatchReward;
        }
        else
        {
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

        if (killedPreysCount == preysCount)
        {
            if (inferenceEnable)
            {
                int prey_survived_step = 0;
                float interPredatorDistance = -1f;
                foreach (var item in agentsList)
                {
                    if (item.agent.CompareTag("Prey"))
                    {
                        var preyAgent = (PreyAgent)item.agent;
                        prey_survived_step = preyAgent.survivedSteps;
                        preyAgent.survivedSteps = 0;
                    }
                    else if (item.agent.CompareTag("Predator") && item.agent != catcherPredator)
                    {
                        interPredatorDistance = Vector3.Distance(
                            catcherPredator.transform.position,
                            item.agent.transform.position
                        );
                    }
                }

                float avgPredatorDistance = interPredatorDistanceSum / resetTimer;
                float predatorProximityRate = (float)interPredatorProximityCount / resetTimer;

                using (StreamWriter writer = new StreamWriter(inferenceLogPath, true))
                {
                    float score = 2 - (float)((1 * loneWolfCaptures) + (2 * (totalCaptures - loneWolfCaptures))) / (float)totalCaptures;
                    string line = $"[Episode {inferenceEpisode}] [capture] " +
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
                if (inferenceEpisode >= inferenceMaxEpisode)
                {
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

    // Helper: check if a predator can see any prey within 60 units and 250° FOV
    private bool PredatorCanSeePrey(Transform predatorTransform) {
        float rayLength = sensorRayLength;
        float halfFov = sensorHalfFOV;
        foreach (var item in agentsList) {
            if (item.agent.CompareTag("Prey") && item.agent.gameObject.activeSelf) {
                Vector3 dirToPrey = item.agent.transform.position - predatorTransform.position;
                float distance = dirToPrey.magnitude;
                if (distance > rayLength) continue;
                float angle = Vector3.Angle(predatorTransform.forward, dirToPrey);
                if (angle > halfFov) continue;
                // Raycast to check for obstacles
                Ray ray = new Ray(predatorTransform.position, dirToPrey.normalized);
                if (Physics.Raycast(ray, out RaycastHit hit, rayLength, ~0)) {
                    if (hit.collider.gameObject == item.agent.gameObject) {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}