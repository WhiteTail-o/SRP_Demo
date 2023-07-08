using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderKeywords   // 用于存放较为通用的Keyword
{
    public static string[] _DIRECTIONAL_PCF = {
        "_DIRECTIONAL_PCF2",
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7"
    };

    public static string[] _CASCADE_BLEND = {
        "_CASCADE_BLEND_HARE",
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
}