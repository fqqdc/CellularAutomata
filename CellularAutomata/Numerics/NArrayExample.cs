namespace Numerics
{
    public static class NArrayExample
    {
        static void Run()
        {
            NArray<float> array = new(10, 10);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = i;
            }
            Console.WriteLine(array.Get2DString("F2"));

            {
                NArray<float> kernel = new(1f, 3, 3);
                var result = NArray.ConvolutionParallel(array, kernel, [-1, -1]);
                Console.WriteLine(result.Get2DString("F2"));
            }            

            {
                NArray<float> kernelH = new(1f, 3, 1);
                var result = NArray.Convolution(array, kernelH, [-1, 0]);

                NArray<float> kernelV = new(1f, 1, 3);
                result = NArray.Convolution(result, kernelV, [0, -1]);

                Console.WriteLine(result.Get2DString("F2"));
            }
        }

        
    }
}
