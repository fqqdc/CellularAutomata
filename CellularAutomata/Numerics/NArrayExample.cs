using System.Diagnostics;

namespace Numerics
{
    public static class NArrayExample
    {
        static void Run(string[] args)
        {
            NArray<float> array = new(2560, 1440);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }
            //Console.WriteLine(array.Get3DString("F2"));

            NArray<float> kernel = new(1f, 5, 5);

            Stopwatch sw = new();

            sw.Restart();

            var result1 = NArray.ConvolutionParallel(array, kernel, [-2, -2]);
            //Console.WriteLine(result1.Get3DString("F2"));

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);


            sw.Restart();

            NArray<float> kernelX = new(1f, 3, 1);
            var result2 = NArray.ConvolutionParallel(array, kernelX, [-2, 0]);
            NArray<float> kernelY = new(1f, 1, 3);
            result2 = NArray.ConvolutionParallel(result2, kernelY, [0, -2]);
            //NArray<float> kernelZ = new(1f, 1, 1, 3);
            //result2 = NArray.ConvolutionParallel(result2, kernelZ, [0, 0, -1]);
            //Console.WriteLine(result2.Get3DString("F2"));

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);

            return;

            for (int i = 0; i < result1.Length; i++)
            {
                if (result1[i] != result2[i])
                {
                    Console.WriteLine($"[{i}]  diff:{result1[i] - result1[i]} | 1:{result1[i]} != 2:{result2[i]}");
                }
            }
        }
    }
}
