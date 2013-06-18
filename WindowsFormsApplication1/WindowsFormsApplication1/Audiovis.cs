using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class Audiovis
    {
        //doing this wrong atm should apply FFT to whole song THEN calculate bins
        //which for bass.wav is 48000Hz, left[] = 10439424, 
        
        //To create a 8192 point fft then need logN = 13
        //Bins are then 8192/2 = 4096
        //Signal contains info upto  24Khz (Nyquist)
        //singal resolution is 24000/4096       
        
        int blockSize = 4096;
        uint logN;
        double scalar = (double)20 / System.Math.Log(10);
        public double[] real; 
        public double[] imaginary;
        public double[] magnitude;
        public double[] frequency;
        public double[] hanningWindow;
        public double[,] magnitudeFrequency;
        public double[][] fftData;
        
   
        public double[][,] spectralGraph;

        FFT2 FFT = new FFT2();
       

        public Audiovis()
        {
            
        }

        // convert two bytes to one double in the range -1 to 1
        static double bytesToDouble(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)           
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0;
        }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public void openWav(string filename, out double[] left, out double[] right)
        {
            byte[] wav = null;

            if (File.Exists(filename))
                wav = File.ReadAllBytes(filename);
            else
                MessageBox.Show("Error locating file", "Error", MessageBoxButtons.OK);

            // Determine if mono or stereo
            int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            int samples = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
            if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            left = new double[samples];
            if (channels == 2) right = new double[samples];
            else right = null;

            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                left[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
                if (channels == 2)
                {
                    right[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
                        
            //set everything up
            logN = (uint)Math.Log(blockSize,2); //logN will determine the points of data the fft has @ ^2, logN 13 = 8192, 12= 4096
            
            int audioIndex = 0;
           
            spectralGraph = new double[left.Length/blockSize][,];
            magnitudeFrequency = new double[blockSize/2,2];
            fftData = new double[left.Length/blockSize][];
            real = new double[blockSize];
            hanningWindow = new double[blockSize];
            imaginary = new double[blockSize];
            magnitude = new double[blockSize/2];
            frequency = new double[blockSize/2];

            for (int x = 0; x < hanningWindow.Length; x++)
            {
              hanningWindow[x] = 0.5 * (1 - Math.Cos(2 * Math.PI * x / blockSize));                 
            }

            for (int x = 0; x < fftData.Length; x++)
            {
                fftData[x] = new double[blockSize];
            }
            //this approach leads to 2548 lots of blockSize which leads to an issue when displaying, since I dont know at what speed to refresh
            //
            //split left into blockSize chunks 
            for (int x = 0; x < left.Length/blockSize; x++)
            {
                for (int y = 0; y < blockSize; y ++)
                {
                    fftData[x][y] = left[audioIndex];
                    audioIndex++;
                }
            }

            for (int x = 0; x < spectralGraph.Length; x++)
            {            
                spectralGraph[x] = new double[blockSize/2, 2];
            }

            
            //length is 2548
            for (int x = 0; x < spectralGraph.Length; x++)
            {
                fft(fftData[x]);
                calculateMagnitude();

                for (int y = 0; y < blockSize/2; y++)
                {
                    //0 is magnitude , 1 is frequency
                    spectralGraph[x][y, 0] = magnitude[y];
                    spectralGraph[x][y, 1] = frequency[y];
                }               
            }
           
           
        }


        private void fft(double[] data)
        {

          //imaginary is always 0
          for (int x = 0; x < imaginary.Length; x++)
          {
             imaginary[x] = 0;
          }

          //need to fill real with audio data chunks
          
          for (int i = 0 ; i < blockSize; i ++)
          {
              real[i] = data[i] * hanningWindow[i];          
          }
          

          FFT.init(logN); 
          FFT.run(real, imaginary, false);      

        }

        public void calculateMagnitude()
        {
            //The frequency at a particular bin i is i*SAMPLE_RATE/N.
            //where N = length of FFT
            
            for (int i = 0; i < magnitude.Length; i++)
            {
                magnitude[i] = Math.Sqrt(real[i] * real[i] + imaginary[i] * imaginary[i]);
                frequency[i] = i * 48000 / blockSize;

                //apply scaling to get dB small number is to prevent zero values giving minus infinity
                magnitude[i] = scalar * Math.Log(magnitude[i] + 0.0001);
            }         
            
        }


       
    }
    
}
