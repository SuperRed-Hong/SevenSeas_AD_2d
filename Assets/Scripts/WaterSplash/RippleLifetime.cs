using UnityEngine;

public class RippleLifetime : MonoBehaviour
{
    public float lifetime = 0.8f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}