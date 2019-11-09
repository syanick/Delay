using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Dsp;
using NAudio.Utils;
using soundtouch;

namespace Delay
{
    public partial class Form1 : Form
    {
        WaveFormat waveformat = new WaveFormat(44100, 16, 2);
        BufferedDelayProvider buffer;
        BufferedDelayProvider outbuffer;
        BufferedDelayProvider repeatBuffer;
        WaveOutEvent output = new WaveOutEvent();
        WaveInEvent input = new WaveInEvent();
        SoundTouch stretcher = new SoundTouch();

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
        float tempochange = 0;
        int endRampTime = 0;
        bool smoothchange = true;
        bool smoothRampEnabled = true;
        bool almostDoneRampingUp = false;
        bool almostDoneRampingDown = false;
        int blinkInterval = 500;
        bool recording = false;
        bool rampForever = false;
        bool pause = false;
        bool holdCough = false;
        bool unplug = false;
        bool plugItIn = false;
        double avgLevel;
        double peakLevel;
        bool pitchMode = false;
        bool timeMode = true;
        bool repeatMode = false;
        bool repeating = false;
        TimeSpan oneday = new TimeSpan(1, 0, 0, 0);
        TimeSpan twodays = new TimeSpan(2, 0, 0, 0);

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
            buffer = new BufferedDelayProvider(waveformat);
            outbuffer = new BufferedDelayProvider(waveformat);
            repeatBuffer = new BufferedDelayProvider(waveformat);
            buffer.BufferDuration = new TimeSpan(1, 1, 0);
            outbuffer.BufferDuration = new TimeSpan(0, 2, 0);
            repeatBuffer.BufferDuration = new TimeSpan(1, 1, 0);
            inputSelector.SelectedIndex = 0;
            outputSelector.SelectedIndex = 0;
            dumpMs = (int)(targetMs / txtDumps.Value);
            numericUpDown1.Value = realRampFactor;
            txtThreshold.Value = Convert.ToDecimal(silenceThreshold);
            timer2.Interval = blinkInterval;
            modeSelector.SelectedItem = "Time";
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

            stretcher.Channels = (uint)input.WaveFormat.Channels;
            stretcher.SampleRate = (uint)input.WaveFormat.SampleRate;
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (pitchMode)
            {
                var stretchedbuffer = Stretch(e.Buffer, (1.00 + (realRampSpeed / (100.0 * realRampFactor))), silenceThreshold);
                buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
            }
            else if (timeMode)
            {
                tempochange = (float)realRampSpeed / (float)realRampFactor;
                tempochange = -(100f * tempochange) / (100f + tempochange);

                stretcher.TempoChange = tempochange;
                float[] inbuffer = new float[e.Buffer.Length * waveformat.Channels / waveformat.BlockAlign];
                inbuffer = BytesToSTSamples(e.Buffer, waveformat);
                stretcher.PutSamples(inbuffer, (uint)(e.Buffer.Length / waveformat.BlockAlign));
                if (stretcher.AvailableSampleCount > 0)
                {
                    uint samplecount = stretcher.AvailableSampleCount;
                    float[] outbuffer = new float[samplecount * waveformat.Channels];
                    stretcher.ReceiveSamples(outbuffer, samplecount);
                    buffer.AddSamples(SamplesToBytes(outbuffer, waveformat), 0, (int)samplecount * waveformat.BlockAlign);
                }

                avgLevel = dBFS(AvgAudioLevel(inbuffer));
                peakLevel = dBFS(PeakAudioLevel(inbuffer));
            }
            else if (repeatMode)
            {
                buffer.AddSamples(e.Buffer, 0, e.Buffer.Length);
                if (rampingup)
                {
                    if (!repeating)
                    {
                        var tempbuffer = new byte[buffer.BufferedBytes];
                        buffer.Read(tempbuffer, 0, buffer.BufferedBytes);
                        repeatBuffer.AddSamples(tempbuffer, 0, tempbuffer.Length);
                        repeating = true;
                        output.Stop();
                        output.Init(repeatBuffer);
                        output.Play();
                    }
                    else
                    {
                        repeatBuffer.AddSamples(e.Buffer, 0, e.Buffer.Length);
                    }
                }
                if (rampingdown)
                {
                    int tempbufferbytes = (waveformat.AverageBytesPerSecond * (targetMs / 1000));
                    var tempbuffer = new byte[buffer.BufferedBytes];

                    tempbufferbytes = buffer.Read(tempbuffer, 0, tempbufferbytes);
                    buffer.ClearBuffer();
                    buffer.AddSamples(tempbuffer, 0, tempbufferbytes);
                }
            }

