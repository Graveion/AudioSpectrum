using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Media;
using System.Windows.Threading;

namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {
        string audiofilename = Application.StartupPath;

        Audiovis AV = new Audiovis();
        DispatcherTimer dispatcherTimer;
        SoundPlayer wavPlayer;
        double[] left;
        double[] right;
        int timeSlice = 0;
        //DataContext = this;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.StackedColumn;
            
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = 16000;
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisY.Maximum = 90;
            // chart1.ChartAreas[0].Area3DStyle.Enable3D = true;
            
            // initialise the audio first
            //string audiofilename = "C:\\Users\\tim\\downloads\\bass.wav"; //217 seconds but need a way to get song length
            //string audiofilename = Application.StartupPath; //System.IO.Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\Downloads\\bass.wav");
            audiofilename += "\\bass.wav";
            AV.openWav(audiofilename, out left, out right);

           
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            
            chart1.Series[0].Points.Clear();
            if (timeSlice < AV.spectralGraph.Length)
            {
              
                
                for (int i = 0; i < AV.magnitude.Length; i++)
                {
                    //x should be frequency whilst y is magnitude // from before 0 mag 1 freq
                    //dB should be negative but cannot figure graph so just *-1
                    chart1.Series[0].Points.AddXY(AV.spectralGraph[timeSlice][i, 1], AV.spectralGraph[timeSlice][i, 0] * -1);
                    
                }
                timeSlice++;

            
            }
            else
            {
                dispatcherTimer.Stop();
                timeSlice = 0;
                wavPlayer.Stop();


            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Hook up the Elapsed event for the timer.   
            int milli = (int)((217f / AV.spectralGraph.Length)* 1000);
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, milli); 
           
            wavPlayer = new SoundPlayer();
            wavPlayer.SoundLocation = audiofilename;  
            wavPlayer.LoadCompleted += new AsyncCompletedEventHandler(wavPlayer_LoadCompleted);

            
            dispatcherTimer.Start();
            wavPlayer.LoadAsync();
        }

        

        private void wavPlayer_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ((System.Media.SoundPlayer)sender).Play();
        }

     
    }
}


