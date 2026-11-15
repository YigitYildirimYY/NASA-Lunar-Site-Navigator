using UnityEngine;
using TMPro;

public class LunarNavigator : MonoBehaviour
{
    [System.Serializable]
    public struct LandingSite
    {
        public string siteName;
        public float latitude;
        public float longitude;
        public float sunBaseElevation;   // Degrees above horizon
        public float earthBaseElevation; // Degrees above horizon
        public float localHorizonAngle;  // Crater wall / terrain obstruction
        public Vector3 spawnPosition;
    }

    [Header("Scene References")]
    public Transform lander;
    public Light sunLight;
    public Transform earthSphere;
    public TextMeshProUGUI navTelemetryText;

    [Header("Simulation Settings")]
    public float lunarDayHours = 0f;
    public float timeSpeedMultiplier = 24f; // 1 real sec = 24 lunar mission hours when holding T

    private LandingSite[] sites;
    private int currentSiteIndex = 0;

    void Start()
    {
        sites = new LandingSite[]
        {
            new LandingSite {
                siteName = "1: Shackleton Rim (South Pole - Artemis III)",
                latitude = -89.90f, longitude = 0.00f,
                sunBaseElevation = 1.5f, earthBaseElevation = 0.8f,
                localHorizonAngle = 1.2f,
                spawnPosition = new Vector3(0, 200, 0)
            },
            new LandingSite {
                siteName = "2: Malapert Massif (5km Peak - High DTE)",
                latitude = -86.00f, longitude = 0.00f,
                sunBaseElevation = 4.2f, earthBaseElevation = 5.5f,
                localHorizonAngle = -0.5f,
                spawnPosition = new Vector3(150, 250, 150)
            },
            new LandingSite {
                siteName = "3: Apollo 11 - Tranquility Base (Equator Ref)",
                latitude = 0.67f, longitude = 23.47f,
                sunBaseElevation = 35.0f, earthBaseElevation = 68.0f,
                localHorizonAngle = 0.2f,
                spawnPosition = new Vector3(-150, 200, -150)
            }
        };

        ApplySite(0);
    }

    void Update()
    {
        // Quick Site Comparison Keys (1, 2, 3)
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplySite(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplySite(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplySite(2);

        // Hold T to fast-forward lunar timeline (Temporal Dynamics)
        if (Input.GetKey(KeyCode.T))
        {
            lunarDayHours += Time.deltaTime * timeSpeedMultiplier;
        }

        UpdateCelestialPositions();
    }

    void ApplySite(int index)
    {
        currentSiteIndex = index;
        lunarDayHours = 0f;
        if (lander != null)
        {
            lander.position = sites[index].spawnPosition;
            lander.rotation = Quaternion.identity;
            Rigidbody rb = lander.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                #else
                rb.velocity = Vector3.zero;
                #endif
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void UpdateCelestialPositions()
    {
        LandingSite site = sites[currentSiteIndex];

        // 29.53 Earth days = 708.7 hours per synodic lunar day
        float lunarPhaseRad = (lunarDayHours / 708.72f) * Mathf.PI * 2f;

        // Calculate realistic Sun & Earth elevation with lunar libration wobble
        float sunElevation = site.sunBaseElevation + (site.latitude > -10f ? 70f * Mathf.Sin(lunarPhaseRad) : 1.5f * Mathf.Sin(lunarPhaseRad));
        float sunAzimuth = (lunarDayHours / 708.72f) * 360f;

        // Earth libration (~27.3 day anomalistic cycle, +/- 6.7 deg wobble)
        float librationRad = (lunarDayHours / 655.7f) * Mathf.PI * 2f;
        float earthElevation = site.earthBaseElevation + 6.5f * Mathf.Sin(librationRad);
        float earthAzimuth = 180f + 7.5f * Mathf.Cos(librationRad);

        // Rotate Directional Light (Sun)
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunElevation, sunAzimuth, 0f);
            sunLight.intensity = sunElevation > site.localHorizonAngle ? 2.2f : 0.05f;
        }

        // Position Earth Sphere in the sky relative to Lander
        if (earthSphere != null && lander != null)
        {
            Quaternion earthRot = Quaternion.Euler(-earthElevation, earthAzimuth, 0f);
            earthSphere.position = lander.position + (earthRot * Vector3.forward * 1500f);
            earthSphere.LookAt(lander.position);
        }

        // Power & Direct-to-Earth (DTE) Assessment
        bool sunVisible = sunElevation > site.localHorizonAngle;
        bool earthVisible = earthElevation > site.localHorizonAngle;
        float powerEfficiency = sunVisible ? Mathf.Clamp(Mathf.Sin(Mathf.Deg2Rad * Mathf.Max(sunElevation, 2f)) * 400f, 15f, 100f) : 0f;

        // Estimate remaining hours before Sunset / Earthset
        string sunsetStatus = !sunVisible ? "IN SHADOW (0% Power)" : (site.latitude < -80f ? "Grazing Sun (~140h window)" : $"{Mathf.Max(0, 354f - (lunarDayHours % 708.7f)):F0} hrs to Sunset");
        string commsStatus = earthVisible ? "<color=#00F5D4>DTE LINK ACTIVE</color>" : "<color=#EF476F>EARTH BLOCKED BY TERRAIN</color>";

        if (navTelemetryText != null)
        {
            navTelemetryText.text =
                $"<b>SITE [Keys 1-3]:</b> {site.siteName}\n" +
                $"<b>COORDS:</b> {site.latitude:F2}°, {site.longitude:F2}° | <b>MISSION TIME [Hold T]:</b> +{lunarDayHours:F1} hrs\n" +
                $"<b>SOLAR ELEV:</b> {sunElevation:F2}° (Horizon: {site.localHorizonAngle:F1}°) | <b>SOLAR POWER:</b> {powerEfficiency:F0}%\n" +
                $"<b>SUNSET WINDOW:</b> {sunsetStatus}\n" +
                $"<b>EARTH ELEV:</b> {earthElevation:F2}° | <b>COMMS:</b> {commsStatus}";
        }
    }
}
