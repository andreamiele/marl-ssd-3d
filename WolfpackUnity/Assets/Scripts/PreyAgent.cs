using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class PreyAgent : Agent {
    private string currentState;
    internal int survivedSteps = 0;

    public bool selfplay = false;
    private EnvironmentController environmentController;

    private List<Transform> predatorTransforms = new List<Transform>();
    private bool useHeuristic = false;
    private static int globalStepCount = 0;
    public int HeuristicStepThreshold = 1_010_000; // Steps after which heuristic is used

    public void Start() {
        environmentController = GetComponentInParent<EnvironmentController>();
        GameObject[] predators = GameObject.FindGameObjectsWithTag("Predator");
        foreach (GameObject predator in predators)
        {
            predatorTransforms.Add(predator.transform);
        }
    }

    public override void OnActionReceived(ActionBuffers actions) {
        survivedSteps++;
        globalStepCount++;
        if (!useHeuristic && globalStepCount >= HeuristicStepThreshold)
        {
            useHeuristic = true;
        }
        float speedForward = environmentController.preyTranslationSpeed * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rotateY = environmentController.preyRotationSpeed * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        transform.position += transform.forward * speedForward * Time.deltaTime;
        transform.Rotate(0f, rotateY, 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        float heuristicRadius = 10f; // Radius to activate heuristic
        Transform nearestPredator = null;
        float minDistance = float.MaxValue;
        foreach (var predator in predatorTransforms) {
            float dist = Vector3.Distance(transform.position, predator.position);
            if (dist < minDistance) {
                minDistance = dist;
                nearestPredator = predator;
            }
        }
        if (useHeuristic && nearestPredator != null && minDistance <= heuristicRadius) {
            // Move away from the nearest predator
            Vector3 toPredator = nearestPredator.position - transform.position;
            Vector3 awayFromPredator = -toPredator.normalized;
            float forwardAmount = Vector3.Dot(transform.forward, awayFromPredator);
            float turnAmount = Vector3.Cross(transform.forward, awayFromPredator).y;
            continuousActionsOut[0] = Mathf.Clamp(forwardAmount, -1f, 1f);
            continuousActionsOut[1] = Mathf.Clamp(turnAmount, -1f, 1f);
        } else {
            // Default random actions
            continuousActionsOut[0] = Random.Range(0f, 1f);
            continuousActionsOut[1] = Random.Range(-1f, 1f);
        }
    }

    private void OnCollisionEnter(Collision collision) {

        if (selfplay)
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                environmentController.preyObstacleCollision(this);
            }
        }
        else
        {
            if (collision.gameObject.CompareTag("Obstacle"))
            {
                Vector3 contactNormal = collision.contacts[0].normal;

                Vector3 incoming = transform.forward;
                Vector3 bounceDirection = Vector3.Reflect(incoming, contactNormal).normalized;

                Vector3 flatDirection = new Vector3(bounceDirection.x, 0f, bounceDirection.z).normalized;
                if (flatDirection != Vector3.zero)
                {
                    Quaternion flatRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                    transform.rotation = flatRotation;
                }

                transform.position += flatDirection / 2f;
            }
        }
    }
}