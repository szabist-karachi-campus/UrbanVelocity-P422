using UnityEngine;

public class CarStabilizer : MonoBehaviour
{
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Moves the weight of the car lower than the wheels
        rb.centerOfMass = centerOfMassOffset;
    }

    void OnDrawGizmosSelected()
    {
        if (GetComponent<Rigidbody>())
        {
            Gizmos.color = Color.red;
            Vector3 com = transform.position + (transform.rotation * centerOfMassOffset);
            Gizmos.DrawSphere(com, 0.2f);
        }
    }
}