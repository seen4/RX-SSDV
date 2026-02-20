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
        private int stateCount;

        private int[,] branchOutputs;
        public int[,] BranchOutputs => branchOutputs;

        public List<int> stateList;
        public int[] pathDst;
        public int StateCount => stateCount;

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
            stateCount = 1 << (viterbi.Constraint - 1); //count = (constraint - 1) ^ 2
            Init();
        }

        /// <summary>
        /// Init <see cref="Trellis"/>
        /// </summary>
        public void Init()
        {
            stateList = new List<int>(stateCount * 4096);
            pathDst = new int[stateCount];
            newPathDst = new int[stateCount];
            CalcOutputs();

            for (int i = 1; i < pathDst.Length; i++)
                pathDst[i] = int.MinValue / 2;
        }

        /// <summary>
        /// Calculate the states table.
        /// </summary>
        private void CalcOutputs()
        {
            branchOutputs = new int[stateCount, 2];

            //Polynomials of (2,1,7) convolutional code, IEEE 802.11 standard
            int poly1 = 0b_1101101;
            int poly2 = 0b_1001111;

            for(int state = 0; state < stateCount; state++)
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
            stateList.Clear();
            Array.Clear(pathDst, 0, pathDst.Length);
            Array.Clear(newPathDst, 0, pathDst.Length);
        }

        public void CleanUpTrellis()
        {
            stateList.Clear();
            Array.Clear(newPathDst, 0, pathDst.Length);

            int minPath = MinPath.Item2;
            for(int i = 0; i < pathDst.Length; i++)
            {
                pathDst[i] -= minPath;
            }
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
                stateList.Add(p.Item2);
            }

            int[] temp = pathDst;
            pathDst = newPathDst;
            newPathDst = temp;
        }

        /// <summary>
        /// Find the surviving path between input bits and a state.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="state">state</param>
        /// <returns>The minimum Hamming distance and the state</returns>
        public ValueTuple<int, int> SurvivingPath(byte bits, int state)
        {
            (int, int) nextstatees = CalcSourcestates(state, viterbi.Constraint);
            int sourcestate1 = nextstatees.Item1;
            int sourcestate2 = nextstatees.Item2;

            int pd1 = pathDst[sourcestate1] + HammingDst(bits, sourcestate1, 0);
            int pd2 = pathDst[sourcestate2] + HammingDst(bits, sourcestate2, 1);

            return pd1 <= pd2 ? (pd1, sourcestate1) : (pd2, sourcestate2);
        }

        /// <summary>
        /// Calculate Hamming distance between input bits and current state.
        /// </summary>
        /// <param name="bits">Input bits</param>
        /// <param name="curstate">Current state</param>
        /// <param name="input">New bit of state</param>
        /// <returns>The Hamming distance</returns>
        public int HammingDst(byte bits, int curstate, int input)
        {
            int state = branchOutputs[curstate, input];

            //Read code
            byte rx1 = ReadInt(bits, 1);
            byte rx2 = ReadInt(bits, 2);
            byte tx1 = ReadInt(state, 1);
            byte tx2 = ReadInt(state, 2);

            //Calcucate Hamming dst
            int dst = 0;
            dst += rx1 == tx1 ? 0 : 1;
            dst += rx2 == tx2 ? 0 : 1;

            return dst;
        }

        /// <summary>
        /// Calculate next statees by given state int.
        /// <param name="state">The state</param>
        /// <param name="constraint">Constraint of the state</param>
        /// <returns>Next statees</returns>
        /// </summary>
        public static (int, int) CalcNextstates(int state, int constraint)
        {
            int s = (state & ((1 << (constraint - 2)) - 1)) << 1;
            return (s | 0, s | 1);
        }

        /// <summary>
        /// Calculate source statees by given state int.
        /// <param name="state">The state</param>
        /// <param name="constraint">Constraint of the state</param>
        /// <returns>Next statees</returns>
        /// </summary>
        public static (int, int) CalcSourcestates(int state, int constraint)
        {
            int s = state >> 1;
            //return (s | (0 << (constraint - 2)), s | (1 << (constraint - 2));
            return (s | 0, s | (1 << (constraint - 2)));
        }
    }
}
