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
        private Viterbi viterbi;
        private int[] newPathDst;
        private int statusCount;

        private int[,] branchOutputs;

        public List<int> statusList;
        public int[] pathDst;
        public int StatusCount => statusCount;

        public (int, int) MinPath
        {
            get
            {
                int index = -1;
                int min = int.MaxValue;
                for(int i = 0; i < pathDst.Length; i++)
                {
                    if(pathDst[i] < min)
                    {
                        min = pathDst[i];
                        index = i;
                    }
                }
                return (index, min);
            }
        }

        public Trellis(Viterbi viterbi) 
        {
            this.viterbi = viterbi;
            statusCount = 1 << (viterbi.Constraint - 1); //count = (constraint - 1) ^ 2
            Init();
        }

        /// <summary>
        /// Init <see cref="Trellis"/>
        /// </summary>
        public void Init()
        {
            statusList = new List<int>(statusCount * 4096);
            pathDst = new int[statusCount];
            newPathDst = new int[statusCount];
            CalcOutputs();

            for (int i = 1; i < pathDst.Length; i++)
                pathDst[i] = int.MinValue / 2;
        }

        private void CalcOutputs()
        {
            branchOutputs = new int[statusCount, 2];

            int poly1 = 0b_1101101;
            int poly2 = 0b_1001111;

            for(int state = 0; state < statusCount; state++)
            {
                for(int input = 0; input < 2; input++)
                {
                    int s = (state << 1) | input;

                    int output1 = Parity(s & poly1);
                    int output2 = Parity(s & poly2);

                    branchOutputs[state, input] = (output1 << 1) | output2;
                }
            }
        }

        /// <summary>
        /// Clear trellis.
        /// </summary>
        public void ClearTrellis()
        {
            statusList.Clear();
            Array.Clear(pathDst, 0, pathDst.Length);
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

            int[] temp = pathDst;
            pathDst = newPathDst;
            newPathDst = temp;
        }

        /// <summary>
        /// Find the surviving path between input bits and a status.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="status">Status</param>
        /// <returns>The minimum Hamming distance and the status</returns>
        public ValueTuple<int, int> SurvivingPath(byte bits, int status)
        {
            (int, int) nextStatuses = CalcSourceStatuses(status, viterbi.Constraint);
            int sourceStatus1 = nextStatuses.Item1;
            int sourceStatus2 = nextStatuses.Item2;

            int pd1 = pathDst[sourceStatus1] + HammingDst(bits, sourceStatus1, 0);
            int pd2 = pathDst[sourceStatus2] + HammingDst(bits, sourceStatus2, 1);

            return pd1 <= pd2 ? (pd1, sourceStatus1) : (pd2, sourceStatus2);
        }

        /// <summary>
        /// Calculate Hamming distance between input bits and current status.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="curStatus">Current status</param>
        /// <param name="input">New bit of status</param>
        /// <returns>The Hamming distance</returns>
        public int HammingDst(byte bits, int curStatus, int input)
        {
            int status = branchOutputs[curStatus, input];

            //Read code
            byte rx1 = ReadInt(bits, 1);
            byte rx2 = ReadInt(bits, 2);
            byte tx1 = ReadInt(status, 1);
            byte tx2 = ReadInt(status, 2);

            //Calcucate Hamming dst
            int dst = 0;
            dst += rx1 == tx1 ? 0 : 1;
            dst += rx2 == tx2 ? 0 : 1;

            return dst;
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

        /// <summary>
        /// Calculate source statuses by given status int.
        /// <param name="status">The status</param>
        /// <param name="constraint">Constraint of the status</param>
        /// <returns>Next statuses</returns>
        /// </summary>
        public static (int, int) CalcSourceStatuses(int status, int constraint)
        {
            int s = status >> 1;
            //return (s | (0 << (constraint - 2)), s | (1 << (constraint - 2));
            return (s | 0, s | (1 << (constraint - 2)));
        }
    }
}
