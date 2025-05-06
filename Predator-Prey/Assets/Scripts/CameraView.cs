using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraView : MonoBehaviour {
    private EnvironmentController environmentController;
    public float cameraSpeed = 2f;

    void Start() {
        environmentController = GetComponentInParent<EnvironmentController>();
    }

    void Update() {
        this.transform.LookAt(environmentController.transform.position);
        this.transform.Translate(cameraSpeed * Vector3.right * Time.deltaTime);
    }
}
