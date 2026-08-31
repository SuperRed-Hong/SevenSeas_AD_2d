using UnityEngine;
using System;
public abstract class FishingInputSource : MonoBehaviour
{
    public abstract Vector2 MoveInput { get;  }

    public abstract bool AccelerateHeld { get; }
   
    
    public event Action<float> CastPerformed;

    public event Action StrikePerformed;
    
    
    public abstract void SetCastEnabled(bool value);
    
    public abstract void SetStrikeEnabled(bool value);
    
    public abstract void SetAccelerateEnabled(bool value);

    public abstract void SetMoveEnabled(bool value);
    protected void RaiseCastPerformed(float power)
    {
        CastPerformed?.Invoke(Mathf.Clamp01(power));
    }

    protected void RaiseStrikePerformed()
    {
        StrikePerformed?.Invoke();
    }
    
    
}
