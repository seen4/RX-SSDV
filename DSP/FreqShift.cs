using System;

namespace RX_SSDV.DSP
{
	public class FreqShift : DspBlock
	{
		int samplerate;
		public int Samplerate
		{
			get { return samplerate; }
			set { SetSamplerate(value); }
		}

		float freq;
		public float Freq
		{
			get { return freq; }
			set { SetFreq(value); }
		}


        float t;
		float secondPerSample;
		float T;

		public FreqShift(int samplerate, float freq)
		{
			this.samplerate = samplerate;
			this.freq = freq;
			T = 1 / freq;
			secondPerSample = 1f / samplerate;
		}

		public void SetFreq(float freq)
		{
            this.freq = freq;
            T = 1 / freq;
        }

        public void SetSamplerate(int samplerate)
        {
            this.samplerate = samplerate;
            secondPerSample = 1f / samplerate;
        }

        public override int Process(int inputLength, float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
		{
			base.Process(inputSamplesI, inputSamplesQ, outputSamplesI, outputSamplesQ, inputLength);

			for (int i = 0; i < inputLength; i++) 
			{
				t += secondPerSample;
				if (t >= T)
				{
					t -= T;
				}

				//omega = (2 * pi) / T
				float theta = 2 * MathF.PI * (t / T);
				float oscI = MathF.Cos(theta);
				float oscQ = MathF.Sin(theta);

				float inputI = inputSamplesI[i];
                float inputQ = inputSamplesQ[i];

                outputSamplesI[i] = inputI * oscI - inputQ * oscQ;
                outputSamplesQ[i] = inputI * oscQ + inputQ * oscI;
            }

			return inputLength;
        }
	}
}