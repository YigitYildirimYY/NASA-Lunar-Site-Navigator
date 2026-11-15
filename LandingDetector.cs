/*
Bu script sahnedeki boş bir GameManager objesine eklenecek.
1, 2, 3 tuşlarıyla farklı Ay bölgeleri arasında geçiş yapar, 
T tuşuyla zamanı hızlandırarak Güneş ve Dünya'nın ufuk çizgisine göre hareketini ve iletişim kesintilerini simüle eder.
*/


using UnityEngine;
using UnityEngine.SceneManagement;

public class LandingDetector : MonoBehaviour
{
    public float maxLandingSpeed = 3f;
    public float maxTiltAngle = 15f;
    public GameObject successUI;
    public GameObject failUI;

    private bool landed = false;

    void OnCollisionEnter(Collision col)
    {
        if (landed) return;
        landed = true;

        float impactSpeed = col.relativeVelocity.magnitude;
        float tilt = Vector3.Angle(transform.up, Vector3.up);

        if (impactSpeed <= maxLandingSpeed && tilt <= maxTiltAngle)
        {
            if (successUI != null) successUI.SetActive(true);
            Debug.Log("MISSION SUCCESS — Safe landing!");
        }
        else
        {
            if (failUI != null) failUI.SetActive(true);
            Debug.Log($"CRASH — Speed: {impactSpeed:F1} m/s, Tilt: {tilt:F1}°");
        }

        GetComponent<Rigidbody>().isKinematic = true;
    }

    public void RetryMission()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
