using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class PreyAgent : Agent {
    private string currentState;
    internal int survivedSteps = 0;
    private EnvironmentController environmentController;

    public void Start() {
        environmentController = GetComponentInParent<EnvironmentController>();
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float speedForward = environmentController.preyTranslationSpeed * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rotateY = environmentController.preyRotationSpeed * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        transform.position += transform.forward * speedForward * Time.deltaTime;
        transform.Rotate(0f, rotateY, 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Random.Range(0f, 1f);
        continuousActionsOut[1] = Random.Range(-1f, 1f);
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Obstacle")) {
            Vector3 contactNormal = collision.contacts[0].normal;

            Vector3 incoming = transform.forward;
            Vector3 bounceDirection = Vector3.Reflect(incoming, contactNormal).normalized;

            Vector3 flatDirection = new Vector3(bounceDirection.x, 0f, bounceDirection.z).normalized;
            if (flatDirection != Vector3.zero) {
                Quaternion flatRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
                transform.rotation = flatRotation;
            }

            transform.position += flatDirection / 2f;
        }
    }
}