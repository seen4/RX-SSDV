using RX_SSDV.DSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static RX_SSDV.Utils.BinaryUtils;

namespace RX_SSDV.CCSDS.Viterbi
{
    /// <summary>
    /// Viterbi for (n,k,N) = (2,1,7) convolutional code.
    /// </summary>
    public class Viterbi : DigitalProcessingBlock
    {
        
        private int n = 1;
        public int AdderCount => n;

        private int k = 1;
        public int InputCount => k;

        private int constraint = 1;
        public int Constraint => constraint;

        private int constraintLength = 1;
        public int ConstraintLength => constraintLength;

        public Trellis trellis;

        public Viterbi() 
        {
            n = 2;
            k = 1;
            constraint = 7;
            constraintLength = 14;

            trellis = new Trellis(this);
        }

        //Don't use this or use param (2,1,7)
        [Obsolete("Only supports (n = 2, k = 1, N = 7) convolutional codes")]
        public Viterbi(int n, int k, int N)
        {
            this.n = n;
            this.k = k;
            constraint = N;
            constraintLength = N * n;

            trellis = new Trellis(this);
        }

        public override int Process(int inputSize, float[] inputArr, float[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);
            int outputSize = 0, processedCount = 0;

            //Generate trellis
            for (int i = 0; i < inputSize; i += n)
            {
                if (i + 1 > inputSize - 1)
                    break;

                //Get input
                byte input1 = (byte)historyBuffer[i];
                byte input2 = (byte)historyBuffer[i + 1];
                byte bits = (byte)((input1 << 1) + input2);

                //Update surviving path
                trellis.UpdateSurvivingPath(bits);

                //Update counter
                processedCount += n;
            }

            //Traceback
            int state = trellis.MinPath.Item1;
            for (int i = trellis.stateList.Count - trellis.StateCount; i >= 0; i -= trellis.StateCount)
            {
                int input = ReadInt(state, 1); //Read input code from current state
                outputArr[outputSize] = input; //Output
                state = trellis.stateList[i + state]; //Find previous state of current state 

                outputSize++;
            }

            trellis.CleanUpTrellis();
            Array.Reverse(outputArr, 0, outputSize);

            CompleteProcess(processedCount);
            return outputSize;
        }
    }
}
