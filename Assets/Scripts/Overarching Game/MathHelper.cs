using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathHelper
{
    #region Math helper
    
    public static Vector3 RotateRight90Deg(this Vector3 v)
    {
        return new Vector3(v.z,0, -v.x);
    }

    public static Vector3 RotateLeft90Deg(this Vector3 v)
    {
        return new Vector3(-v.z,0, v.x);
    }
    #endregion
}