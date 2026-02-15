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

        private Trellis trellis;

        public Viterbi() 
        {
            n = 2;
            k = 1;
            constraint = 7;
            constraintLength = 14;

            trellis = new Trellis(this);
        }

        //Don't use this or use param (2,1,7)
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

            for (int i = 0; i < inputSize; i += n)
            {
                if (i + 1 > inputSize - 1)
                    break;

                //Update counter
                processedCount += n;
                outputSize++;

                //Get input
                int input1 = (int)inputArr[i];
                int input2 = (int)inputArr[i + 1];

                int hammingDst = 0b_1000_0000; //Just a big number
                int survivingPath = -1;

                //Get surviving path
                for (int j = 0; j < 2; j++)
                {
                    int status = trellis.LastStatus.CalcCode(j);

                    //Read code from status
                    byte code1 = ReadInt(status, 1);
                    byte code2 = ReadInt(status, 2);

                    //Calcucate hamming dst
                    int dst = 0;
                    dst += input1 - code1 > 0 ? input1 - code1 : -input1 + code1; //Abs(input1 - code1)
                    dst += input2 - code2 > 0 ? input2 - code2 : -input2 + code2;

                    if (dst < hammingDst)
                    {
                        hammingDst = dst;
                        survivingPath = j;
                    }
                }

                //To next status
                trellis.Add(survivingPath);

                outputArr[outputSize - 1] = survivingPath;
            }

            CompleteProcess(processedCount);
            return outputSize;
        }
    }
}
