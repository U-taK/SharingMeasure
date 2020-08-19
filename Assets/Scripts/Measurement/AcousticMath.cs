using UnityEngine;
using System;

class AcousticMath
{
    /// <summary>
    /// クロススペクトル法で三次元音響インテンシティ計算
    /// </summary>
    public static Vector3 CrossSpectrumMethod(double[][] sound_signals, int fs, int length_bit, float freq_range_min, float freq_range_max, float atmDensity, float dr)
    {
        //サンプル長を求める
        int sampleLength = 1 << length_bit;

        //FFT
        double[][] fftRe = new double[4][];
        double[][] fftIm = new double[4][];
        for (int micID = 0; micID < 4; micID++)
        {
            fftIm[micID] = new double[sampleLength];
            FFT(length_bit, sound_signals[micID], fftIm[micID], out fftRe[micID], out fftIm[micID]);
            //FFT結果を平均化
            for (int fftIndex = 0; fftIndex < sampleLength; fftIndex++)
            {
                fftRe[micID][fftIndex] /= sampleLength;
                fftIm[micID][fftIndex] /= sampleLength;
            }
        }

        //FFTした時のサンプル範囲を求める
        float df = fs / sampleLength;
        int fftIndexMin = Mathf.CeilToInt(freq_range_min / df);
        int fftIndexMax = Mathf.FloorToInt(freq_range_max / df);

        //インテンシティの積分範囲を求める
        double sig01 = 0d;
        double sig02 = 0d;
        double sig03 = 0d;
        double sig12 = 0d;
        double sig13 = 0d;
        double sig23 = 0d;
        for (int fftIndex = fftIndexMin; fftIndex <= fftIndexMax; fftIndex++)
        {
            //ナイキスト周波数の時だけ計算
            if (fftIndex <= sampleLength / 2 )
            {
                //両側クロススペクトルの虚部
                double imS01 = fftRe[0][fftIndex] * fftIm[1][fftIndex] - fftRe[1][fftIndex] * fftIm[0][fftIndex];
                double imS02 = fftRe[0][fftIndex] * fftIm[2][fftIndex] - fftRe[2][fftIndex] * fftIm[0][fftIndex];
                double imS03 = fftRe[0][fftIndex] * fftIm[3][fftIndex] - fftRe[3][fftIndex] * fftIm[0][fftIndex];
                double imS12 = fftRe[1][fftIndex] * fftIm[2][fftIndex] - fftRe[2][fftIndex] * fftIm[1][fftIndex];
                double imS13 = fftRe[1][fftIndex] * fftIm[3][fftIndex] - fftRe[3][fftIndex] * fftIm[1][fftIndex];
                double imS23 = fftRe[2][fftIndex] * fftIm[3][fftIndex] - fftRe[3][fftIndex] * fftIm[2][fftIndex];

                //両側クロススペクトル虚部 -> 片側クロススペクトル虚部
                double imG01;
                double imG02;
                double imG03;
                double imG12;
                double imG13;
                double imG23;
                if (fftIndex == 0 || fftIndex == sampleLength / 2)
                {
                    imG01 = imS01;
                    imG02 = imS02;
                    imG03 = imS03;
                    imG12 = imS12;
                    imG13 = imS13;
                    imG23 = imS23;
                }
                else
                {
                    imG01 = 2 * imS01;
                    imG02 = 2 * imS02;
                    imG03 = 2 * imS03;
                    imG12 = 2 * imS12;
                    imG13 = 2 * imS13;
                    imG23 = 2 * imS23;
                }

                //積分範囲
                sig01 += imG01 / fftIndex;
                sig02 += imG02 / fftIndex;
                sig03 += imG03 / fftIndex;
                sig12 += imG12 / fftIndex;
                sig13 += imG13 / fftIndex;
                sig23 += imG23 / fftIndex;
            }
        }

        //インテンシティ計算
        double i01 = sig01 / (2d * Math.PI * atmDensity * dr);
        double i02 = sig02 / (2d * Math.PI * atmDensity * dr);
        double i03 = sig03 / (2d * Math.PI * atmDensity * dr);
        double i12 = sig12 / (2d * Math.PI * atmDensity * dr);
        double i13 = sig13 / (2d * Math.PI * atmDensity * dr);
        double i23 = sig23 / (2d * Math.PI * atmDensity * dr);

        //左手系ワールド座標(x,y,z)に変換
        double ix = -(i01 - i02 + i23 - i13 - (2 * i12)) / 4;
        double iy = -(i01 + i02 + i03) / Math.Sqrt(6d);
        double iz = -(-i01 - i02 + (2 * i03) + (3 * i13) + (3 * i23)) / (4d * Math.Sqrt(3d));
        return new Vector3((float)ix, (float)iy, (float)iz);
    }

