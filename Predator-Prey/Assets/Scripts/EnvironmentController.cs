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

    public float predatorTranslationSpeed = 5f;
    public float predatorRotationSpeed    = 2f;
    public float preyTranslationSpeed     = 4f;
    public float preyRotationSpeed        = 1.5f;
    
    public bool inferenceEnable = false;
    public string inferenceLogDir = "inference_logs";
    private string inferenceLogPath;

    public List<AgentInfo> agentsList = new List<AgentInfo>();

    private int predatorsCount = 0;
    private int preysCount = 0;

    private List<Agent> killedPreys = new List<Agent>();
    private int killedPreysCount = 0;

    public int maxEnvironmentSteps = 100;
    private int resetTimer = 0;

    public float soloCatchReward = 1f;
    public float teamCatchReward = 1.5f;
    public float catchRadius     = 15f;

    void Start() {
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

        // Debug.Log("FixedUpdate is running : " + resetTimer);

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
                if (item.agent.CompareTag("Predator")) {
                    ((PredatorAgent)item.agent).LogMetrics();
                }
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

    private void ResetScene() {
        resetTimer = 0;

        foreach (var item in agentsList) {
            var startingPosition = item.startingPosition;
            var startingRotation = item.startingRotation;
            item.agent.transform.SetPositionAndRotation(startingPosition, startingRotation);
        }

        foreach (var item in killedPreys)
            item.gameObject.SetActive(true);

        killedPreys = new List<Agent>();
        killedPreysCount = 0;
    }
}