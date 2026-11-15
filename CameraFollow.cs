
# (C tuşu ile Dış Kamera / Kokpit İçi Ufuk Görünümü Geçişi)

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform lander;
    public Vector3 chaseOffset = new Vector3(0f, 6f, -15f);
    public Vector3 cockpitOffset = new Vector3(0f, 0.8f, 0.6f);
    public float smoothSpeed = 8f;

    private bool cockpitView = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            cockpitView = !cockpitView;
        }
    }

    void LateUpdate()
    {
        if (lander == null) return;

        if (cockpitView)
        {
            // First-person view from the lander looking toward the lunar horizon
            transform.position = lander.TransformPoint(cockpitOffset);
            transform.rotation = lander.rotation;
        }
        else
        {
            Vector3 desiredPos = lander.position + chaseOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
            transform.LookAt(lander);
        }
    }
}
