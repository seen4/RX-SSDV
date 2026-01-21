using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV
{
    public interface IDspBlock
    {
        public void Process(float[] inputI, float[] inputQ, float[] outputI, float[] outputQ);
    }
}
