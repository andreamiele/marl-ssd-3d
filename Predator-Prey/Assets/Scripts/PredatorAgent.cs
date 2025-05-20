using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PredatorAgent : Agent {
    private EnvironmentController environmentController;

    public void Start() {
        environmentController = GetComponentInParent<EnvironmentController>();
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float speedForward = environmentController.predatorTranslationSpeed * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rotateY = environmentController.predatorRotationSpeed * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        transform.position += transform.forward * speedForward * Time.deltaTime;
        transform.Rotate(0f, rotateY, 0f);
    }

    public override void CollectObservations(VectorSensor sensor) {
        if (!environmentController.smellingEnable) return;

        SmellMap smellMap = environmentController.smellMap;
        float smellingRadius = environmentController.smellingRadius;

        List<float> smellValues = smellMap.GetSmellRadius(transform.position, smellingRadius);

        for (int i = 0; i < smellValues.Count; i++) {
            sensor.AddObservation(smellValues[i]);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Vertical");
        continuousActions[1] = Input.GetAxisRaw("Horizontal");
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Prey")) {
            Agent preyAgent = other.gameObject.GetComponent<Agent>();
            environmentController.PredatorPreyCollision(this, preyAgent);
        } else if (other.gameObject.CompareTag("Obstacle")) {
            environmentController.predatorObstacleCollision(this);
        }
    }
}