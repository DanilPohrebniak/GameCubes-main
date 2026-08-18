using UnityEngine;

public class Dice : MonoBehaviour
{
    [Header("Face Anchors")]
    public Transform[] faceAnchors; // 6 пустых объектов, привязанных к граням кубика

    [Header("Звук удара о стол")]
    public AudioClip[] hitSounds;           // несколько вариантов звука для разнообразия
    public string tableTag = "Table";       // тег объекта стола
    public float minTimeBetweenSounds = 0.05f; // защита от "треска" при частых столкновениях
    public float minImpactForce = 0.5f;     // ниже этой силы удара звук не проигрываем (мелкое дребезжание)
    public float maxImpactForce = 5f;       // сила удара, при которой звук уже на полной громкости

    private Rigidbody rb;
    private AudioSource audioSource;
    private float lastPlayTime = -1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(tableTag)) return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce) return;

        PlayHitSound(impactForce);
    }

    private void PlayHitSound(float impactForce)
    {
        if (hitSounds == null || hitSounds.Length == 0) return;
        if (Time.time - lastPlayTime < minTimeBetweenSounds) return;

        lastPlayTime = Time.time;

        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
        float volume = Mathf.Clamp01(impactForce / maxImpactForce);

        audioSource.pitch = Random.Range(0.92f, 1.08f); // лёгкая вариация тона, чтобы звук не звучал одинаково
        audioSource.PlayOneShot(clip, volume);
    }

    public bool IsStopped()
    {
        return rb.IsSleeping();
    }

    public int GetValue()
    {
        if (!IsStopped()) return 0;

        Transform bestFace = null;
        float maxDot = -1f;

        foreach (var face in faceAnchors)
        {
            float dot = Vector3.Dot(face.up, Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestFace = face;
            }
        }

        return bestFace != null ? bestFace.GetSiblingIndex() + 1 : 0;
    }
}