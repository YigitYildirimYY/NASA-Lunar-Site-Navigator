using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LanderController : MonoBehaviour
{
    [Header("Engine Specs")]
    public float maxThrust = 45000f;      // Newtons (Apollo LM ~45 kN)
    public float fuelMass = 8000f;        // kg
    public float dryMass = 2000f;         // kg
    public float exhaustVelocity = 3000f; // m/s

    [Header("RCS Attitude Control")]
    public float torqueForce = 5000f;

    [Header("Optional Effects")]
    public ParticleSystem thrusterParticles;

    private Rigidbody rb;
    public bool isThrusting { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = dryMass + fuelMass;
    }

    void FixedUpdate()
    {
        HandleThrust();
        HandleRotation();
    }

    void HandleThrust()
    {
        if (Input.GetKey(KeyCode.Space) && fuelMass > 0f)
        {
            isThrusting = true;
            float massFlowRate = maxThrust / exhaustVelocity;
            float fuelUsed = massFlowRate * Time.fixedDeltaTime;
            fuelMass = Mathf.Max(0f, fuelMass - fuelUsed);
            rb.mass = dryMass + fuelMass;

            rb.AddForce(transform.up * maxThrust, ForceMode.Force);

            if (thrusterParticles != null && !thrusterParticles.isPlaying)
                thrusterParticles.Play();
        }
        else
        {
            isThrusting = false;
            if (thrusterParticles != null && thrusterParticles.isPlaying)
                thrusterParticles.Stop();
        }
    }

    void HandleRotation()
    {
        float pitch = Input.GetAxis("Vertical");   // W/S
        float roll = Input.GetAxis("Horizontal");  // A/D
        float yaw = 0f;
        if (Input.GetKey(KeyCode.Q)) yaw = 1f;
        if (Input.GetKey(KeyCode.E)) yaw = -1f;

        rb.AddTorque(transform.right * pitch * torqueForce);
        rb.AddTorque(transform.forward * roll * torqueForce);
        rb.AddTorque(transform.up * yaw * torqueForce);
    }
}

