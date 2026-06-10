using UnityEngine;
using System.Collections.Generic;

public class CableBuilder : MonoBehaviour
{
    [Header("Anchors")]
    public Rigidbody startAnchor;
    public Rigidbody endAnchor;

    [Header("Cable Settings")]
    public GameObject segmentPrefab;
    public int segmentCount = 20;
    public float segmentLength = 0.05f;

    private List<Rigidbody> segments = new List<Rigidbody>();

    void Start()
    {
        BuildCable();
    }

    void BuildCable()
    {
        Rigidbody previousBody = startAnchor;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = Vector3.Lerp(
                startAnchor.position,
                endAnchor.position,
                (float)i / segmentCount
            );

            GameObject segment = Instantiate(
                segmentPrefab,
                pos,
                Quaternion.identity,
                transform
            );

            Rigidbody rb = segment.GetComponent<Rigidbody>();
            ConfigurableJoint joint = segment.GetComponent<ConfigurableJoint>();

            joint.connectedBody = previousBody;

            // Ajuste fino del joint
            SoftJointLimit limit = joint.linearLimit;
            limit.limit = segmentLength;
            joint.linearLimit = limit;

            previousBody = rb;
            segments.Add(rb);
        }

        // Conectar último segmento al anchor final
        ConfigurableJoint endJoint =
            segments[^1].GetComponent<ConfigurableJoint>();

        endJoint.connectedBody = endAnchor;
    }
}