    /// <summary>
    /// 三次元音響インテンシティ(vec3)からインテンシティレベル(float)を計算
    /// </summary>
    public static float CalcuIntensityLevel(Vector3 intensity)
    {
        float intensityNorm = intensity.magnitude;
        float intensityLevel = 10f * Mathf.Log10(intensityNorm / Mathf.Pow(10f, -12f));
        return intensityLevel;
    }

    /// <summary>
    /// FFT
    /// </summary>
    /// <param name="bitSize">ビット数</param>
    /// <param name="inputRe">入力(実数)</param>
    /// <param name="inputIm">入力(虚数) -> なければoutputImと一緒でOK</param>
    /// <param name="outputRe">結果(実数)</param>
    /// <param name="outputIm">結果(虚数)</param>
    private static void FFT(int bitSize, double[] inputRe, double[] inputIm, out double[] outputRe, out double[] outputIm)
    {
        int dataSize = 1 << bitSize;
        int[] reverseBitArray = BitScrollArray(dataSize);

        outputRe = new double[dataSize];
        outputIm = new double[dataSize];

        // バタフライ演算のための置き換え
        for (int i = 0; i < dataSize; i++)
        {
            outputRe[i] = inputRe[reverseBitArray[i]];
            outputIm[i] = inputIm[reverseBitArray[i]];
        }

        // バタフライ演算
        for (int stage = 1; stage <= bitSize; stage++)
        {
            int butterflyDistance = 1 << stage;
            int numType = butterflyDistance >> 1;
            int butterflySize = butterflyDistance >> 1;

            double wRe = 1.0;
            double wIm = 0.0;
            double uRe =
                System.Math.Cos(System.Math.PI / butterflySize);
            double uIm =
                -System.Math.Sin(System.Math.PI / butterflySize);

            for (int type = 0; type < numType; type++)
            {
                for (int j = type; j < dataSize; j += butterflyDistance)
                {
                    int jp = j + butterflySize;
                    double tempRe =
                        outputRe[jp] * wRe - outputIm[jp] * wIm;
                    double tempIm =
                        outputRe[jp] * wIm + outputIm[jp] * wRe;
                    outputRe[jp] = outputRe[j] - tempRe;
                    outputIm[jp] = outputIm[j] - tempIm;
                    outputRe[j] += tempRe;
                    outputIm[j] += tempIm;
                }
                double tempWRe = wRe * uRe - wIm * uIm;
                double tempWIm = wRe * uIm + wIm * uRe;
                wRe = tempWRe;
                wIm = tempWIm;
            }
        }
    }

    /// <summary>
    /// ビットを左右反転した配列を返す
    /// </summary>
    /// <param name="arraySize"></param>
    /// <returns></returns>
    private static int[] BitScrollArray(int arraySize)
    {
        int[] reBitArray = new int[arraySize];
        int arraySizeHarf = arraySize >> 1;

        reBitArray[0] = 0;
        for (int i = 1; i < arraySize; i <<= 1)
        {
            for (int j = 0; j < i; j++)
                reBitArray[j + i] = reBitArray[j] + arraySizeHarf;
            arraySizeHarf >>= 1;
        }
        return reBitArray;
    }
}