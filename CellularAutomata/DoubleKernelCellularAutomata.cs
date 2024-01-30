using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomata
{
    internal class DoubleKernelCellularAutomata : IEnumerable<(int, int)>
    {
        public record ValueRange(double minFirstKernel, double maxFirstKernel)

        public DoubleKernelCellularAutomata(double firstKernelRadius, double minFirstKernel, double maxFirstKernel,
            double secondKernelRadius, double minSecondKernel, double maxSecondKernel) 
        {
        
        
        }


        public IEnumerator<(int, int)> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
