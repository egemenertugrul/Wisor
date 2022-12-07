using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utilities
{
    public static string ToDelimitedString(this double[] values)
    {
        return string.Join(",", values);
    }
}
