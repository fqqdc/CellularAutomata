using System;
using System.Diagnostics;

namespace Numerics
{
    public static class NArrayExample
    {
        public static void Run()
        {
            NArray<float> array = new(100, 100);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }
            //Console.WriteLine(array.Get3DString("F2"));

            NArray<float> kernel = new(1f, 13, 13);

            Stopwatch sw = new();

            sw.Restart();

            var result1 = NArray.ConvolutionParallel(array, kernel, [-6, -6]);
            //Console.WriteLine(result1.Get3DString("F2"));

            sw.Stop();
            Console.WriteLine($"cost {sw.Elapsed.TotalMicroseconds} us");


            sw.Restart();

            NArray<float> kernelX = new(1f, 13, 1);
            var result2 = NArray.ConvolutionParallel(array, kernelX, [-6, 0]);
            NArray<float> kernelY = new(1f, 1, 13);
            result2 = NArray.ConvolutionParallel(result2, kernelY, [0, -6]);
            //NArray<float> kernelZ = new(1f, 1, 1, 3);
            //result2 = NArray.ConvolutionParallel(result2, kernelZ, [0, 0, -1]);
            //Console.WriteLine(result2.Get3DString("F2"));

            sw.Stop();
            Console.WriteLine($"cost {sw.Elapsed.TotalMicroseconds} us");

            return;

            for (int i = 0; i < result1.Length; i++)
            {
                var diff = float.Abs(result1[i] - result2[i]);
                if (diff > 1e-5)
                {
                    Console.WriteLine($"[{i}]  diff:{diff:e2} | {result1[i]:F2} != {result2[i]:F2}");
                }
            }
        }


    }
}
