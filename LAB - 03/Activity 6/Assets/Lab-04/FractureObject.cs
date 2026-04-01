using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class FractureObject : MonoBehaviour
{
    public GameObject originalObject;
    public GameObject fracturedObject;
    public GameObject Particles;

    public float explosionMinForce = 5f;
    public float explosionMaxForce = 100f;
    public float explosionForceRadius = 10f;
    public float fragScaleFactor = 1f;

    public float vfxDuration = 10f;
    public float explodeEarlyBy = 0.1f;

    public bool runOnAwake = false;

    public Transform cameraToShake;
    public float shakeStartTime = 8f;
    public float shakeEndTime = 10f;
    public float maxShakeAmount = 0.15f;

    private VisualEffect objectVFX;
    private bool hasExploded = false;

    private Transform[] fragments;
    private Vector3[] startLocalPositions;
    private Quaternion[] startLocalRotations;
    private Vector3[] startLocalScales;

    private Vector3 cameraOriginalLocalPos;

    void Start()
    {
        if (originalObject != null)
        {
            objectVFX = originalObject.GetComponent<VisualEffect>();
        }

        if (fracturedObject != null)
        {
            fracturedObject.SetActive(false);
        }

        if (Particles != null)
        {
            Particles.SetActive(false);
        }

        if (cameraToShake != null)
        {
            cameraOriginalLocalPos = cameraToShake.localPosition;
        }

        if (fracturedObject != null)
        {
            int count = fracturedObject.transform.childCount;

            fragments = new Transform[count];
            startLocalPositions = new Vector3[count];
            startLocalRotations = new Quaternion[count];
            startLocalScales = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Transform t = fracturedObject.transform.GetChild(i);
                fragments[i] = t;
                startLocalPositions[i] = t.localPosition;
                startLocalRotations[i] = t.localRotation;
                startLocalScales[i] = t.localScale;
            }
        }

        if (runOnAwake)
        {
            ResetShield();
            StartCoroutine(ExplodeAfterVFX());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ResetShield();
            StartCoroutine(ExplodeAfterVFX());
        }
    }

    IEnumerator ExplodeAfterVFX()
    {
        float totalTime = shakeEndTime;
        float timer = 0f;

        while (timer < totalTime)
        {
            if (timer >= shakeStartTime && cameraToShake != null)
            {
                float t = (timer - shakeStartTime) / (shakeEndTime - shakeStartTime);
                t = Mathf.Clamp01(t);

                float currentShake = Mathf.Lerp(0f, maxShakeAmount, t);
                Vector3 randomOffset = Random.insideUnitSphere * currentShake;
                cameraToShake.localPosition = cameraOriginalLocalPos + randomOffset;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (cameraToShake != null)
        {
            cameraToShake.localPosition = cameraOriginalLocalPos;
        }

        if (!hasExploded)
        {
            Explode();
            hasExploded = true;
        }
    }

    void Explode()
    {
        if (originalObject == null || fracturedObject == null)
            return;

        fracturedObject.SetActive(true);
        Particles?.SetActive(true);
        originalObject.SetActive(false);

        foreach (Transform t in fracturedObject.transform)
        {
            Rigidbody rb = t.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.AddExplosionForce(
                    Random.Range(explosionMinForce, explosionMaxForce),
                    originalObject.transform.position,
                    explosionForceRadius
                );

                StartCoroutine(Shrink(t, 2f));
            }
        }

        StartCoroutine(HideFracturedAfterDelay(5f));
    }

    IEnumerator HideFracturedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        fracturedObject.SetActive(false);
    }

    IEnumerator Shrink(Transform t, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 newScale = t.localScale;

        while (newScale.x > 0f && newScale.y > 0f && newScale.z > 0f)
        {
            newScale -= new Vector3(fragScaleFactor, fragScaleFactor, fragScaleFactor) * 0.05f;
            t.localScale = newScale;
            yield return new WaitForSeconds(0.05f);
        }
    }

    void ResetShield()
    {
        StopAllCoroutines();

        hasExploded = false;

        if (originalObject != null)
        {
            originalObject.SetActive(true);
        }

        if (fracturedObject != null)
        {
            fracturedObject.SetActive(false);
        }

        if (Particles != null)
        {
            Particles.SetActive(false);
        }

        if (cameraToShake != null)
        {
            cameraToShake.localPosition = cameraOriginalLocalPos;
        }

        if (objectVFX != null)
        {
            objectVFX.Reinit();
            objectVFX.Play();
        }

        if (fragments != null)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                Transform t = fragments[i];

                t.localPosition = startLocalPositions[i];
                t.localRotation = startLocalRotations[i];
                t.localScale = startLocalScales[i];

                Rigidbody rb = t.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }
            }
        }
    }
}