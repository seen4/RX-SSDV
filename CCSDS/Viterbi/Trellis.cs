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
            private int constraint = 6;
            private int[] nextStatuses = new int[2];

            public Status(int status, int constraint)
            {
                this.status = status;
                this.constraint = constraint;
                CalcNextStatuses();
            }

            public Status(Status lastStatus, int input)
            {
                if (input < 0 || input > 1)
                    throw new ArgumentOutOfRangeException("input must equals 0 or 1");

                status = lastStatus.nextStatuses[input];
                constraint = lastStatus.constraint;
                CalcNextStatuses();
            }

            /// <summary>
            /// Calculate next statuses.
            /// </summary>
            private void CalcNextStatuses()
            {
                (int, int) nextStatues = CalcNextStatuses(status, constraint);
                nextStatuses[0] = nextStatues.Item1;
                nextStatuses[1] = nextStatues.Item2;
            }

            /// <summary>
            /// Calculate next statuses by given status int.
            /// <param name="status">The status</param>
            /// <param name="constraint">Constraint of the status</param>
            /// <returns>Next statuses</returns>
            /// </summary>
            public static (int, int) CalcNextStatuses(int status, int constraint)
            {
                int s = (status & ((1 << (constraint - 2)) - 1)) << 1;
                return (s | 0, s | 1);
            }

            //Convolutionly code (n,k,N) = (2,1,7) ONLY! (IEEE 802.11 Standard)
            /// <summary>
            /// Calculate convolutional code by the input value.
            /// </summary>
            /// <param name="inputCode">Input value</param>
            /// <returns>The convolutional code</returns>
            /// <exception cref="ArgumentOutOfRangeException">when 'inputCode' not equals 0 or 1</exception>
            public int CalcCode217(int inputCode)
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

            //Convolutionly code (n,k,N) = (2,1,7) ONLY! (IEEE 802.11 Standard)
            /// <summary>
            /// Calculate convolutional code by the input value.
            /// </summary>
            /// <param name="status">The status</param>
            /// <param name="inputCode">Input value</param>
            /// <returns>The convolutional code</returns>
            public static int CalcCode217(int status, int inputCode)
            {
                Status s = new Status(status, 7);
                return s.CalcCode217(inputCode);
            }

            /// <summary>
            /// Get next status by the input.
            /// </summary>
            /// <param name="input">Input value</param>
            /// <returns>Next status</returns>
            /// <exception cref="ArgumentOutOfRangeException">When 'input' not equals 0 or 1</exception>
            public Status GetNextStatus(int input)
            {
                if (input < 0 || input > 1)
                    throw new ArgumentOutOfRangeException("input must equals 0 or 1");

                return new Status(status, input);
            }
        }

        private Viterbi viterbi;
        private List<int> statusList;
        private int[] pathDst;
        private int[] newPathDst;

        public Trellis(Viterbi viterbi) 
        {
            this.viterbi = viterbi;
            Init();
        }

        /// <summary>
        /// Init <see cref="Trellis"/>
        /// </summary>
        public void Init()
        {
            statusList = new List<int>();
            pathDst = new int[1 << (viterbi.Constraint - 1)]; //size = (constraint - 1) ^ 2
            newPathDst = new int[1 << (viterbi.Constraint - 1)];
        }

        /// <summary>
        /// Clear trellis.
        /// </summary>
        public void ClearTrellis()
        {
            statusList.Clear();
            //Array.Clear(pathDst, 0, pathDst.Length);
            Array.Clear(newPathDst, 0, pathDst.Length);
        }

        /// <summary>
        /// Update surviving path.
        /// </summary>
        /// <param name="bits">Input bits</param>
        public void UpdateSurvivingPath(byte bits)
        {
            for(int i = 0; i < pathDst.Length; i++)
            {
                (int, int) p = SurvivingPath(bits, i);
                newPathDst[i] = p.Item1;
                statusList.Add(p.Item2);
            }
        }

        /// <summary>
        /// Find the surviving path between input bits and a status.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="status">Status</param>
        /// <returns>The minimum Hamming distance and the status</returns>
        public (int, int) SurvivingPath(byte bits, int status)
        {
            (int, int) nextStatuses = Status.CalcNextStatuses(status, viterbi.Constraint);
            int nextStatus1 = nextStatuses.Item1;
            int nextStatus2 = nextStatuses.Item2;

            int pd1 = pathDst[nextStatus1] + HammingDst(bits, status, 0);
            int pd2 = pathDst[nextStatus2] + HammingDst(bits, status, 1);

            return pd1 <= pd2 ? (pd1, nextStatus1) : (pd2, nextStatus2);
        }

        /// <summary>
        /// Calculate Hamming distance between input bits and current status.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="curStatus">Current status</param>
        /// <param name="input">New bit of status</param>
        /// <returns>The Hamming distance</returns>
        public static int HammingDst(byte bits, int curStatus, int input)
        {
            int status = Status.CalcCode217(curStatus, input);

            //Read code
            byte input1 = ReadInt(bits, 1);
            byte input2 = ReadInt(bits, 2);
            byte code1 = ReadInt(status, 1);
            byte code2 = ReadInt(status, 2);

            //Calcucate Hamming dst
            int dst = 0;
            dst += input1 - code1 >= 0 ? input1 - code1 : -input1 + code1; //Abs(input1 - code1)
            dst += input2 - code2 >= 0 ? input2 - code2 : -input2 + code2;

            return dst;
        }
    }
}
