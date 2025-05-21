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
        sensor.AddObservation(environmentController.resetTimer / environmentController.maxEnvironmentSteps);
        
        if (!environmentController.smellingEnable) return;

        foreach (var item in environmentController.agentsList) {
            if (item.agent.CompareTag("Prey")) {
                float distance = Vector3.Distance(item.agent.transform.position, transform.position);
                Vector3 preyDirection;
                if (distance < environmentController.smellingRadius)
                    preyDirection = (item.agent.transform.position - transform.position).normalized;
                else
                    preyDirection = Vector3.zero;
                sensor.AddObservation(preyDirection);
            }

            if (item.agent.CompareTag("Predator") && item.agent != this) {
                float distance = Vector3.Distance(item.agent.transform.position, transform.position);
                Vector3 predatorDirection;
                if (distance < environmentController.smellingRadius)
                    predatorDirection = (item.agent.transform.position - transform.position).normalized;
                else
                    predatorDirection = Vector3.zero;
                sensor.AddObservation(predatorDirection);
            }
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