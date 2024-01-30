using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;

namespace CellularAutomata
{
    /// <summary>
    /// 双核细胞自动机
    /// </summary>
    internal class DoubleKernelCellularAutomata : IEnumerable<Vector2>
    {
        private readonly float _InsideKernelRadius;
        private readonly ValueRange _InsideBirthValue;
        private readonly ValueRange _InsideSurviveValue;
        private readonly float _OutsideKernelRadius;
        private readonly ValueRange _OutsideBirthValue;
        private readonly ValueRange _OutsideSurviveValue;
        private HashSet<Vector2> _Coords = [];

        private readonly static ParallelOptions _ParallelOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };

        public (int left, int top, int right, int bottom)? Bounding { get; set; }

        public record ValueRange
        {
            //最大值不能小于最小值
            public ValueRange(float min, float max)
            {
                if (min > max)
                {
                    throw new ArgumentException("最大值不能小于最小值");
                }
                Min = min;
                Max = max;
            }

            public double Min { get; }
            public double Max { get; }
        }

        public DoubleKernelCellularAutomata(float insideKernelRadius, ValueRange insideBirthValue, ValueRange insideSurviveValue,
            float outsideKernelRadius, ValueRange outsideBirthValue, ValueRange outsideSurviveValue)
        {
            //初始化卷积核半径
            //核的半径不能小于0 
            //外核的半径不能小于内核的半径
            if (insideKernelRadius < 0)
            {
                throw new ArgumentException("核的半径不能小于0");
            }
            if (outsideKernelRadius < 0)
            {
                throw new ArgumentException("核的半径不能小于0");
            }
            if (outsideKernelRadius < insideKernelRadius)
            {
                throw new ArgumentException("外核的半径不能小于内核的半径");
            }

            _InsideKernelRadius = insideKernelRadius;
            _InsideBirthValue = insideBirthValue;
            _InsideSurviveValue = insideSurviveValue;
            _OutsideKernelRadius = outsideKernelRadius;
            _OutsideBirthValue = outsideBirthValue;
            _OutsideSurviveValue = outsideSurviveValue;
        }

        public void Iterate()
        {
            var insideConv = ConvolutionCalc(_InsideKernelRadius, _Coords, Shape.Square);
            var outsideConv = ConvolutionCalc(_OutsideKernelRadius, _Coords, Shape.Circle);

            var coordsInRadius = insideConv.Keys.Union(outsideConv.Keys).Distinct();
            var coords = new ConcurrentSet<Vector2>();
            //foreach (var coord in coordsInRadius)
            Parallel.ForEach(coordsInRadius, _ParallelOptions, coord =>
            {
                if (!insideConv.TryGetValue(coord, out float insideValue)) insideValue = 0;
                if (!outsideConv.TryGetValue(coord, out float outsideValue)) outsideValue = 0;
                var valueConv = _Coords.Contains(coord) ? 1 : 0;

                if (valueConv == 0 && _InsideBirthValue.Min <= insideValue && insideValue <= _InsideBirthValue.Max
                    && _OutsideSurviveValue.Min <= outsideValue && outsideValue <= _OutsideSurviveValue.Max)
                {
                    coords.Add(coord);
                }
                else if (_InsideSurviveValue.Min <= insideValue && insideValue <= _InsideSurviveValue.Max
                    && _OutsideSurviveValue.Min <= outsideValue && outsideValue <= _OutsideSurviveValue.Max)
                {
                    coords.Add(coord);
                }
            }
            );

            _Coords = coords.ToHashSet();
        }

        enum Shape
        {
            Square,
            Circle
        }

        private static ConcurrentDictionary<Vector2, float> ConvolutionCalc(float kernelRadius, HashSet<Vector2> coords, Shape kernelshape)
        {
            var radiusSquared = kernelRadius * kernelRadius;

            ConcurrentSet<Vector2> coordsInRadius = []; //被核覆盖的点
            Parallel.ForEach(coords, _ParallelOptions, coord =>
            {
                Vector2 kernelOffset = new();
                for (kernelOffset.X = -kernelRadius; kernelOffset.X <= kernelRadius; kernelOffset.X++)
                {
                    for (kernelOffset.Y = -kernelRadius; kernelOffset.Y <= kernelRadius; kernelOffset.Y++)
                    {
                        coordsInRadius.Add(coord + kernelOffset);
                    }
                }
            });

            ConcurrentDictionary<Vector2, float> convolution = []; //卷积计算结果

            Parallel.ForEach(coordsInRadius, _ParallelOptions, coord =>
            //foreach (var coord in coordsInRadius)
            {

                float sum = 0;
                float weightSum = 0;

                Vector2 kernelOffset = new();
                for (kernelOffset.X = (float)-kernelRadius; kernelOffset.X <= kernelRadius; kernelOffset.X++)
                {
                    for (kernelOffset.Y = (float)-kernelRadius; kernelOffset.Y <= kernelRadius; kernelOffset.Y++)
                    {
                        var offsetCoord = coord + kernelOffset;

                        if (kernelshape == Shape.Circle)
                        {
                            if (Vector2.DistanceSquared(coord, offsetCoord) > radiusSquared)
                            {
                                continue;
                            }
                        }

                        sum += coords.Contains(offsetCoord) ? 1 : 0;
                        weightSum++;
                    }
                }
                var value = sum / weightSum;
                if (value > 0)
                {
                    convolution.GetOrAdd(coord, value);
                }
            }
            );

            return convolution;
        }

        private void SetBit(in (int x, int y) index, in bool value)
        {
            var (x, y) = index;

            if (value)
            {
                if (Bounding != null)
                {
                    var (left, top, right, bottom) = Bounding.Value;
                    if (x < left || x >= right
                        || y < top || y >= bottom)
                        return;
                }

                _Coords.Add(new(x, y));
            }
            else
            {
                _Coords.Remove(new(x, y));
            }
        }
        private bool GetBit(in (int x, int y) index)
        {
            return _Coords.Contains(new(index.x, index.y));
        }

        public bool this[int x, int y]
        {
            get => GetBit((x, y));
            set => SetBit((x, y), value);
        }

        public void Clear() => _Coords.Clear();

        public IEnumerator<Vector2> GetEnumerator() => _Coords.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
