using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class PredatorAgent : Agent {
    private Animator animator;
    private string currentState;
    private const string RUN_FORWARD = "RunForward";
    private const string RUN_BACKWARD = "RunBack";
    private const string WALK_FORWARD = "Walk Forward";
    private const string WALK_BACKWARD = "Walk Backward";

    private EnvironmentController environmentController;

    public void Start() {
        animator = GetComponent<Animator>();
        ChangeAnimationState(RUN_FORWARD);
        environmentController = GetComponentInParent<EnvironmentController>();
    }

    public override void OnActionReceived(ActionBuffers actions) {
        float speedForward = environmentController.predatorTranslationSpeed * Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rotateY = environmentController.predatorRotationSpeed * Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        transform.position += transform.forward * speedForward * Time.deltaTime;
        transform.Rotate(0f, rotateY, 0f);
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
        }
    }

    private void ChangeAnimationState(string newState) {
        if (newState == currentState) return;
        animator.Play(newState);
        currentState = newState;
    }

    private bool IsAnimationPlaying(Animator animator, string stateName) {
        return (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f);
    }
}