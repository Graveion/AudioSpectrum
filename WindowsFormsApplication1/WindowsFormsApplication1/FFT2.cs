using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
   public class FFT2
    {
        // Element for linked list in which we store the
        // input/output data. We use a linked list because
        // for sequential access it's faster than array index.
        class FFTElement
        {
            public double re = 0.0;     // Real component
            public double im = 0.0;     // Imaginary component
            public FFTElement next;     // Next element in linked list
            public uint revTgt;         // Target position post bit-reversal
        }
 
        private uint m_logN = 0;        // log2 of FFT size
        private uint m_N = 0;           // FFT size
        private FFTElement[] m_X;       // Vector of linked list elements
 
        /**
         *
         */
        public FFT2()
        {
        }
 
        /**
         * Initialize class to perform FFT of specified size.
         *
         * @param   logN    Log2 of FFT length. e.g. for 512 pt FFT, logN = 9.
         */
        public void init(
            uint logN )
        {
            m_logN = logN;
            m_N = (uint)(1 << (int)m_logN);
 
            // Allocate elements for linked list of complex numbers.
            m_X = new FFTElement[m_N];
            for (uint k = 0; k < m_N; k++)
                m_X[k] = new FFTElement();
 
            // Set up "next" pointers.
            for (uint k = 0; k < m_N-1; k++)
                m_X[k].next = m_X[k+1];
 
            // Specify target for bit reversal re-ordering.
            for (uint k = 0; k < m_N; k++ )
                m_X[k].revTgt = BitReverse(k,logN);
        }
 
        /**
         * Performs in-place complex FFT.
         *
         * @param   xRe     Real part of input/output
         * @param   xIm     Imaginary part of input/output
         * @param   inverse If true, do an inverse FFT
         */
        public void run(
            double[] xRe,
            double[] xIm,
            bool inverse = false )
        {
            uint numFlies = m_N >> 1; // Number of butterflies per sub-FFT
            uint span = m_N >> 1;     // Width of the butterfly
            uint spacing = m_N;         // Distance between start of sub-FFTs
            uint wIndexStep = 1;        // Increment for twiddle table index
 
            // Copy data into linked complex number objects
            // If it's an IFFT, we divide by N while we're at it
            FFTElement x = m_X[0];
            uint k = 0;
            double scale = inverse ? 1.0/m_N : 1.0;
            while (x != null)
            {
                x.re = scale*xRe[k];
                x.im = scale*xIm[k];
                x = x.next;
                k++;
            }
 
            // For each stage of the FFT
            for (uint stage = 0; stage < m_logN; stage++)
            {
                // Compute a multiplier factor for the "twiddle factors".
                // The twiddle factors are complex unit vectors spaced at
                // regular angular intervals. The angle by which the twiddle
                // factor advances depends on the FFT stage. In many FFT
                // implementations the twiddle factors are cached, but because
                // array lookup is relatively slow in C#, it's just
                // as fast to compute them on the fly.
                double wAngleInc = wIndexStep * 2.0*Math.PI/m_N;
                if (inverse == false)
                    wAngleInc *= -1;
                double wMulRe = Math.Cos(wAngleInc);
                double wMulIm = Math.Sin(wAngleInc);
 
                for (uint start = 0; start < m_N; start += spacing)
                {
                    FFTElement xTop = m_X[start];
                    FFTElement xBot = m_X[start+span];
 
                    double wRe = 1.0;
                    double wIm = 0.0;
 
                    // For each butterfly in this stage
                    for (uint flyCount = 0; flyCount < numFlies; ++flyCount)
                    {
                        // Get the top & bottom values
                        double xTopRe = xTop.re;
                        double xTopIm = xTop.im;
                        double xBotRe = xBot.re;
                        double xBotIm = xBot.im;
 
                        // Top branch of butterfly has addition
                        xTop.re = xTopRe + xBotRe;
                        xTop.im = xTopIm + xBotIm;
 
                        // Bottom branch of butterly has subtraction,
                        // followed by multiplication by twiddle factor
                        xBotRe = xTopRe - xBotRe;
                        xBotIm = xTopIm - xBotIm;
                        xBot.re = xBotRe*wRe - xBotIm*wIm;
                        xBot.im = xBotRe*wIm + xBotIm*wRe;
 
                        // Advance butterfly to next top & bottom positions
                        xTop = xTop.next;
                        xBot = xBot.next;
 
                        // Update the twiddle factor, via complex multiply
                        // by unit vector with the appropriate angle
                        // (wRe + j wIm) = (wRe + j wIm) x (wMulRe + j wMulIm)
                        double tRe = wRe;
                        wRe = wRe*wMulRe - wIm*wMulIm;
                        wIm = tRe*wMulIm + wIm*wMulRe;
                    }
                }
 
                numFlies >>= 1;   // Divide by 2 by right shift
                span >>= 1;
                spacing >>= 1;
                wIndexStep <<= 1;     // Multiply by 2 by left shift
            }
 
            // The algorithm leaves the result in a scrambled order.
            // Unscramble while copying values from the complex
            // linked list elements back to the input/output vectors.
            x = m_X[0];
            while (x != null)
            {
                uint target = x.revTgt;
                xRe[target] = x.re;
                xIm[target] = x.im;
                x = x.next;
            }
        }
 
        /**
         * Do bit reversal of specified number of places of an int
         * For example, 1101 bit-reversed is 1011
         *
         * @param   x       Number to be bit-reverse.
         * @param   numBits Number of bits in the number.
         */
        private uint BitReverse(
            uint x,
            uint numBits)
        {
            uint y = 0;
            for (uint i = 0; i < numBits; i++)
            {
                y <<= 1;
                y |= x & 0x0001;
                x >>= 1;
            }
            return y;
        }
    }

}
