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
    
    public int soloCapturesCount = 0;
    public int teamCapturesCount = 0; 

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

    public void LogMetrics() {
        var recorder = Academy.Instance.StatsRecorder;
        float loneWolfRate = 0f;
        if ((teamCapturesCount + soloCapturesCount) == 0)
            loneWolfRate = 0f;
        else
            loneWolfRate = (float)soloCapturesCount / (teamCapturesCount + soloCapturesCount);
        recorder.Add($"lone_wolf_rate_{name}", loneWolfRate, StatAggregationMethod.Average);
        Debug.Log($"[PredatorAgent] lone_wolf_rate_{name} = {loneWolfRate}");
        Debug.Log($"[PredatorAgent] solo_captures_count_{name} = {soloCapturesCount}");
        Debug.Log($"[PredatorAgent] team_captures_count_{name} = {teamCapturesCount}");
    }
}