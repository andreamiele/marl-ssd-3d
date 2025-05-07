using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class PreyAgent : Agent {
    private Animator animator;
    private const string RUN = "Run";

    private string currentState;
    internal int survivedSteps = 0;
    private EnvironmentController environmentController;

    public void Start() {
        animator = gameObject.GetComponent<Animator>();
        ChangeAnimationState(RUN);
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
        continuousActionsOut[0] = Random.Range(-1f, 1f);
        continuousActionsOut[1] = Random.Range(-1f, 1f);
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