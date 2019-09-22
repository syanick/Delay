using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

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
        double silenceThreshold;
        bool quickramp = false;
        int realRampSpeed = 0;
        int realRampFactor = 20; //ramp ramp speed -- set higher for slower ramp ramp -- make it an even number
        

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
            buffer.BufferDuration = new TimeSpan(1, 0, 0);
            inputSelector.SelectedIndex = 0;
            outputSelector.SelectedIndex = 0;
            dumpMs = (int)(targetMs / txtDumps.Value);
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
            
            output.Init(buffer);
            output.Pause();
            try
            {
                input.StartRecording();
            }
            catch
            {

            }
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {

           

            if (targetRampedUp && rampingup && curdelay < targetMs && !quickramp)
            {
                var stretchedbuffer = stretch(e.Buffer, (1.00 + (realRampSpeed/(100.0 * realRampFactor)))); //removing this for now ,silenceThreshold);
                buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                
                if((targetMs - (curdelay * (1.00 + (realRampSpeed/(100.0 * realRampFactor))))) > (realRampSpeed * 20))
                {
                    if(realRampSpeed < (rampSpeed * realRampFactor))
                        realRampSpeed++;
                    else if(realRampSpeed > (rampSpeed * realRampFactor))
                        realRampSpeed--;
                }
                else
                {
                    if(realRampSpeed > (realRampFactor / 2))
                        realRampSpeed--;
                    else
                        realRampSpeed = realRampFactor / 2;
                }
                
                //ramping = false;
                if (curdelay >= targetMs)
                {
                    rampingup = false;
                    quickramp = false;
                    if(output.PlaybackState == PlaybackState.Paused)
                    {
                        output.Play();
                    }
                }
            }
            
            else if ((curdelay > targetMs || !targetRampedUp) && rampingdown && curdelay > output.DesiredLatency)
            {
                //Ramp down to the target
                var stretchedbuffer = stretch(e.Buffer, (1.00 + (realRampSpeed/(100.0 * realRampFactor)))); //removing this for now , silenceThreshold);
                buffer.AddSamples(stretchedbuffer, 0, stretchedbuffer.Length);
                
                if((-1 * curdelay * (1.00 + (realRampSpeed/(100.0 * realRampFactor)))) < ((realRampSpeed * 20) - 500))
                {
                    if(realRampSpeed > (-1 * rampSpeed * realRampFactor))
                        realRampSpeed--;
                    else if(realRampSpeed < (-1 * rampSpeed * realRampFactor))
                        realRampSpeed++;
                }
                else
                {
                    if(realRampSpeed < (-1 * (realRampFactor / 2)))
                        realRampSpeed++;
                    else
                        realRampSpeed = -1 * (realRampFactor / 2);
                }

                if ((curdelay <= targetMs && targetRampedUp)||curdelay <= output.DesiredLatency)
                {
                    rampingdown = false;
                }
            }
            else
            {
                buffer.AddSamples(stretch(e.Buffer,1.00),0,e.BytesRecorded);
                realRampSpeed = 0;

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
            if (buffer.BufferedDuration.TotalMilliseconds > output.DesiredLatency && !quickramp)
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
            if (buffer.BufferedBytes >= 0)
            {
                curdelay = (int)buffer.BufferedDuration.TotalMilliseconds;
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
                }
                                             
            }
            if (rampingup || rampingdown)
            {
                if (rampingdown)
                {
                    //we are ramping down
                    btnBuild.BackColor = Color.DarkGreen;
                    if (btnExit.BackColor == Color.Yellow)
                    {
                        btnExit.BackColor = Color.Olive;
                    }
                    else
                    {
                        btnExit.BackColor = Color.Yellow;
                    }
                    timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((buffavg - output.DesiredLatency) / (rampSpeed / 100.0)));
                    lblRampTimer.Text = (timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                }
                else if (rampingup)
                {
                    //we are ramping up
                    btnExit.BackColor = Color.Olive;
                    if (btnBuild.BackColor == Color.Lime)
                    {
                        btnBuild.BackColor = Color.DarkGreen;
                    }
                    else
                    {
                        btnBuild.BackColor = Color.Lime;
                    }
                    timetoRamp = new TimeSpan(0, 0, 0, 0, (int)((targetMs - buffavg) / (rampSpeed / 100.0)));
                    lblRampTimer.Text = (timetoRamp.ToString(@"h\:mm\:ss") + " Remaining");
                }
                
            }
            else
            {
                btnBuild.BackColor = Color.DarkGreen;
                btnExit.BackColor = Color.Olive;
                timetoRamp = new TimeSpan();
                lblRampTimer.Text = "";
            }
            if (curdelay > dumpMs - output.DesiredLatency)
            {
                btnDump.BackColor = Color.Red;
            }
            else
            {
                btnDump.BackColor = Color.DarkRed;
            }
            if (targetMs < output.DesiredLatency)
            {
                targetMs = output.DesiredLatency;
            }
            progressBar1.Maximum = targetMs;
            if (curdelay <= targetMs)
            {
                progressBar1.Value = curdelay;
            }
            else
            {
                progressBar1.Value = targetMs;
            }

            
        }

        private void txtTarget_ValueChanged(object sender, EventArgs e)
        {
            targetMs = (int)(txtTarget.Value*1000);
            
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
        }

        private void txtSpeed_ValueChanged(object sender, EventArgs e)
        {
            rampSpeed = (int)txtSpeed.Value;
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

        
        
              

        private byte[] stretch(byte[] inputbytes, double stretchfactor)
        {
            int blockalign = input.WaveFormat.BlockAlign;
            int outputblocks = (int)((inputbytes.Length / blockalign) * stretchfactor);
            byte[] outputbytes = new byte[outputblocks* blockalign];
            
            byte[][] inputblocks = new byte[inputbytes.Length / blockalign][];
            for (int i = 0; i < inputblocks.Length; i++)
            {
                byte[] block = new byte[blockalign];
                for (int j = 0; j < blockalign; j++)
                {
                    block[j] = inputbytes[(i * blockalign) + j];
                }
                inputblocks[i] = block;
            }
            for (int i = 0; i < outputbytes.Length; i+=blockalign)
            {
                int blocktarget = (int)(((float)i / outputbytes.Length) * inputblocks.Length);
                for (int j = 0; j < blockalign; j++)
                {
                    outputbytes[i + j] = inputblocks[blocktarget][j];
                }
            }
            
            return outputbytes;
        }

        private byte[] stretch(byte[] inputbytes, double stretchfactor, double silenceThreshold)
        {
            if (dBFS(avgAudioLevel(inputbytes, waveformat))>silenceThreshold)
            {
                stretchfactor = 1.00;
            }
            int blockalign = input.WaveFormat.BlockAlign;
            int outputblocks = (int)((inputbytes.Length / blockalign) * stretchfactor);
            byte[] outputbytes = new byte[outputblocks * blockalign];

            byte[][] inputblocks = new byte[inputbytes.Length / blockalign][];
            for (int i = 0; i < inputblocks.Length; i++)
            {
                byte[] block = new byte[blockalign];
                for (int j = 0; j < blockalign; j++)
                {
                    block[j] = inputbytes[(i * blockalign) + j];
                }
                inputblocks[i] = block;
            }
            for (int i = 0; i < outputbytes.Length; i += blockalign)
            {
                int blocktarget = (int)(((float)i / outputbytes.Length) * inputblocks.Length);
                for (int j = 0; j < blockalign; j++)
                {
                    outputbytes[i + j] = inputblocks[blocktarget][j];
                }
            }

            return outputbytes;
        }

        private float[] bytesToMonoSamples(byte[] buffer, WaveFormat waveformat)
        {
            float[] samples = new float[buffer.Length / waveformat.BlockAlign];
            
            for (int i = 0; i < samples.Length; i++)
            {
                
                int sample=0;
                //one frame
                for (int j = 0; j < waveformat.Channels; j++)
                {
                    //one channel in the frame
                    int monosample = 0;
                    for (int k = 0; k < waveformat.BlockAlign/waveformat.Channels; k++)
                    {
                        //one byte in the channel
                        monosample = sample << 8;
                        monosample += buffer[(i * waveformat.BlockAlign) + (j * (waveformat.BlockAlign / waveformat.Channels)) + k];
                    }
                    sample += monosample / waveformat.Channels;
                }
                samples[i] = sample / 32768f;
            }
            return samples;
        }

        private float avgAudioLevel(byte[] buffer, WaveFormat waveformat)
        {
            float[] samples = bytesToMonoSamples(buffer, waveformat);
            float avgLevel = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                avgLevel += samples[i];
            }
            return avgLevel / samples.Length;
        }

        private double dBFS (float level)
        {
            return 20 * Math.Log10((double)level);
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            input.StopRecording();
            output.Pause();
            var tempbuffer = buffer;
            int dumpbytes = (dumpMs / 1000) * waveformat.SampleRate * (waveformat.BitsPerSample / 8);
            if (tempbuffer.BufferedBytes > dumpbytes)
            {
                byte[] newBuffer = new byte[tempbuffer.BufferedBytes - dumpbytes];
                for (int i = 0; i < newBuffer.Length; i++)
                {
                    
                }
            }
            buffer.ClearBuffer();

            
            
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
        }

        private void btnCough_MouseDown(object sender, EventArgs e)
        {
            input.StopRecording();
            btnCough.BackColor = Color.Blue;
        }
        private void btnCough_MouseUp(object sender, EventArgs e)
        {
            input.StartRecording();
            btnCough.BackColor = Color.DarkBlue;
            if(targetRampedUp && curdelay < targetMs)
            {
                rampingup = true;
            }
        }

    }
}
