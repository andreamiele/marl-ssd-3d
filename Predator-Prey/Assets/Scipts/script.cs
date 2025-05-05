using UnityEngine;

public class script : MonoBehaviour
{
    public float radius = 2f; 
    void OnDrawGizmosSelected() 
    { 
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, radius); 
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
