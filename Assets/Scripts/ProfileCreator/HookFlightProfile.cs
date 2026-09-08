using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "HookFlightProfile", menuName = "Seven Seas/Hook Flight Profile")]
public sealed class HookFlightProfile : ScriptableObject
{
    [Header("Launch Speed")] [SerializeField, Min(0.01f)]
    private float minimumLaunchSpeed = 10f;
    
    [SerializeField, Min(0.01f)]
    private float maximumLaunchSpeed = 22.36068f;

    [Header("Trajectory")] [SerializeField, Range(1f, 89f)]
    private float launchAngleDrees = 45f;

    [SerializeField, Min(0.01f)] private float gravity = 20f;

    public float LaunchAngleDegrees => launchAngleDrees;
    public float Gravity => Mathf.Max(0.01f, gravity);

    public float EvaluateLaunchSpeed(float power)
    {
        float minimum = Mathf.Max(0.01f, minimumLaunchSpeed);
        float maximum = Mathf.Max(minimum, maximumLaunchSpeed);
        
        return Mathf.Lerp(minimum, maximum, Mathf.Clamp01(power));
    }

    private void OnValidate()
    {
        minimumLaunchSpeed = Mathf.Max(0.01f, minimumLaunchSpeed);
        maximumLaunchSpeed = Mathf.Max(minimumLaunchSpeed, maximumLaunchSpeed);
        
        launchAngleDrees = Mathf.Clamp(launchAngleDrees, 1f, 89f);
        gravity = Mathf.Max(0.01f, gravity);
    }


}
