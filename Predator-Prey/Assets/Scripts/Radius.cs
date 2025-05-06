using UnityEngine;

public class script : MonoBehaviour {
    public float radius = 2f; 
    
    void OnDrawGizmosSelected() { 
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, radius); 
    }

    void Start() {}

    void Update() {}
}
