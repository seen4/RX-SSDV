using RX_SSDV.Base;
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
        //Polynomials of (2,1,7) convolutional code, IEEE 802.11 standard

        /* POLY1    POLY2    NAME
         * 
         * 79       -109     CCSDS
         * -109     79       NASA-DSN
         * 79       109      CCSDS uninverted
         * 109      79       NASA-DSN uninverted
         */

        //Use reversed 'NASA-DSN uninverted' to adapt the 'Encode CCSDS 27' block of GNU Radio. (idk why)

        /* For origin 'cc_decode'
         * poly1 = 0b_0111_1001;
         * poly2 = 0b_0101_1011;
         * 
         * For origin 'Encode CCSDS 27'
         * poly1 = 0b_0101_1011;
         * poly2 = 0b_0111_1001;
         */

        public const int poly1 = 0b_0101_1011; //0b_0101_1011
        public const int poly2 = 0b_0111_1001; //0b_0111_1001

        private int n = 1;
        public int AdderCount => n;

        private int k = 1;
        public int InputCount => k;

        private int constraint = 1;
        public int Constraint => constraint;

        private int constraintLength = 1;
        public int ConstraintLength => constraintLength;

        private int stateCount = 0;
        public int StateCount => stateCount;

        private int[,] branchOutputs;
        public int[,] BranchOutputs => branchOutputs;

        public Trellis trellis;

        public Viterbi() 
        {
            n = 2;
            k = 1;
            constraint = 7;
            constraintLength = 14;
            stateCount = 1 << 6; //count = Pow(2, (constraint - 1))

            trellis = new Trellis(this);
            CalcOutputs();
        }

        //Don't use this or use param (2,1,7)
        [Obsolete("Only supports (n = 2, k = 1, N = 7) convolutional codes")]
        public Viterbi(int n, int k, int N)
        {
            this.n = n;
            this.k = k;
            constraint = N;
            constraintLength = N * n;
            stateCount = 1 << (N - 1);

            trellis = new Trellis(this);
            CalcOutputs();
        }

        public override int Process(int inputSize, float[] inputArr, float[] outputArr)
        {
            base.Process(inputArr, outputArr, inputSize);
            int outputSize = 0, processedCount = 0;

            //Generate trellis
            for (int i = 0; i < historyBuffer.Length; i += n)
            {
                if (i + 1 > historyBuffer.Length - 1)
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
                int input = ReadInt(state, 6); //Read input code from current state
                outputArr[outputSize] = input; //Output
                state = trellis.stateList[i + state]; //Find previous state of current state 

                outputSize++;
            }

            trellis.CleanUpTrellis();
            Array.Reverse(outputArr, 0, outputSize);

            CompleteProcess(processedCount);
            return outputSize;
        }

        /// <summary>
        /// Calculate the states table.
        /// </summary>
        private void CalcOutputs()
        {
            branchOutputs = new int[stateCount, 2];

            for (int state = 0; state < stateCount; state++)
            {
                for (int input = 0; input < 2; input++)
                {
                    int s = state | (input << (constraint - 1));

                    int absPoly1 = Math.Abs(poly1);
                    int absPoly2 = Math.Abs(poly2);
                    int raw1 = Parity(s & absPoly1);
                    int raw2 = Parity(s & absPoly2);
                    int output1 = poly1 < 0 ? raw1 ^ 1 : raw1;
                    int output2 = poly2 < 0 ? raw2 ^ 1 : raw2;

                    branchOutputs[state, input] = (output1 << 1) | output2;
                }
            }
        }
    }
}
