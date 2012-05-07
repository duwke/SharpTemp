using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace SharpTemp
{
    public partial class Form1 : Form
    {
        private System.IO.Ports.SerialPort _serialPort1;
        private bool _isStarted = false;
        private double _lastAmbient = 0;
        private double _lastT1 = 0;
        private double _lastT2 = 0;
        private bool _showLast50 = false;
        private MyHttpServer _httpServer;
        private Thread _httpThread;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            System.ComponentModel.IContainer components = new System.ComponentModel.Container();
            _serialPort1 = new System.IO.Ports.SerialPort(components);
            _serialPort1.PortName = "COM3";
            _serialPort1.BaudRate = 57600;

            _serialPort1.Open();
            if (!_serialPort1.IsOpen)
            {
                Console.WriteLine("Oops");
                return;
            }
            _httpServer = new MyHttpServer(8080);
            _httpThread = new Thread(new ThreadStart(_httpServer.listen));
            _httpThread.Start();
            
            // this turns on !
            _serialPort1.DtrEnable = true;

            // callback for text coming back from the arduino
            _serialPort1.DataReceived += OnReceived;

            Thread t = new Thread(new ThreadStart(StartLogger));
            t.Start();

            Chart1.Legends.Add("chtArea");
            Chart1.Series.Clear();
            Chart1.Series.Add("Ambient");

            Chart1.Series["Ambient"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            Chart1.Series["Ambient"].IsVisibleInLegend = true;
            Chart1.Series["Ambient"].ToolTip = "Data Point Y Value: #VALY{G}";

            Chart1.Series.Add("T0");
            Chart1.Series["T0"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            Chart1.Series.Add("T1");
            Chart1.Series["T1"].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;


            Chart1.ChartAreas[0].AxisX.Title = "Time";
            Chart1.ChartAreas[0].AxisX.TitleFont = new System.Drawing.Font("Verdana", 10, System.Drawing.FontStyle.Bold);
            Chart1.ChartAreas[0].AxisY.Title = "Temp";
            Chart1.ChartAreas[0].AxisY.TitleFont = new System.Drawing.Font("Verdana", 10, System.Drawing.FontStyle.Bold);
        }

        private void StartLogger()
        {
            // give it 2 secs to start up the sketch
            System.Threading.Thread.Sleep(2000);

            for (int i = 0; i < 10 && !_isStarted; i++)
            {
                _serialPort1.WriteLine("RESET");
                Console.WriteLine("Send Reset");
                System.Threading.Thread.Sleep(500);
            }
        }
            
        public void NewTemp(string msg){
            string[] datum = msg.Split(',');
            label1.Text = msg;
            if (datum.Length > 5)
            {
                _lastAmbient = double.Parse(datum[1]);
                DataPoint dp = new DataPoint(double.Parse(datum[0]), _lastAmbient);
                Chart1.Series["Ambient"].Points.Add(dp);
                _lastT1 = double.Parse(datum[2]);
                dp = new DataPoint(double.Parse(datum[0]), _lastT1);
                Chart1.Series["T0"].Points.Add(dp);
                _lastT2 = double.Parse(datum[4]);
                dp = new DataPoint(double.Parse(datum[0]), _lastT2);
                Chart1.Series["T1"].Points.Add(dp);

                if (_showLast50 && Chart1.Series["Ambient"].Points.Count > 50)
                {
                    Chart1.ChartAreas[0].AxisX.ScaleView.Position = Chart1.Series["Ambient"].Points.Count - 50;
                    Chart1.ChartAreas[0].AxisX.ScaleView.Size = 50;
                }
                else
                {
                    Chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                }
                _httpServer.Temps = new double[] { _lastAmbient, _lastT1, _lastT2 };
            }

        }
        public delegate void NewTempDelegate(string msg);


        private void OnReceived(object sender, SerialDataReceivedEventArgs c)
        {
            try
            {
                // write out text coming back from the arduino
                string msg = _serialPort1.ReadLine();
                Console.WriteLine("a - " + msg);
                if (msg.StartsWith("# Reset"))
                {
                    _isStarted = true;
                }else if(!msg.StartsWith("#")){
                    NewTempDelegate handler = NewTemp;
                    this.Invoke(handler, new object[] { msg });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            double ymin = _lastAmbient;
            if (_lastT1 < ymin)
            {
                ymin = _lastT1;
            }
            if (_lastT2 < ymin)
            {
                ymin = _lastT2;
            }
            double ymax = _lastAmbient;
            if (_lastT1 > ymax)
            {
                ymax = _lastT1;
            }
            if (_lastT2 > ymax)
            {
                ymax = _lastT2;
            }
            Chart1.ChartAreas[0].AxisY.ScaleView.Position = ymin - 1;
            Chart1.ChartAreas[0].AxisY.ScaleView.Size = ymax - ymin + 2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _showLast50 = !_showLast50;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _httpThread.Abort();
            // this will close the port
            using (_serialPort1)
            {
                _serialPort1.DataReceived -= OnReceived;
                _serialPort1.DtrEnable = false;
                _serialPort1.DiscardOutBuffer();
                _serialPort1.Close();
            }
        }
    }
}
