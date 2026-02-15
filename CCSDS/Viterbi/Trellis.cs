using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RX_SSDV.Utils.BinaryUtils;

namespace RX_SSDV.CCSDS.Viterbi
{
    public class Trellis
    {
        public struct Status
        {
            private int status = 0b_0000_0000;
            public int StatusCode => status;
            private int length = 6;
            private int[] nextStatuses = new int[2];

            public Status(int status, int length)
            {
                this.status = status;
                this.length = length;
                CalcNextStatus();
            }

            public Status(Status lastStatus, int input)
            {
                if (input < 0 || input > 1)
                    throw new ArgumentOutOfRangeException("input must equals 0 or 1");

                status = lastStatus.nextStatuses[input];
                length = lastStatus.length;
                CalcNextStatus();
            }

            private void CalcNextStatus()
            {
                nextStatuses[0] = status << 1;
                nextStatuses[1] = (status << 1) + 1;
            }

            //Convolutionly code (n,k,N) = (2,1,7) ONLY! (IEEE 802.11 Standard)
            public int CalcCode(int inputCode)
            {
                if (inputCode < 0 || inputCode > 1)
                    throw new ArgumentOutOfRangeException("inputCode must equals 0 or 1");

                /*  [0]   [1]   [2]   [3]   [4]   [5]   [6] (length = N = 7)
                 *  new  int#0 int#1 int#2 int#3 int#4 int#5
                 */

                int output1 = 0, output2 = 0;
                output1 += inputCode + ReadInt(status, 2) + ReadInt(status, 3) + ReadInt(status, 5) + ReadInt(status, 6);
                output1 += inputCode + ReadInt(status, 1) + ReadInt(status, 2) + ReadInt(status, 3) + ReadInt(status, 6);
                output1 = output1 % 2 == 0 ? 0 : 1;
                output2 = output2 % 2 == 0 ? 0 : 1;
                int code = output1;
                code <<= 1;
                code += output2;

                return code;
            }

            public Status GetNextStatus(int input)
            {
                if (input < 0 || input > 1)
                    throw new ArgumentOutOfRangeException("input must equals 0 or 1");

                return new Status(status, input);
            }
        }

        private Viterbi viterbi;

        private Status lastStatus;
        public Status LastStatus => lastStatus;

        private bool isInited = false;
        public bool IsInited => isInited;

        public Trellis(Viterbi viterbi) 
        {
            this.viterbi = viterbi;
            Init();
        }

        public void Init()
        {
            this.isInited = true;
            lastStatus = GetInitalStatus(0);
        }

        public void Init(int initalValue)
        {
            this.isInited = true;
            lastStatus = GetInitalStatus(initalValue);
        }

        public void Add(int value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentOutOfRangeException("value must equals 0 or 1");

            lastStatus = lastStatus.GetNextStatus(value);
        }

        private Status GetInitalStatus(int initalValue)
        {
            return new Status(initalValue, viterbi.Constraint - 1);
        }

        public void ResetTrellis()
        {
            isInited = false;
        }
    }
}
