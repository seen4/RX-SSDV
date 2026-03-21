using NWaves.Utils;
using RX_SSDV.Base;
using RX_SSDV.CCSDS.Viterbi;
using RX_SSDV.IO;
using RX_SSDV.Utils;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.CCSDS
{
    public class CCSDSDecoder
    {
        private Viterbi.Viterbi viterbiDecoder0;
        private Viterbi.Viterbi viterbiDecoder1;
        private MDecoder mDecoder0;
        private MDecoder mDecoder1;
        private Deframer deframer0;
        private Deframer deframer1;
        private BitDelay delay;

        //private PackAndOutputBits output1;
        //private PackAndOutputBits output2;

        private bool useMDecode = false;
        private int deframerPacketSize = 223;

        private float[] inputBuffer;
        private float[] outputBuffer;

        private float[] hardDecisionBits;

        public const int DIGITAL_BUFFER_SIZE = 1024;

        public CCSDSDecoder(bool useMDecode)
        {
            this.useMDecode = useMDecode;
            InitProcessingFlow();
            CheckProcessOutputArr(DIGITAL_BUFFER_SIZE);

            //SampleSource.onStart += OpenOutputStream;
            //SampleSource.onStop += CloseOutputStream;
        }

        public void InitProcessingFlow()
        {
            viterbiDecoder0 = new Viterbi.Viterbi();
            viterbiDecoder1 = new Viterbi.Viterbi();
            mDecoder0 = new MDecoder();
            mDecoder1 = new MDecoder();
            deframer0 = new Deframer(deframerPacketSize);
            deframer1 = new Deframer(deframerPacketSize);

            //output1 = new PackAndOutputBits("C:\\Users\\AstarLC\\Desktop\\Documents\\misc\\test_out_viterbi_1.bin");
            //output2 = new PackAndOutputBits("C:\\Users\\AstarLC\\Desktop\\Documents\\misc\\test_out_viterbi_2.bin");

            delay = new BitDelay(1);
        }

        private void ConfigureOutput()
        {
            float[] temp = outputBuffer;
            outputBuffer = inputBuffer;
            inputBuffer = temp;
        }

        public static void HardDecision(float[] inputSamplesI, float[] inputSamplesQ, float[] outputBits, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }

            for (int i = 0; i < inputSize; i++)
            {
                outputBits[i] = inputSamplesI[i] > 0 ? 1 : 0;
            }
        }

        public void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outputBits, out int outputSize, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }
            
            HardDecision(inputSamplesI, inputSamplesQ, hardDecisionBits, inputSize);

            //Branch Normal
            int outputSize0 = viterbiDecoder0.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            if (useMDecode) { outputSize0 = mDecoder0.Process(outputSize0, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer0.Process(outputSize0, inputBuffer, outputBuffer); ConfigureOutput();

            //Branch Delay
            delay.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            int outputSize1 = viterbiDecoder1.Process(inputSize, inputBuffer, outputBuffer); ConfigureOutput();
            if (useMDecode) { outputSize1 = mDecoder1.Process(outputSize1, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer1.Process(outputSize1, inputBuffer, outputBuffer);

            //Output
            outputSize = outputSize1;
            outputBuffer.FastCopyTo(outputBits, outputSize, 0, 0);
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outputBuffer, arrSize))
            {
                outputBuffer = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(inputBuffer, arrSize))
            {
                inputBuffer = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(hardDecisionBits, arrSize))
            {
                hardDecisionBits = new float[arrSize];
            }
        }

        //public void OpenOutputStream()
        //{
        //    output1.OpenStream();
        //    output2.OpenStream();
        //}

        //public void CloseOutputStream()
        //{
        //    output1.CloseStream();
        //    output2.CloseStream();
        //}
    }
}
