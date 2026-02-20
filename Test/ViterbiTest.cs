using RX_SSDV.Base;
using RX_SSDV.CCSDS.Viterbi;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RX_SSDV.Test
{
    public class ViterbiTest
    {
        public Viterbi viterbi  = new Viterbi();
        public float[] inputDataArray;
        public float[] inputArray;
        public float[] outputArray;
        public int outputSize;
        public int sampleCount = 320;

        private int generatorState = 0;

        public void Test()
        {
            Logger.LogInfo("Running Viterbi Test...");

            Logger.LogInfo("GROUP 1");
            GenerateInput(10);
            Decode();
            PrintResult();

            Logger.LogInfo("GROUP 2");
            GenerateInput(1);
            Decode();
            PrintResult();

            Logger.LogInfo("GROUP RAND 1");
            GenerateInputRand();
            Decode();
            PrintResult();

            Logger.LogInfo("GROUP RAND 2");
            GenerateInputRand();
            Decode();
            PrintResult();
        }

        public void PrintResult()
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;

            Logger.LogInfo("Input array");
            for (int i = 0; i < inputArray.Length; i++)
            {
                sb.Append(inputArray[i]);
                count++;

                if (count > 32)
                {
                    count = 0;
                    Logger.LogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            count = 0;
            sb.Clear();
            Logger.LogInfo($"Input data array");
            for (int i = 0; i < sampleCount; i++)
            {
                sb.Append(inputDataArray[i]);
                count++;

                if (count > 32)
                {
                    count = 0;
                    Logger.LogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            count = 0;
            sb.Clear();

            int err = 0;
            Logger.LogInfo($"Output array");
            for (int i = 0; i < outputSize; i++)
            {
                if (inputDataArray[i] != outputArray[i])
                    err++;

                sb.Append(outputArray[i]);
                count++;

                if (count > 32)
                {
                    count = 0;
                    Logger.LogInfo(sb.ToString());
                    sb.Clear();
                }
            }

            Logger.LogInfo($"Errors: {err}/{inputDataArray.Length}, Error rate: { ((float)err / inputDataArray.Length) * 100}%");
        }

        public void GenerateInput(int modPtr = 10)
        {
            inputArray = new float[sampleCount * 2];
            inputDataArray = new float[sampleCount];
            //generatorState = 0;

            int decisionPtr = 0;
            for (int i = 0; i < sampleCount * 2; i+=2)
            {
                int mod = decisionPtr % 2;
                if(i % modPtr == 0)
                    decisionPtr++;

                //Logger.LogInfo(mod.ToString());

                int input = 0;
                if (mod == 0)
                {
                    input = viterbi.BranchOutputs[generatorState, 0];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item1;
                    inputDataArray[i / 2] = 0;

                    //Logger.LogInfo("input 0");
                }
                else
                {
                    input = viterbi.BranchOutputs[generatorState, 1];

                    //Next state
                    (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                    generatorState = nextState.Item2;
                    inputDataArray[i / 2] = 1;
                }

                //Write
                inputArray[i] = BinaryUtils.ReadInt(input, 1);
                inputArray[i + 1] = BinaryUtils.ReadInt(input, 2);
            }
        }

        public void GenerateInputRand()
        {
            inputArray = new float[sampleCount * 2];
            inputDataArray = new float[sampleCount];
            //generatorState = 0;

            Random random = new Random();

            for (int i = 0, j = 0; i < sampleCount * 2; i += 2, j++)
            {
                int rand = random.Next(2);

                int input = 0;

                (int, int) nextState = Trellis.CalcNextStates(generatorState, viterbi.Constraint);
                //Logger.LogInfo($"{Trellis.CalcSourceStates(0b_101011, 7).Item1.ToString("B6")}");

                if (rand == 0)
                {
                    input = viterbi.BranchOutputs[generatorState, 0];
                    generatorState = nextState.Item1;
                    inputDataArray[j] = 0;
                }
                else
                {
                    input = viterbi.BranchOutputs[generatorState, 1];
                    generatorState = nextState.Item2;
                    inputDataArray[j] = 1;
                }

                //Write
                inputArray[i] = BinaryUtils.ReadInt(input, 1);
                inputArray[i + 1] = BinaryUtils.ReadInt(input, 2);
            }
        }

        public void Decode()
        {
            outputArray = new float[sampleCount * 2];

            outputSize = viterbi.Process(inputArray.Length, inputArray, outputArray);
        }
    }
}
