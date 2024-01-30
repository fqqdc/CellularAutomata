using System.Collections;

namespace CellularAutomata
{
    class OneDimAutomata
    {
        private readonly int _RuleNumber;
        private readonly int[] _DataBuffer;
        private readonly int _DataSize;
        private readonly int _SpaceSize;
        private readonly bool _OutsizeValue;

        const int UnitBits = 32;

        public OneDimAutomata(in int ruleNumber, in int spaceSize, in bool outsizeValue)
        {
            if (ruleNumber < 0 || ruleNumber > Math.Pow(2, Math.Pow(2, 3)))
                throw new ArgumentOutOfRangeException(nameof(ruleNumber));

            _RuleNumber = ruleNumber;
            _OutsizeValue = outsizeValue;

            var valuesLength = spaceSize / UnitBits;
            var remainder = spaceSize % UnitBits;
            if (remainder > 0) valuesLength += 1;

            _DataSize = valuesLength;
            _DataBuffer = new int[_DataSize * 2];
            _SpaceSize = spaceSize;
        }

        public void Iterate()
        {
            var iterateData = _DataBuffer.AsSpan(_DataSize);
            iterateData.Clear();

            Parallel.For(0, _SpaceSize, new() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                index => SetBit(index, Iterate(index), _DataSize));

            iterateData.CopyTo(_DataBuffer);
        }

        private void SetBit(in int index, in bool value, int baseDataIndex = 0)
        {
            if ((uint)index >= (uint)_SpaceSize)
                throw new ArgumentOutOfRangeException(nameof(index));

            var data = _DataBuffer.AsSpan(baseDataIndex..(baseDataIndex + _DataSize));

            int bitMask = 1 << index;
            ref int segment = ref data[index >> 5];

            if (value)
            {
                segment |= bitMask;
            }
            else
            {
                segment &= ~bitMask;
            }
        }

        private bool GetBit(in int index, int baseIndex = 0)
        {
            if ((uint)index >= (uint)_SpaceSize)
                throw new ArgumentOutOfRangeException(nameof(index));
            var data = _DataBuffer.AsSpan(baseIndex..(baseIndex + _DataSize));

            return (data[index >> 5] & (1 << index)) != 0;
        }

        private bool Iterate(in int index)
        {
            bool bit0 = index - 1 < 0 ? _OutsizeValue : GetBit(index - 1);
            bool bit1 = GetBit(index);
            bool bit2 = index + 1 == _SpaceSize ? _OutsizeValue : GetBit(index + 1);

            int number = (bit2 ? 1 : 0) << 2 | (bit1 ? 1 : 0) << 1 | (bit0 ? 1 : 0);

            int value = _RuleNumber & (1 << number);
            return value != 0;
        }

        public int IntSize => _DataSize;
        public int[] Data => _DataBuffer[0.._DataSize];
        public void CopyTo(Span<int> destination) => _DataBuffer.AsSpan(0, _DataSize).CopyTo(destination);
        public void SetValues(Span<int> values) => values.CopyTo(_DataBuffer.AsSpan(0, _DataSize));
        public int RuleNumber => _RuleNumber;
    }

}
