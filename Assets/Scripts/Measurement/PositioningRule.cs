///<summary>
///仮想オブジェクトを格子状に配置するための関数
/// Serverにおいて仮想オブジェクトを配置するかの判定時実装
/// 近傍にオブジェクトがあるかどうかの判定は残しておく
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class PositioningRule
{
    static float[] delta = new float[3];
    static int safeline = 95;

    public static bool FollowRule(Vector3 mPoint, float interval, int safe)
    {       
        safeline = safe;
        var hInterval = interval / 2f;

        delta[0] = mPoint.x % interval;

        delta[1] = mPoint.y % interval;

        delta[2] = mPoint.z % interval;


        for (int i = 0 ; i < 3; i++)
        {
            if (delta[i] > hInterval)
            {
                delta[i] = interval - delta[i];
            }
        }
        return BeSafe(RMS(delta), interval, safeline);
    }


    static float RMS(float[] delta)
    {

        float sum = 0;

        for (int i = 0; i<delta.Length; i++)
        {
            delta[i] = Mathf.Pow(delta[i],2) / 3f;
            sum += delta[i];
        }
        float rms = Mathf.Sqrt(sum);
        return rms;
    }


    static bool BeSafe(float rms, float interval, int safeline)
    {
        float error = ((interval - rms) * 100f) / interval;
        return error > safeline;
    }
}