            if (targetRampedUp && rampingup && curdelay < targetMs && !quickramp)
            {
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
                    if (output.PlaybackState == PlaybackState.Paused && !pause)
                    {
                        output.Play();
                    }
                }
            }

            else if ((curdelay > targetMs || !targetRampedUp) && rampingdown && curdelay > output.DesiredLatency)
            {
                //Ramp down to the target
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
                    if (rampSpeed < 100)
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
                if (unplug)
                {
                    curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                    if (curdelay < 880000)
                    {
                        if (realRampSpeed < 100)
                        {
                            realRampSpeed++;
                        }
                        else if (realRampSpeed < 250)
                        {
                            realRampSpeed += 5;
                        }
                        else if (realRampSpeed < 500)
                        {
                            realRampSpeed += 25;
                        }
                        else if (realRampSpeed < 5000)
                        {
                            realRampSpeed += 500;
                        }
                        else
                        {
                            input.StopRecording();
                            holdCough = true;
                        }
                    }
                    else
                    {
                        input.StopRecording();
                        holdCough = true;
                    }
                }
                else if (plugItIn)
                {
                    if (output.PlaybackState != PlaybackState.Playing)
                    {
                        output.Play();
                    }

                    if (realRampSpeed > 100)
                    {
                        realRampSpeed = 100;
                    }
                    else if (realRampSpeed > 50)
                    {
                        realRampSpeed -= 25;
                    }
                    else if (realRampSpeed > 10)
                    {
                        realRampSpeed -= 5;
                    }
                    else
                    {
                        plugItIn = false;
                    }

                    var stretchedbuffer = Stretch(e.Buffer, (1.00 + (realRampSpeed / 100.0)));
                    buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                    curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                }
                else if (repeatMode)
                {
                    if (repeating)
                    {
                        repeating = false;
                        output.Stop();
                        output.Init(buffer);
                        output.Play();
                        repeatBuffer.ClearBuffer();
                    }
                }
                else if (buffavg < output.DesiredLatency)
                {
                    realRampSpeed = realRampFactor;
                }
                else if (realRampSpeed == 0)
                {
                    curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
                    if (rampForever)
                    {
                        if (targetRampedUp)
                        {
                            targetRampedUp = false;
                            //targetMs = output.DesiredLatency;
                            rampingdown = true;
                            rampingup = false;
                            rampingup = false;
                            quickramp = false;
                            if (output.PlaybackState == PlaybackState.Paused && !pause)
                            {
                                output.Play();
                            }
                        }
                        else
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
                                if (almostDoneRampingUp)
                                {
                                    realRampSpeed = 0;
                                }
                            }
                            rampingup = true;
                        }
                    }
                }
                else
                {
                    if (smoothRampEnabled)
                    {
                        if (curdelay > 200)
                        {
                            if (realRampSpeed > 0)
                                realRampSpeed--;
                            else if (realRampSpeed < 0)
                                realRampSpeed++;
                        }
                        else
                        {
                            realRampSpeed = 0;
                        }
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
                    if (output.PlaybackState == PlaybackState.Paused && !pause)
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
            if (buffer.BufferedDuration.TotalMilliseconds > output.DesiredLatency && !quickramp && output.PlaybackState != PlaybackState.Playing && !pause)
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
            //lblDebug1.Text = tempochange.ToString();
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

            if (buffer != null)
            {
                buffcumulative += curdelay;
                buffavgcounter++;
                if (buffavgcounter == 5)
                {
                    buffavg = buffcumulative / buffavgcounter;
                    if(buffavg >= 3600000)
                    {
                        lblCurrentDelay.Text = new TimeSpan(0, 0, 0, 0, buffavg).ToString(@"h\:mm\:ss");
                    }
                    else
                    {
                        lblCurrentDelay.Text = new TimeSpan(0, 0, 0, 0, buffavg).ToString(@"mm\:ss\.f");
                    }
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
                }
                else if (rampingup)
                {
                    //we are ramping up

                    timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((targetMs - buffavg) / (realRampSpeed / (100.0 * realRampFactor))));
                }

                if (!repeatMode)
                {
                    if (timetoRamp >= twodays)
                    {
                        lblRampTimer.Text = (timetoRamp.ToString("%d") + " Days " + timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                    }
                    else if (timetoRamp >= oneday)
                    {
                        lblRampTimer.Text = ("1 Day " + timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                    }
                    else
                    {
                        lblRampTimer.Text = (timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                    }
                }
                else
                {
                    lblRampTimer.Text = "";
                }
            }
            else
            {
                timetoRamp = new TimeSpan();

                lblRampTimer.Text = "";
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
            else if (rampingup && !repeatMode)
            {
                output.Pause();
                quickramp = true;
                if (almostDoneRampingUp)
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
            if (output.PlaybackState == PlaybackState.Paused && !pause)
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
                if (stretchfactor <= 0)
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
                        monosample = 0 - (maxint - (monosample & (maxint - 1)));
                    }
                    channel[j] = monosample / (float)maxint;
                }
                samples[i] = channel;
            }
            return samples;
        }

        private float[] BytesToSTSamples(byte[] buffer, WaveFormat waveformat)
        {
            float[] samples = new float[waveformat.Channels * (buffer.Length / waveformat.BlockAlign)];
            for (int j = 0; j < samples.Length; j++)
            {
                int monosample = 0;
                for (int k = waveformat.BitsPerSample / 8; k > 0; k--)
                {
                    monosample = monosample << 8;
                    monosample += buffer[(j * waveformat.BlockAlign / waveformat.Channels) + (k - 1)];
                }
                int maxint = ((int)Math.Pow(2, waveformat.BitsPerSample) / 2);
                if (monosample >= maxint)
                {
                    monosample = 0 - (maxint - (monosample & (maxint-1)));
                }
                samples[j] = monosample / (float)maxint;
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

        private byte[] SamplesToBytes(float[] samples, WaveFormat waveformat)
        {
            byte[] bytes = new byte[samples.Length * waveformat.BlockAlign / waveformat.Channels];

            for (int j = 0; j < samples.Length; j++)
            {
                int maxint = ((int)Math.Pow(2, waveformat.BitsPerSample) / 2);
                int monosample = (int)(samples[j] * maxint);
                //int k = waveformat.BitsPerSample / 8; k > 0; k--
                for (int k = 0; k < waveformat.BlockAlign / waveformat.Channels; k++)
                {
                    bytes[(j * waveformat.BlockAlign / waveformat.Channels) + (k)] = (byte)(monosample);
                    monosample = monosample >> 8;
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

        private float AvgAudioLevel(float[] samples)
        {
            float avgLevel = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                avgLevel += samples[i] * samples[i];
            }
            return (float)Math.Sqrt(avgLevel / (samples.Length));
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
        private float PeakAudioLevel(float[] samples)
        {
            float peakLevel = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                if (Math.Abs(samples[i]) > peakLevel)
                {
                    peakLevel = Math.Abs(samples[i]);
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
            MouseEventArgs me = (MouseEventArgs)e;
            input.StopRecording();
            recording = false;

            //int tempbufferbytes;
            int dumpbytes;

            if (curdelay > dumpMs && dumps > 1)
            {
                //tempbufferbytes = buffer.BufferedBytes - ((waveformat.AverageBytesPerSecond * (targetMs / 1000) / dumps));//* (dumps - 1) / dumps / waveformat.BlockAlign * waveformat.BlockAlign;
                dumpbytes = (waveformat.AverageBytesPerSecond * (targetMs / 1000) / dumps);
                //var tempbuffer = new byte[buffer.BufferedBytes];

                if(me.Button != System.Windows.Forms.MouseButtons.Right)
                {
                    //tempbufferbytes = buffer.Read(tempbuffer, 0, tempbufferbytes);
                    //buffer.ClearBuffer();
                    //buffer.AddSamples(tempbuffer, 0, tempbufferbytes);
                    buffer.Dump(dumpbytes);
                }
                else
                {
                    //int tempoffset = (waveformat.AverageBytesPerSecond * (targetMs / 1000) / dumps);
                    //var tempbuffer2 = new byte[tempbufferbytes];
                    //buffer.Read(tempbuffer, 0, tempbuffer.Length);
                    //Buffer.BlockCopy(tempbuffer, tempoffset, tempbuffer2, 0, tempbufferbytes);
                    //buffer.ClearBuffer();
                    //buffer.AddSamples(tempbuffer2, 0, tempbufferbytes);
                    buffer.DumpEnd(dumpbytes);
                }

            }
            else
            {
                output.Pause();
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
            stretcher.Clear();
            if(curdelay < output.DesiredLatency)
            {
                output.Pause();
            }
            try
            {
                input.StartRecording();
            }
            catch
            {

            }
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

        private void BtnForever_Click(object sender, EventArgs e)
        {
            if (rampForever)
            {
                rampForever = false;
                btnForever.BackColor = SystemColors.Control;
            }
            else
            {
                rampForever = true;
                btnForever.BackColor = Color.White;
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            if (pause)
            {
                output.Play();
                pause = false;
                btnPause.BackColor = SystemColors.Control;
            }
            else
            {
                output.Pause();
                pause = true;
                btnPause.BackColor = Color.White;
            }
        }

        private void BtnHold_Click(object sender, EventArgs e)
        {
            if (holdCough)
            {
                input.StartRecording();
                holdCough = false;
                btnHold.BackColor = SystemColors.Control;
            }
            else
            {
                input.StopRecording();
                holdCough = true;
                btnHold.BackColor = Color.White;
            }
        }

        private void BtnRampStop_Click(object sender, EventArgs e)
        {
            almostDoneRampingDown = true;
            almostDoneRampingUp = true;
            rampingdown = false;
            rampingup = false;
            if (rampForever)
            {
                btnForever.PerformClick();
            }
        }

        private void BtnBypass_Click(object sender, EventArgs e)
        {
            buffer.ClearBuffer();
            realRampSpeed = 0;
            rampingdown = false;
            rampingup = false;
            unplug = false;
            plugItIn = false;
            if (rampForever)
            {
                btnForever.PerformClick();
            }
            if (holdCough)
            {
                input.StartRecording();
                holdCough = false;
                btnHold.BackColor = SystemColors.Control;
            }
            if (pause)
            {
                output.Play();
                pause = false;
                btnPause.BackColor = SystemColors.Control;
            }
            btnUnplug.BackColor = SystemColors.Control;
        }

        private void BtnCrashRamp_Click(object sender, EventArgs e)
        {
            realRampSpeed = 0;
            rampingdown = false;
            rampingup = false;
            if (rampForever)
            {
                btnForever.PerformClick();
            }
        }

        private void BtnSetTarget_Click(object sender, EventArgs e)
        {
            targetMs = (int)buffer.BufferedDuration.TotalMilliseconds / 1000;
            txtTarget.Value = (decimal)buffer.BufferedDuration.TotalMilliseconds / 1000;
        }

        private void BtnSetSmooth_Click(object sender, EventArgs e)
        {
            int oldFactor = realRampFactor;
            int oldSpeed = realRampSpeed;

            if (numericUpDown1.Value < 1)
            {
                realRampSpeed = rampSpeed;
                realRampFactor = 1;
                smoothRampEnabled = false;
            }
            else
            {
                realRampSpeed = oldSpeed * (int)numericUpDown1.Value / oldFactor;
                realRampFactor = (int)numericUpDown1.Value;
                smoothRampEnabled = true;
            }
            smoothchange = false;
        }

        private void BtnUnplug_Click(object sender, EventArgs e)
        {
            modeSelector.SelectedItem = "Pitch";
            pitchMode = true;
            timeMode = false;

            if (unplug)
            {
                unplug = false;
                plugItIn = true;
                btnUnplug.BackColor = SystemColors.Control;
                buffer.ClearBuffer();
                realRampSpeed = 100;

                if (holdCough)
                {
                    holdCough = false;
                    input.StartRecording();
                }
            }
            else
            {
                unplug = true;
                plugItIn = false;
                realRampFactor = 1;
                numericUpDown1.Value = 1;
                rampingdown = false;
                rampingup = false;
                quickramp = false;
                btnUnplug.BackColor = Color.White;
            }
        }

        private void TxtThreshold_ValueChanged(object sender, EventArgs e)
        {
            silenceThreshold = (double)txtThreshold.Value;
        }

        private void ModeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            stretcher.Clear();
            if(modeSelector.Text == "Pitch")
            {
                pitchMode = true;
                timeMode = false;
                repeatMode = false;
            }
            else if(modeSelector.Text == "Time")
            {
                pitchMode = false;
                timeMode = true;
                repeatMode = false;
            }
            else
            {
                pitchMode = false;
                timeMode = false;
                repeatMode = true;
            }
        }
    }

    /// <summary>
    /// Modified version of CircularBuffer from NAudio
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularDelayBuffer
    {
        private readonly byte[] buffer;
        private readonly object lockObject;
        private int writePosition;
        private int readPosition;
        private int byteCount;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularDelayBuffer(int size)
        {
            buffer = new byte[size];
            lockObject = new object();
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of bytes to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(byte[] data, int offset, int count)
        {
            lock (lockObject)
            {
                var bytesWritten = 0;
                if (count > buffer.Length - byteCount)
                {
                    count = buffer.Length - byteCount;
                }
                // write to end
                int writeToEnd = Math.Min(buffer.Length - writePosition, count);
                Array.Copy(data, offset, buffer, writePosition, writeToEnd);
                writePosition += writeToEnd;
                writePosition %= buffer.Length;
                bytesWritten += writeToEnd;
                if (bytesWritten < count)
                {
                    //Debug.Assert(writePosition == 0);
                    // must have wrapped round. Write to start
                    Array.Copy(data, offset + bytesWritten, buffer, writePosition, count - bytesWritten);
                    writePosition += (count - bytesWritten);
                    bytesWritten = count;
                }
                byteCount += bytesWritten;
                return bytesWritten;
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">Bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(byte[] data, int offset, int count)
        {
            lock (lockObject)
            {
                if (count > byteCount)
                {
                    count = byteCount;
                }
                int bytesRead = 0;
                int readToEnd = Math.Min(buffer.Length - readPosition, count);
                Array.Copy(buffer, readPosition, data, offset, readToEnd);
                bytesRead += readToEnd;
                readPosition += readToEnd;
                readPosition %= buffer.Length;

                if (bytesRead < count)
                {
                    // must have wrapped round. Read from start
                    //Debug.Assert(readPosition == 0);
                    Array.Copy(buffer, readPosition, data, offset + bytesRead, count - bytesRead);
                    readPosition += (count - bytesRead);
                    bytesRead = count;
                }

                byteCount -= bytesRead;
                //Debug.Assert(byteCount >= 0);
                return bytesRead;
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => buffer.Length;

        /// <summary>
        /// Number of bytes currently stored in the circular buffer
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return byteCount;
                }
            }
        }

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            lock (lockObject)
            {
                ResetInner();
            }
        }

        private void ResetInner()
        {
            byteCount = 0;
            readPosition = 0;
            writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding bytes
        /// </summary>
        /// <param name="count">Bytes to advance</param>
        public void Advance(int count)
        {
            lock (lockObject)
            {
                if (count >= byteCount)
                {
                    ResetInner();
                }
                else
                {
                    byteCount -= count;
                    readPosition += count;
                    readPosition %= MaxLength;
                }
            }
        }

        public void Dump(int count)
        {
            lock (lockObject)
            {
                if (count >= byteCount)
                {
                    ResetInner();
                }
                else
                {
                    byteCount -= count;
                    writePosition -= count;
                    if(writePosition < 0)
                    {
                        writePosition += MaxLength;
                    }
                }
            }
        }
    }

    /// <summary>
    /// This is a modified verison of NAudio's BufferedWaveProvider
    /// Provides a buffered store of samples
    /// Read method will return queued samples or fill buffer with zeroes
    /// Now backed by a circular buffer
    /// </summary>
    public class BufferedDelayProvider : IWaveProvider
    {
        private CircularDelayBuffer circularBuffer;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public BufferedDelayProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
            BufferLength = waveFormat.AverageBytesPerSecond * 5;
            ReadFully = true;
        }

        /// <summary>
        /// If true, always read the amount of data requested, padding with zeroes if necessary
        /// By default is set to true
        /// </summary>
        public bool ReadFully { get; set; }

        /// <summary>
        /// Buffer length in bytes
        /// </summary>
        public int BufferLength { get; set; }

        /// <summary>
        /// Buffer duration
        /// </summary>
        public TimeSpan BufferDuration
        {
            get
            {
                return TimeSpan.FromSeconds((double)BufferLength / WaveFormat.AverageBytesPerSecond);
            }
            set
            {
                BufferLength = (int)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            }
        }

        /// <summary>
        /// If true, when the buffer is full, start throwing away data
        /// if false, AddSamples will throw an exception when buffer is full
        /// </summary>
        public bool DiscardOnBufferOverflow { get; set; }

        /// <summary>
        /// The number of buffered bytes
        /// </summary>
        public int BufferedBytes
        {
            get
            {
                return circularBuffer == null ? 0 : circularBuffer.Count;
            }
        }

        /// <summary>
        /// Buffered Duration
        /// </summary>
        public TimeSpan BufferedDuration
        {
            get { return TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond); }
        }

        /// <summary>
        /// Gets the WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary
        /// </summary>
        public void AddSamples(byte[] buffer, int offset, int count)
        {
            // create buffer here to allow user to customise buffer length
            if (circularBuffer == null)
            {
                circularBuffer = new CircularDelayBuffer(BufferLength);
            }

            var written = circularBuffer.Write(buffer, offset, count);
            if (written < count && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
        }

        /// <summary>
        /// Reads from this WaveProvider
        /// Will always return count bytes, since we will zero-fill the buffer if not enough available
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (circularBuffer != null) // not yet created
            {
                read = circularBuffer.Read(buffer, offset, count);
            }
            if (ReadFully && read < count)
            {
                // zero the end of the buffer
                Array.Clear(buffer, offset + read, count - read);
                read = count;
            }
            return read;
        }

        /// <summary>
        /// Discards all audio from the buffer
        /// </summary>
        public void ClearBuffer()
        {
            if (circularBuffer != null)
            {
                circularBuffer.Reset();
            }
        }

        public void Dump(int bytes)
        {
            circularBuffer.Dump(bytes);
        }

        public void DumpEnd(int bytes)
        {
            circularBuffer.Advance(bytes);
        }
    }
}
