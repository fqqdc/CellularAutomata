using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Numerics
{
    public class NArray<T> : IEnumerable<T>
    {
        readonly T[] _array = [];
        readonly int[] _rankLengths = [0];

        public NArray() { }
        public NArray(params int[] rankLengths)
        {
            if (rankLengths.Length < 1)
                throw new ArgumentException($"The length of {nameof(rankLengths)} is less than 1.");

            _rankLengths = [.. rankLengths];
            int size = rankLengths[0];
            for (int i = 1; i < rankLengths.Length; i++)
            {
                size *= _rankLengths[i];
            }
            _array = new T[size];
        }

        public NArray(T[] initValues, params int[] rankLengths) : this(rankLengths)
        {
            initValues.AsSpan(0..int.Min(initValues.Length, _array.Length)).CopyTo(_array);
        }

        public NArray(T initValue, params int[] rankLengths) : this(rankLengths)
        {
            Array.Fill(_array, initValue);
        }

        public ref T this[params int[] indexes]
        {
            get
            {
                if (_rankLengths.Length < indexes.Length)
                    throw new ArgumentException($"The length of {nameof(indexes)} is less than {nameof(RankCount)}.");

                int index = indexes[0];
                int rankSize = 1;
                for (int i = 1; i < indexes.Length; i++)
                {
                    rankSize *= _rankLengths[i - 1];
                    index += rankSize * indexes[i];
                }
                return ref _array[index];
            }
        }

        public T? GetValueOrDefault(T? defalutValue, params int[] indexes)
        {
            if (_rankLengths.Length < indexes.Length)
                throw new ArgumentException($"The length of {nameof(indexes)} is less than {nameof(RankCount)}.");

            if (indexes[0] < 0 || indexes[0] >= _rankLengths[0])
                return defalutValue;

            int index = indexes[0];
            int rankSize = 1;
            for (int i = 1; i < indexes.Length; i++)
            {
                if (indexes[i] < 0 || indexes[i] >= _rankLengths[i])
                    return defalutValue;
                rankSize *= _rankLengths[i - 1];
                index += rankSize * indexes[i];
            }


            return _array[index];
        }
        public T? GetValueOrDefault(Vector2 vector, T? defalutValue = default) => GetValueOrDefault(defalutValue, [(int)vector.X, (int)vector.Y]);
        public T? GetValueOrDefault(Vector3 vector, T? defalutValue = default) => GetValueOrDefault(defalutValue, [(int)vector.X, (int)vector.Y, (int)vector.Z]);

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array.Length;
        }

        public int RankCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _rankLengths.Length;
            }
        }

        public int[] RankLengths
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return [.. _rankLengths];
            }
        }

        #region IEnumerable<T>

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Array.AsReadOnly(_array).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _array.GetEnumerator();

        #endregion

        public Span<T>.Enumerator GetEnumerator() => _array.AsSpan().GetEnumerator();
    }

    public static class NArray
    {
        public static void Convolution<T>(NArray<T> input, NArray<T> kernel, int[] indexesKernelOffset, ref NArray<T> result)
            where T : INumber<T>
        {
            if (input.RankCount != kernel.RankCount || input.RankCount != indexesKernelOffset.Length)
                throw new NotSupportedException();

            var inputRankLengths = input.RankLengths;
            if (!result.RankLengths.SequenceEqual(inputRankLengths))
            {
                throw new ArgumentException($"The {nameof(result.RankLengths)} of {nameof(result)} is not equal to {nameof(input)}.");
            }

            int[] sizes = new int[input.RankCount];
            sizes[0] = 1;
            for (int i = 1; i < sizes.Length; i++)
            {
                sizes[i] = sizes[i - 1] * inputRankLengths[i - 1];
            }

            int[] indexesInput = new int[input.RankCount];
            for (int i = 0; i < input.Length; i++)
            {
                int index = i;

                for (int iSize = sizes.Length - 1; iSize >= 0; iSize--)
                {
                    indexesInput[iSize] = index / sizes[iSize];
                    index %= sizes[iSize];
                }

                var (sum, sumWeight) = ConvolutionIndexes(indexesInput, input, kernel, indexesKernelOffset);
                result[i] = sum / sumWeight;
            }
        }
        public static NArray<T> Convolution<T>(NArray<T> input, NArray<T> kernel, int[] indexesKernelOffset)
            where T : INumber<T>
        {
            var result = new NArray<T>(input.RankLengths);
            Convolution(input, kernel, indexesKernelOffset, ref result);

            return result;
        }

        public static ParallelOptions DefalutParallelOptions { get; set; } = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        public static void ConvolutionParallel<T>(NArray<T> input, NArray<T> kernel, int[] indexesKernelOffset, ref NArray<T> result)
        where T : INumber<T>
        {
            if (input.RankCount != kernel.RankCount || input.RankCount != indexesKernelOffset.Length)
                throw new NotSupportedException();

            var inputRankLengths = input.RankLengths;
            if (!result.RankLengths.SequenceEqual(inputRankLengths))
            {
                throw new ArgumentException($"The {nameof(result.RankLengths)} of {nameof(result)} is not equal to {nameof(input)}.");
            }

            int[] sizes = new int[input.RankCount];
            sizes[0] = 1;
            for (int i = 1; i < sizes.Length; i++)
            {
                sizes[i] = sizes[i - 1] * inputRankLengths[i - 1];
            }

            var refResult = result;
            Parallel.For(0, input.Length, DefalutParallelOptions, i =>
            {
                int[] indexesInput = new int[input.RankCount];
                int index = i;

                for (int iSize = sizes.Length - 1; iSize >= 0; iSize--)
                {
                    indexesInput[iSize] = index / sizes[iSize];
                    index %= sizes[iSize];
                }

                var (sum, sumWeight) = ConvolutionIndexes(indexesInput, input, kernel, indexesKernelOffset);
                refResult[i] = sum / sumWeight;
            });
        }
        public static NArray<T> ConvolutionParallel<T>(NArray<T> input, NArray<T> kernel, int[] indexesKernelOffset)
            where T : INumber<T>
        {
            var inputRankLengths = input.RankLengths;
            var result = new NArray<T>(inputRankLengths);
            ConvolutionParallel(input, kernel, indexesKernelOffset, ref result);
            return result;
        }

        private static (T sum, T sumWeight) ConvolutionIndexes<T>(int[] indexesInput, NArray<T> input, NArray<T> kernel, int[] indexesKernelOffset)
            where T : INumber<T>
        {
            var kernelRankLengths = kernel.RankLengths;
            int[] indexeskernel = new int[input.RankCount];
            int[] sizes = new int[input.RankCount];
            sizes[0] = 1;
            for (int i = 1; i < sizes.Length; i++)
            {
                sizes[i] = sizes[i - 1] * kernelRankLengths[i - 1];
            }

            T sum = T.Zero;
            T sumWeight = T.Zero;

            for (int i = 0; i < kernel.Length; i++)
            {
                int index = i;

                for (int iSize = sizes.Length - 1; iSize >= 0; iSize--)
                {
                    indexeskernel[iSize] = index / sizes[iSize];
                    index %= sizes[iSize];
                }

                var indexesInputOffset =
                    indexesInput
                    .Select((number, index) => number + indexeskernel[index] + indexesKernelOffset[index])
                    .ToArray();

                var value = input.GetValueOrDefault(T.Zero, indexesInputOffset)! * kernel[indexeskernel];
                sum += value;
                sumWeight += kernel[indexeskernel];

                //Debug.WriteLine($"{i}:({indexesInputOffset[0]},{indexesInputOffset[1]},{indexesInputOffset[2]}):{value}");
            }

            return (sum, sumWeight);
        }

        public static string Get2DString(this NArray<float> array, string format)
        {
            if (array.RankCount != 2) throw new NotSupportedException();
            var rankLengths = array.RankLengths;

            StringBuilder stringBuilder = new();

            for (int y = 0; y < rankLengths[1]; y++)
            {
                for (int x = 0; x < rankLengths[0]; x++)
                {
                    stringBuilder.Append(array[x, y].ToString(format))
                        .Append('\t');
                }
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        public static string Get3DString(this NArray<float> array, in string format, in int unitStringLength = 5)
        {
            if (array.RankCount != 3) throw new NotSupportedException();
            var rankLengths = array.RankLengths;

            StringBuilder stringBuilder = new();

            for (int y = 0; y < rankLengths[1]; y++)
            {
                for (int z = 0; z < rankLengths[2]; z++)
                {
                    for (int x = 0; x < rankLengths[0]; x++)
                    {
                        stringBuilder.Append(' ')
                            .Append(array[x, y, z].ToString(format).PadLeft(unitStringLength))
                            .Append(' ');
                    }

                    stringBuilder.Append(" | ");
                }
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }
    }
}
