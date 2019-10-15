using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Dsp;


namespace Delay
{
    public partial class Form1 : Form
    {
        WaveFormat waveformat = new WaveFormat(44100, 16, 2);
        BufferedWaveProvider buffer;
        WaveOutEvent output = new WaveOutEvent();
        WaveInEvent input = new WaveInEvent();

        TimeSpan timetoRamp;

        bool targetRampedUp = false;
        bool rampingup = false;
        bool rampingdown = false;
        int targetMs = 20000;
        int rampSpeed = 2;
        int curdelay;
        int buffavg = 0;
        int buffcumulative;
        int buffavgcounter = 0;
        int dumpMs = 0;
        int dumps = 1;
        double silenceThreshold = 0;
        bool quickramp = false;
        int realRampSpeed = 0;
        int realRampFactor = 1; //ramp ramp speed -- set higher for slower ramp ramp
        int endRampTime = 0;
        bool smoothchange = true;
        bool smoothRampEnabled = true;
        bool almostDoneRampingUp = false;
        bool almostDoneRampingDown = false;
        int blinkInterval = 500;
        bool recording = false;
        double avgLevel;
        double peakLevel;


        double Q = (1 / (double)2); // default Q value for low pass filter
        BiQuadFilter[] filter;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                outputSelector.Items.Add($"{caps.ProductName}");
            }
            for (int n = 0; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                inputSelector.Items.Add($"{caps.ProductName}");
            }
            txtTarget.DecimalPlaces = 1;
            buffer = new BufferedWaveProvider(waveformat);
            buffer.BufferDuration = new TimeSpan(0, 15, 0);
            inputSelector.SelectedIndex = 0;
            outputSelector.SelectedIndex = 0;
            dumpMs = (int)(targetMs / txtDumps.Value);
            numericUpDown1.Value = realRampFactor;
            txtThreshold.Value = Convert.ToDecimal(silenceThreshold);
            timer2.Interval = blinkInterval;
            InitializeAudio();
        }

        private void InitializeAudio()
        {
            input.Dispose();
            output.Dispose();
            output = new WaveOutEvent();
            input = new WaveInEvent();
            input.DeviceNumber = inputSelector.SelectedIndex;
            output.DeviceNumber = outputSelector.SelectedIndex;


            input.WaveFormat = waveformat;

            input.DataAvailable += new EventHandler<WaveInEventArgs>(DataAvailable);
            filter = new BiQuadFilter[waveformat.Channels];
            for (int i = 0; i < filter.Length; i++)
            {
                filter[i] = BiQuadFilter.LowPassFilter(waveformat.SampleRate, waveformat.SampleRate / 2, (float)Q);
            }

            output.Init(buffer);
            output.Pause();
            try
            {
                input.StartRecording();
                recording = true;
            }
            catch
            {

            }
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (targetRampedUp && rampingup && curdelay < targetMs && !quickramp)
            {
                var stretchedbuffer = Stretch(e.Buffer, (1.00 + (realRampSpeed / (100.0 * realRampFactor))), silenceThreshold);
                buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                if (smoothRampEnabled)
                {
                    if ((targetMs - curdelay) > endRampTime && !almostDoneRampingUp)
                    {
                        if (realRampSpeed < (rampSpeed * realRampFactor))
                            realRampSpeed++;
                        else if (realRampSpeed > (rampSpeed * realRampFactor))
                            realRampSpeed--;

                        double tempEndRampTime = 0;
                        for (int i = 0; i < realRampSpeed; i++)
                        {
                            tempEndRampTime += 1.000 * i / realRampFactor;
                        }
                        endRampTime = (int)tempEndRampTime;
                    }
                    else
                    {
                        if (realRampSpeed > realRampFactor)
                            realRampSpeed--;
                        else
                            realRampSpeed = realRampFactor;
                        almostDoneRampingUp = true;
                    }
                }
                else
                {
                    realRampSpeed = rampSpeed;
                }

                //ramping = false;
                if (curdelay >= targetMs)
                {
                    rampingup = false;
                    quickramp = false;
                    if (output.PlaybackState == PlaybackState.Paused)
                    {
                        output.Play();
                    }
                }
            }

            else if ((curdelay > targetMs || !targetRampedUp) && rampingdown && curdelay > output.DesiredLatency)
            {
                //Ramp down to the target
                var stretchedbuffer = Stretch(e.Buffer, (1.00 + (realRampSpeed / (100.0 * realRampFactor))), silenceThreshold);
                buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;

                int realTarget = 0;
                if (targetRampedUp)
                    realTarget = targetMs;

                if (smoothRampEnabled)
                {
                    if ((realTarget - curdelay) < endRampTime && !almostDoneRampingDown)
                    {
                        if (realRampSpeed > (-1 * rampSpeed * realRampFactor) && realRampSpeed / realRampFactor > -99)
                            realRampSpeed--;
                        else if (realRampSpeed < (-1 * rampSpeed * realRampFactor))
                            realRampSpeed++;


                        double tempEndRampTime = 0 - output.DesiredLatency;
                        if (targetRampedUp)
                            tempEndRampTime = 0;

                        for (int i = 0; i > realRampSpeed; i--)
                        {
                            tempEndRampTime += 1.000 * i / realRampFactor;
                        }
                        endRampTime = (int)tempEndRampTime;
                    }
                    else
                    {
                        if (realRampSpeed < (-1 * realRampFactor))
                            realRampSpeed++;
                        else
                            realRampSpeed = -1 * realRampFactor;
                        almostDoneRampingDown = true;
                    }
                }
                else
                {
                    if(rampSpeed < 100)
                    {
                        realRampSpeed = -1 * rampSpeed;
                    }
                    else
                    {
                        realRampSpeed = -99;
                    }
                }

                if ((curdelay <= targetMs && targetRampedUp) || curdelay <= output.DesiredLatency)
                {
                    rampingdown = false;
                }
            }
            else
            {
                if (realRampSpeed == 0)
                {
                    buffer.AddSamples(Stretch(e.Buffer, 1.00), 0, e.BytesRecorded);
                    curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                }
                else
                {
                    if (smoothRampEnabled)
                    {
                        if (curdelay > 200)
                        {
                            var stretchedbuffer = Stretch(e.Buffer, (1.00 + (realRampSpeed / (100.0 * realRampFactor))), silenceThreshold);
                            buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                            if (realRampSpeed > 0)
                                realRampSpeed--;
                            else if (realRampSpeed < 0)
                                realRampSpeed++;
                        }
                        else
                            realRampSpeed = 0;
                    }
                    else
                    {
                        realRampSpeed = 0;
                    }
                }

                curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                endRampTime = 0;
                almostDoneRampingDown = false;
                almostDoneRampingUp = false;
                if (curdelay >= targetMs)
                {
                    rampingup = false;
                    quickramp = false;
                    if (output.PlaybackState == PlaybackState.Paused)
                    {
                        output.Play();
                    }
                }
            }
            if (targetRampedUp && curdelay >= targetMs)
            {
                rampingup = false;
            }
            else if ((curdelay <= targetMs && targetRampedUp) || curdelay <= output.DesiredLatency)
            {
                rampingdown = false;
            }
            if (buffer.BufferedDuration.TotalMilliseconds > output.DesiredLatency && !quickramp && output.PlaybackState != PlaybackState.Playing)
            {
                output.Play();
            }




        }

        private void outputSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitializeAudio();
        }

        private void inputSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitializeAudio();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double realRampSpeedPercent = 1.00 * realRampSpeed / realRampFactor;
            lblDebug1.Text = endRampTime.ToString();
            lblDebug2.Text = realRampSpeedPercent.ToString() + "%";
            lblDebug3.Text = peakLevel.ToString("F") + " dB";
            lblDebug4.Text = avgLevel.ToString("F") + " dB";

            if (buffer.BufferedBytes >= 0)
            {
                if (!recording)
                {
                    curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                }
            }
            else
            {
                curdelay = 0;
            }
            silenceThreshold = (double)txtThreshold.Value;
            if (buffer != null)
            {
                buffcumulative += curdelay;
                buffavgcounter++;
                if (buffavgcounter == 5)
                {
                    buffavg = buffcumulative / buffavgcounter;
                    lblCurrentDelay.Text = new TimeSpan(0, 0, 0, 0, buffavg).ToString(@"mm\:ss\.f");
                    buffavgcounter = 0;
                    buffcumulative = 0;
                    if (buffavg > dumpMs - output.DesiredLatency)
                    {
                        btnDump.BackColor = Color.Red;
                    }
                    else
                    {
                        btnDump.BackColor = Color.DarkRed;
                    }
                }

            }
            if (rampingup || rampingdown)
            {
                if (rampingdown)
                {
                    //we are ramping down
                    
                    if (!targetRampedUp)
                    {
                        timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((buffavg - output.DesiredLatency) / (realRampSpeed / (100.0 * realRampFactor))));
                    }
                    else
                    {
                        timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((buffavg - targetMs) / (realRampSpeed / (100.0 * realRampFactor))));
                    }

                    lblRampTimer.Text = (timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                }
                else if (rampingup)
                {
                    //we are ramping up
                    
                    timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((targetMs - buffavg) / (realRampSpeed / (100.0 * realRampFactor))));
                    lblRampTimer.Text = (timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                }

            }
            else
            {
                timetoRamp = new TimeSpan();

                //lblRampTimer.Text = "";
                if (smoothchange)
                {
                    realRampFactor = (int)numericUpDown1.Value;
                    if (realRampFactor < 1)
                    {
                        realRampFactor = 1;
                        smoothRampEnabled = false;
                    }
                    else
                    {
                        smoothRampEnabled = true;
                    }
                    smoothchange = false;
                }
            }
            if (targetMs < output.DesiredLatency)
            {
                targetMs = output.DesiredLatency;
            }

            if (targetRampedUp && targetMs < curdelay && rampingdown)
            {
                progressBar1.Minimum = targetMs;
                if (curdelay <= progressBar1.Maximum && curdelay >= progressBar1.Minimum)
                {
                    progressBar1.Value = curdelay;
                }

            }
            else
            {
                progressBar1.Maximum = targetMs;
                progressBar1.Minimum = 0;
                if (curdelay <= targetMs)
                {
                    progressBar1.Value = curdelay;
                }
                else
                {
                    progressBar1.Value = targetMs;
                }
            }



        }

        private void txtTarget_ValueChanged(object sender, EventArgs e)
        {
            targetMs = (int)(txtTarget.Value * 1000);

            if (targetRampedUp && curdelay > targetMs)
            {
                rampingdown = true;
                rampingup = false;
            }
            else if (targetRampedUp && curdelay < targetMs)
            {
                rampingup = true;
                rampingdown = false;
            }
            dumpMs = (int)(targetMs / txtDumps.Value);
            almostDoneRampingUp = false;
            almostDoneRampingDown = false;
        }

        private void txtSpeed_ValueChanged(object sender, EventArgs e)
        {
            rampSpeed = (int)txtSpeed.Value;
            almostDoneRampingDown = false;
            almostDoneRampingUp = false;
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            targetMs = (int)(txtTarget.Value * 1000);
            targetRampedUp = true;
            rampingdown = false;
            if (quickramp)
            {
                output.Play();
                quickramp = false;
            }
            else if (rampingup)
            {
                output.Pause();
                quickramp = true;
                if(almostDoneRampingUp)
                {
                    realRampSpeed = 0;
                }
            }
            rampingup = true;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            targetRampedUp = false;
            //targetMs = output.DesiredLatency;
            rampingdown = true;
            rampingup = false;
            rampingup = false;
            quickramp = false;
            if (output.PlaybackState == PlaybackState.Paused)
            {
                output.Play();
            }
        }




        private byte[] Stretch(byte[] inputbytes, double stretchfactor)
        {
            return (Stretch(inputbytes, stretchfactor, 0));
        }

        private byte[] Stretch(byte[] inputbytes, double stretchfactor, double silenceThreshold)
        {
            float[][] inputSamples = BytesToSamples(inputbytes, waveformat);
            float[][] outputSamples = new float[inputSamples.Length][];

            avgLevel = dBFS(AvgAudioLevel(inputSamples));
            peakLevel = dBFS(PeakAudioLevel(inputSamples));

            if (avgLevel > silenceThreshold)
            {
                stretchfactor = 1.00;
                realRampSpeed = 0;
            }
            if (stretchfactor < 1)
            {
                if(stretchfactor <= 0)
                {
                    stretchfactor = 0.01;
                }
                inputSamples = LowPass(inputSamples, waveformat.SampleRate / (2 / stretchfactor), waveformat.SampleRate);
            }

            for (int c = 0; c < outputSamples.Length; c++)
            {
                outputSamples[c] = new float[(int)(inputSamples[c].Length * stretchfactor)];
                outputSamples[c][0] = inputSamples[c][0];
                for (int i = 1; i < outputSamples[c].Length - 1; i++)
                {
                    double sampleTarget = (((double)(i * inputSamples[c].Length) / outputSamples[c].Length));
                    if (sampleTarget % 1 == 0 || ((int)sampleTarget + 1) >= inputSamples[c].Length)
                    {
                        outputSamples[c][i] = inputSamples[c][(int)sampleTarget];
                    }
                    else
                    {
                        double interp = sampleTarget - (int)sampleTarget;
                        float diff = inputSamples[c][(int)sampleTarget + 1] - inputSamples[c][(int)sampleTarget];
                        outputSamples[c][i] = inputSamples[c][(int)sampleTarget] + (float)(interp * diff);
                    }
                }
                outputSamples[c][outputSamples[c].Length - 1] = inputSamples[c][inputSamples[c].Length - 1];
            }
            if (stretchfactor > 1)
            {
                outputSamples = LowPass(outputSamples, waveformat.SampleRate / (2 * (stretchfactor)), waveformat.SampleRate);
            }
            else if (stretchfactor == 1)
            {
                for (int i = 0; i < filter.Length; i++)
                {
                    filter[i].SetLowPassFilter(waveformat.SampleRate, waveformat.SampleRate / 2, (float)Q);
                }
            }
            return SamplesToBytes(outputSamples, waveformat);


        }

        private float[] BytesToMonoSamples(byte[] buffer, WaveFormat waveformat)
        {
            float[] samples = new float[buffer.Length / waveformat.BlockAlign];

            for (int i = 0; i < samples.Length; i++)
            {

                int sample = 0;
                //one frame
                for (int j = 0; j < waveformat.Channels; j++)
                {
                    //one channel in the frame
                    int monosample = 0;
                    //int k = 0; k < waveformat.BlockAlign/waveformat.Channels; k++
                    for (int k = 0; k < waveformat.BlockAlign / waveformat.Channels; k++)
                    {
                        //one byte in the channel
                        monosample = monosample << 8;
                        monosample += buffer[(i * waveformat.BlockAlign) + (j * (waveformat.BlockAlign / waveformat.Channels)) + k];
                    }
                    int maxint = ((int)Math.Pow(2, waveformat.BitsPerSample) / 2) - 1;
                    if (monosample > maxint)
                    {
                        monosample = -1 - ((maxint + 1) - (monosample - maxint));
                    }
                    sample += monosample / waveformat.Channels;
                }
                samples[i] = sample / ((float)Math.Pow(2, waveformat.BitsPerSample - 1));
            }
            return samples;
        }

        private float[][] BytesToSamples(byte[] buffer, WaveFormat waveformat)
        {
            float[][] samples = new float[waveformat.Channels][];
            for (int i = 0; i < waveformat.Channels; i++)
            {
                float[] channel = new float[(buffer.Length / waveformat.BlockAlign)];
                for (int j = 0; j < channel.Length; j++)
                {
                    int monosample = 0;
                    for (int k = waveformat.BitsPerSample / 8; k > 0; k--)
                    {
                        monosample = monosample << 8;
                        monosample += buffer[(j * waveformat.BlockAlign) + (i * (waveformat.BlockAlign / waveformat.Channels)) + (k - 1)];
                    }
                    int maxint = ((int)Math.Pow(2, waveformat.BitsPerSample) / 2);
                    if (monosample >= maxint)
                    {
                        monosample = 0 - (maxint - (monosample & (maxint-1)));
                    }
                    channel[j] = monosample / (float)maxint;
                }
                samples[i] = channel;
            }
            return samples;
        }

        private byte[] SamplesToBytes(float[][] samples, WaveFormat waveformat)
        {
            byte[] bytes = new byte[samples[0].Length * waveformat.BlockAlign];

            for (int i = 0; i < waveformat.Channels; i++)
            {
                for (int j = 0; j < samples[0].Length; j++)
                {
                    int maxint = ((int)Math.Pow(2, waveformat.BitsPerSample) / 2);
                    int monosample = (int)(samples[i][j] * maxint);
                    //int k = waveformat.BitsPerSample / 8; k > 0; k--
                    for (int k = 0; k < waveformat.BlockAlign / waveformat.Channels; k++)
                    {
                        bytes[(j * waveformat.BlockAlign) + (i * (waveformat.BlockAlign / waveformat.Channels)) + (k)] = (byte)(monosample);
                        monosample = monosample >> 8;
                    }
                }
            }
            return bytes;
        }

        private float AvgAudioLevel(float[][] samples)
        {
            float avgLevel = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                for (int j = 0; j < samples[0].Length; j++)
                {
                    avgLevel += samples[i][j] * samples[i][j];
                }
            }
            return (float)Math.Sqrt(avgLevel / (samples.Length * samples[0].Length));
        }

        private float PeakAudioLevel(float[][] samples)
        {
            float peakLevel = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                for (int j = 0; j < samples[0].Length; j++)
                {
                    if (Math.Abs(samples[i][j]) > peakLevel)
                    {
                        peakLevel = Math.Abs(samples[i][j]);
                    }
                }
            }
            return peakLevel;
        }

        private double dBFS(float level)
        {
            return 20 * Math.Log10((double)level);
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            input.StopRecording();
            recording = false;

            int tempbufferbytes;

            if (curdelay > dumpMs && dumps > 1)
            {
                tempbufferbytes = buffer.BufferedBytes - ((waveformat.AverageBytesPerSecond * (targetMs / 1000) / dumps));//* (dumps - 1) / dumps / waveformat.BlockAlign * waveformat.BlockAlign;
                var tempbuffer = new byte[buffer.BufferedBytes];

                tempbufferbytes = buffer.Read(tempbuffer, 0, tempbufferbytes);

                buffer.ClearBuffer();

                buffer.AddSamples(tempbuffer, 0, tempbufferbytes);
            }
            else
            {
                buffer.ClearBuffer();
            }


            curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
            if (targetRampedUp && curdelay < targetMs)
            {
                rampingup = true;
            }
            else
            {
                rampingdown = true;
            }
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            dumpMs = (int)(targetMs / txtDumps.Value);
            dumps = (int)txtDumps.Value;
        }

        private void btnCough_MouseDown(object sender, EventArgs e)
        {
            input.StopRecording();
            recording = false;
            btnCough.BackColor = Color.Blue;
        }
        private void btnCough_MouseUp(object sender, EventArgs e)
        {
            input.StartRecording();
            recording = true;
            btnCough.BackColor = Color.DarkBlue;
            if (targetRampedUp && curdelay < targetMs)
            {
                rampingup = true;
            }
            almostDoneRampingDown = false;
            almostDoneRampingUp = false;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (!(rampingup || rampingdown))
            {
                realRampFactor = (int)numericUpDown1.Value;
                if (realRampFactor < 1)
                {
                    realRampFactor = 1;
                    smoothRampEnabled = false;
                }
                else
                {
                    smoothRampEnabled = true;
                }
            }
            else
            {
                smoothchange = true;
            }


        }

        public float[][] LowPass(float[][] source, double Frequency, int SampleRate, double Q)
        {
            for (int ch = 0; ch < source.Length; ch++)
            {
                filter[ch].SetLowPassFilter(SampleRate, (float)Frequency, (float)Q);
                for (int i = 0; i < source[ch].Length; i++)
                {
                    source[ch][i] = filter[ch].Transform(source[ch][i]);
                }
            }
            return source;
        }

        public float[][] LowPass(float[][] input, double Frequency, int SampleRate)
        {
            return LowPass(input, Frequency, SampleRate, Q);
        }
        public byte[] LowPass(byte[] source, double Frequency, WaveFormat waveformat, double Q)
        {
            var sourceSamples = BytesToSamples(source, waveformat);
            LowPass(sourceSamples, Frequency, waveformat.SampleRate, Q);
            return SamplesToBytes(sourceSamples, waveformat);
        }

        public byte[] LowPass(byte[] source, double Frequency, WaveFormat waveformat)
        {
            return LowPass(source, Frequency, waveformat, Q);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            
            if (rampingdown)
            {
                btnBuild.BackColor = Color.DarkGreen;
                if (btnExit.BackColor == Color.Yellow)
                {
                    btnExit.BackColor = Color.Olive;
                }
                else
                {
                    btnExit.BackColor = Color.Yellow;
                }
            }
            else if (rampingup)
            {
                btnExit.BackColor = Color.Olive;
                if (btnBuild.BackColor == Color.Lime && !quickramp)
                {
                    btnBuild.BackColor = Color.DarkGreen;
                }
                else
                {
                    btnBuild.BackColor = Color.Lime;
                }
            }
            else
            {
                btnBuild.BackColor = Color.DarkGreen;
                btnExit.BackColor = Color.Olive;
            }


        }
    }
}
