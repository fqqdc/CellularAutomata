using CellularAutomata;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main0(string[] args)
        {
            TwoDimAutomata automata = new TwoDimAutomata([0,1,2,3], [0]);

            Console.WriteLine(automata.RuleNumber[0]);



        }

        static void Main(string[] args)
        {

            int rule = 126;
            //for (; rule < 0; rule++)
            {
                OneDimAutomata automata = new(rule, 256, false);
                automata.SetValues([0, 0, 0, 0, 1]);
                int[] data = automata.Data;

                var msg = ConvertInts(data);
                Console.WriteLine(msg);

                for (int i = 0; i < 256; i++)
                {
                    automata.Iterate();
                    automata.CopyTo(data);
                    msg = ConvertInts(data);
                    Console.WriteLine(msg);
                }

                Console.WriteLine();
                Console.ReadKey(true);
            }
        }

        static string[]? BytesString;
        static string ConvertInts(int[] ints, char trueChar = 'X', char zeroChar = '.')
        {
            if (BytesString == null)
            {
                byte[] mask = [1 << 0, 1 << 1, 1 << 2, 1 << 3, 1 << 4, 1 << 5, 1 << 6, 1 << 7];
                char[][] byteCharArray = new char[256][];

                {
                    for (int i = 0; i < byteCharArray.Length; i++)
                    {
                        byteCharArray[i] = "".PadRight(8, zeroChar).ToCharArray();
                        int value = i;

                        for (int j = 0; j < 8; j++)
                        {
                            if ((value & mask[j]) != 0)
                                byteCharArray[i][j] = trueChar;
                        }
                        byteCharArray[i].AsSpan().Reverse();
                    }
                }
                BytesString = byteCharArray.Select(arr => new string(arr)).ToArray();
            }

            StringBuilder stringBuilder = new();
            var bytes = MemoryMarshal.Cast<int, byte>(ints);
            for (int i = 0; i < bytes.Length; i++)
            {
                stringBuilder.Insert(0, BytesString[bytes[i]]);
            }
            return stringBuilder.ToString();
        }
    }

}
