
// SingleFireModule.cs
using UnityEngine;

public class SingleFireModule : BaseFireModule
{
    protected override bool ShouldFire()
    {
        // Fire only on button press, not hold
        return wasFirePressed;
    }
}
//end