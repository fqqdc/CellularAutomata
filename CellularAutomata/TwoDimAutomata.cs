using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace CellularAutomata
{
    class TwoDimAutomata : IEnumerable<(int, int)>
    {
        private readonly BitArray _RuleNumber;
        private HashSet<(int x, int y)> _Data = [];

        private readonly bool _OutsizeValue;
        private (int left, int top, int right, int bottom)? _Bounding;

        readonly static int BitArraySize = (int)Math.Pow(2, 9);

        public TwoDimAutomata(in BitArray ruleNumber,
            (int left, int top, int right, int bottom)? bounding = null, in bool outsizeValue = false)
        {
            if (ruleNumber.Length != BitArraySize)
                throw new ArgumentOutOfRangeException(nameof(ruleNumber));
            if (bounding == null && ruleNumber[0])
                throw new ArgumentOutOfRangeException(nameof(ruleNumber), "Will cause uncountable unit changes.");

            _RuleNumber = new(ruleNumber);
            _OutsizeValue = outsizeValue;
            _Bounding = bounding;
        }

        public TwoDimAutomata(in HashSet<int> birth, in HashSet<int> survive,
            (int left, int top, int right, int bottom)? bounding = null, in bool outsizeValue = false)
        {
            HashSet<int> vaildNumber = [.. Enumerable.Range(0, 9)];

            if (birth.Except(vaildNumber).Any())
                throw new ArgumentOutOfRangeException(nameof(birth));

            if (bounding == null && birth.Contains(0))
                throw new ArgumentOutOfRangeException(nameof(birth), "Will cause uncountable unit changes.");

            if (survive.Except(vaildNumber).Any())
                throw new ArgumentOutOfRangeException(nameof(survive));

            int maskMidUnit = 1 << 4;
            _RuleNumber = new(BitArraySize);
            for (int i = 0; i < _RuleNumber.Length; i++)
            {
                if ((i & maskMidUnit) != 0)
                {
                    int nBit = BitOperations.PopCount((uint)i) - 1;
                    if (survive.Contains(nBit))
                        _RuleNumber[i] = true;
                }
                else
                {
                    int nBit = BitOperations.PopCount((uint)i);
                    if (birth.Contains(nBit))
                        _RuleNumber[i] = true;
                }
            }

            _OutsizeValue = outsizeValue;
            _Bounding = bounding;
        }

        public void Iterate()
        {
            var iterateData = new ConcurrentBag<(int, int)>();

            if (_RuleNumber[0])
            {
                Debug.Assert(_Bounding != null);
                var (left, top, right, bottom) = _Bounding.Value;
                Parallel.For(left, right + 1, new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                     x =>
                     {
                         for (int y = top; y < bottom; y++)
                         {
                             var index = (x, y);
                             var isSet = Iterate(index);
                             if (isSet)
                                 iterateData.Add(index);
                         }
                     });
            }
            else
            {
                ConcurrentSet<(int, int)> affectedSet = new(_Data);
                Parallel.ForEach(_Data, new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    index =>
                    {
                        var (x, y) = index;

                        affectedSet.Add((x - 1, y - 1));
                        affectedSet.Add((x, y - 1));
                        affectedSet.Add((x + 1, y - 1));

                        affectedSet.Add((x - 1, y));
                        //affectedSet.Add((x, y));
                        affectedSet.Add((x + 1, y));

                        affectedSet.Add((x - 1, y + 1));
                        affectedSet.Add((x, y + 1));
                        affectedSet.Add((x + 1, y + 1));
                    });

                Parallel.ForEach(affectedSet, new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    index =>
                    {
                        if (_Bounding != null)
                        {
                            var (x, y) = index;
                            var (left, top, right, bottom) = _Bounding.Value;
                            if (x < left || x >= right
                                || y < top || y >= bottom)
                                return;
                        }
                        var isSet = Iterate(index);
                        if (isSet)
                            iterateData.Add(index);
                    });
            }

            _Data = iterateData.ToHashSet();
        }

        private void SetBit(in (int x, int y) index, in bool value)
        {
            if (value)
            {
                if (_Bounding != null)
                {
                    var (x, y) = index;
                    var (left, top, right, bottom) = _Bounding.Value;
                    if (x < left || x >= right
                        || y < top || y >= bottom)
                        return;
                }

                _Data.Add(index);
            }
            else
            {
                _Data.Remove(index);
            }
        }

        private bool GetBit(in (int x, int y) index)
        {
            return _Data.Contains(index);
        }

        private bool Iterate(in (int x, int y) index)
        {
            var (x, y) = index;

            bool[] bits =
            [
                GetBit((x - 1, y - 1)),
                GetBit((x, y - 1)),
                GetBit((x + 1, y - 1)),

                GetBit((x - 1, y)),
                GetBit((x, y)),
                GetBit((x + 1, y)),

                GetBit((x - 1, y + 1)),
                GetBit((x, y + 1)),
                GetBit((x + 1, y + 1)),
            ];

            int number = 0;
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i]) number |= 1 << i;
            }

            return _RuleNumber[number];
        }

        public BitArray RuleNumber => new(_RuleNumber);
        public bool this[int x, int y]
        {
            get => GetBit((x, y));
            set => SetBit((x, y), value);
        }
        public void Clear() => _Data.Clear();
        public int Count => _Data.Count;

        IEnumerator<(int, int)> IEnumerable<(int, int)>.GetEnumerator() => _Data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _Data.GetEnumerator();

    }

}
