# Canvas objesine eklenecek. Dikey hız 3m/s 'den hızlıysa veya eğim 15 derece'yi geçerse yazıyı kırmızıya çevirir


using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public Transform lander;
    public Rigidbody landerRb;
    public LanderController controller;
    public Terrain terrain;

    [Header("HUD Text Fields")]
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI vSpeedText;
    public TextMeshProUGUI hSpeedText;
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI tiltText;

    void Update()
    {
        if (lander == null || landerRb == null) return;

        float groundY = terrain != null ? terrain.SampleHeight(lander.position) + terrain.transform.position.y : 0f;
        float altitude = lander.position.y - groundY;

        #if UNITY_6000_0_OR_NEWER
        Vector3 v = landerRb.linearVelocity;
        #else
        Vector3 v = landerRb.velocity;
        #endif

        float vSpeed = v.y;
        float hSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        float tilt = Vector3.Angle(lander.up, Vector3.up);

        altitudeText.text = $"ALT: {altitude:F1} m";
        vSpeedText.text = $"V/S: {vSpeed:F2} m/s";
        hSpeedText.text = $"H/S: {hSpeed:F2} m/s";
        fuelText.text = $"FUEL: {controller.fuelMass:F0} kg";
        tiltText.text = $"TILT: {tilt:F1}°";

        // Cockpit Warning Colors
        vSpeedText.color = vSpeed < -3.0f ? Color.red : Color.green;
        tiltText.color = tilt > 15.0f ? Color.red : Color.green;
        fuelText.color = controller.fuelMass < 1000f ? Color.yellow : Color.white;
    }
}using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public Transform lander;
    public Rigidbody landerRb;
    public LanderController controller;
    public Terrain terrain;

    [Header("HUD Text Fields")]
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI vSpeedText;
    public TextMeshProUGUI hSpeedText;
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI tiltText;

    void Update()
    {
        if (lander == null || landerRb == null) return;

        float groundY = terrain != null ? terrain.SampleHeight(lander.position) + terrain.transform.position.y : 0f;
        float altitude = lander.position.y - groundY;

        #if UNITY_6000_0_OR_NEWER
        Vector3 v = landerRb.linearVelocity;
        #else
        Vector3 v = landerRb.velocity;
        #endif

        float vSpeed = v.y;
        float hSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        float tilt = Vector3.Angle(lander.up, Vector3.up);

        altitudeText.text = $"ALT: {altitude:F1} m";
        vSpeedText.text = $"V/S: {vSpeed:F2} m/s";
        hSpeedText.text = $"H/S: {hSpeed:F2} m/s";
        fuelText.text = $"FUEL: {controller.fuelMass:F0} kg";
        tiltText.text = $"TILT: {tilt:F1}°";

        // Cockpit Warning Colors
        vSpeedText.color = vSpeed < -3.0f ? Color.red : Color.green;
        tiltText.color = tilt > 15.0f ? Color.red : Color.green;
        fuelText.color = controller.fuelMass < 1000f ? Color.yellow : Color.white;
    }
}
