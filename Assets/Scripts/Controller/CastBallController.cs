using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class CastBallController : MonoBehaviour
{
    [SerializeField] private CastTuningProfile tuningProfile;
    [SerializeField, Min(0f)] private float minimumFlightDuration = 0.2f;
    [SerializeField, Min(0f)] private float landingSpeedThreshold = 0.5f;

    private Rigidbody2D body;
    private Vector3 startPosition;
    private float launchTime;
    
    public event Action<float> Landed;

    public bool IsFlying { get; private set; }
    public float CurrentDistance => Mathf.Max(0f, transform.position.x - startPosition.x);
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
        ResetHook();
    }

    public void Configure(CastTuningProfile profile)
    {
        tuningProfile = profile;
    }

    public void Launch(float power)
    {
        if (tuningProfile == null)
        {
            Debug.LogError("CastBallController requires a CastTuningProfile.");
            return;
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.linearVelocity = tuningProfile.EvaluateLaunchVelocity(power);
        body.angularVelocity = 0f;
        launchTime = Time.time;
        IsFlying = true;
    }

    public void ResetHook()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        transform.SetPositionAndRotation(startPosition, Quaternion.identity);
        IsFlying = false;
        Physics2D.SyncTransforms();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryCompleteLanding(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryCompleteLanding(collision);
    }

    private void TryCompleteLanding(Collision2D collision)
    {
        if (!IsFlying || Time.time - launchTime < minimumFlightDuration)
        {
            return;
        }

        bool hasGroundContact = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                hasGroundContact = true;
                break;
            }
        }

        if (!hasGroundContact || Mathf.Abs(body.linearVelocity.y) > landingSpeedThreshold)
        {
            return;
        }

        IsFlying = false;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        Landed?.Invoke(CurrentDistance);
    }
}
