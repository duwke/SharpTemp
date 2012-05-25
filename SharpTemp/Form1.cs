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
using System.Net.Mail;
using System.Net;
using System.Configuration;

namespace SharpTemp
{
    public partial class Form1 : Form
    {
        private System.IO.Ports.SerialPort _serialPort1;
        private bool _isStarted = false;
        private double _lastAmbient = 0;
        private double _lastT0 = 0;
        private double _lastT1 = 0;
        private bool _showLast50 = false;
        private MyHttpServer _httpServer;
        private Thread _httpThread;
        private int _port = 8080;

        // alarms
        private enum Rate
        {
            RISING,
            FALLING
        }
        private Rate _t0Rate = Rate.RISING;
        private Rate _t1Rate = Rate.RISING;
        private double? _t0AlarmTemp = null;
        private double? _t1AlarmTemp = null;


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
            _httpServer = new MyHttpServer(_port);
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

            textBox1.Text = SharpTemp.Properties.Settings.Default.T0Alarm.ToString();
            textBox2.Text = SharpTemp.Properties.Settings.Default.T1Alarm.ToString();
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
            if (_isStarted)
            {
                IPHostEntry host;
                string localIP = "?";
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily.ToString() == "InterNetwork")
                    {
                        localIP = ip.ToString();
                    }
                }
                SendEmail(new string[]{"SharpTemp Started", "goto: http://" + localIP + ":" + _port.ToString() + "/www/Index.html"});
            }
        }
            
        public void NewTemp(string msg){
            msg = msg.Replace("\r", "");
            string[] datum = msg.Split(',');
            label1.Text = msg;
            btnAlarm1.Enabled = true;
            btnAlarm2.Enabled = true;
            if (datum.Length > 5)
            {
                // graph
                double index = double.Parse(datum[0]);
                _lastAmbient = double.Parse(datum[1]);
                DataPoint dp = new DataPoint(index, _lastAmbient);
                Chart1.Series["Ambient"].Points.Add(dp);
                _lastT0 = double.Parse(datum[2]);
                dp = new DataPoint(index, _lastT0);
                Chart1.Series["T0"].Points.Add(dp);
                double t0Rate = double.Parse(datum[3]);
                _lastT1 = double.Parse(datum[4]);
                double t1Rate = double.Parse(datum[5]);
                dp = new DataPoint(index, _lastT1);
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

                // webserver
                double[] temps = { index, _lastAmbient, _lastT0, t0Rate, _lastT1, t1Rate };
                _httpServer.TempInfo.Temps = temps;
                _httpServer.TempInfo.Alarms = new double?[] { _t0AlarmTemp, _t1AlarmTemp };

                // alarms
                if (_t0AlarmTemp.HasValue)
                {
                    if ((_t0Rate == Rate.RISING && _lastT0 >= _t0AlarmTemp) || _t0Rate == Rate.FALLING && _lastT0 <= _t0AlarmTemp)
                    {
                        btnAlarm1.BackColor = Color.White;
                        _t0AlarmTemp = null;
                        Thread t = new Thread(new ParameterizedThreadStart(SendEmail));
                        t.Start(new string[]{"Temp Alarm", _lastT0.ToString()});
                    }
                }
                if (_t1AlarmTemp.HasValue)
                {
                    if ((_t1Rate == Rate.RISING && _lastT1 >= _t1AlarmTemp) || _t1Rate == Rate.FALLING && _lastT1 <= _t1AlarmTemp)
                    {
                        btnAlarm2.BackColor = Color.White;
                        _t1AlarmTemp = null;
                        Thread t = new Thread(new ParameterizedThreadStart(SendEmail));
                        t.Start(new string[] { "Temp Alarm", _lastT0.ToString() });
                    }
                }
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
            if (_lastT0 < ymin)
            {
                ymin = _lastT0;
            }
            if (_lastT1 < ymin)
            {
                ymin = _lastT1;
            }
            double ymax = _lastAmbient;
            if (_lastT0 > ymax)
            {
                ymax = _lastT0;
            }
            if (_lastT1 > ymax)
            {
                ymax = _lastT1;
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

        private void SendEmail(object data)
        {
            string[] emailText = (string[])data;
            string subject = emailText[0];
            string body = emailText[1];

            
            string email = SharpTemp.Properties.Settings.Default.gmail_email;
            string password = SharpTemp.Properties.Settings.Default.gmail_password;
            var fromAddress = new MailAddress(email);
            var toAddress = new MailAddress(email);

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, password)
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body
            })
            {
                smtp.Send(message);
            }
        }

        private void btnAlarm1_Click(object sender, EventArgs e)
        {
            double tmp;
            if (!_t0AlarmTemp.HasValue && double.TryParse(textBox1.Text, out tmp))
            {
                _t0AlarmTemp = tmp;
                btnAlarm1.BackColor = Color.Red;
                if (_lastT0 < _t0AlarmTemp)
                {
                    _t0Rate = Rate.RISING;
                }
                else
                {
                    _t0Rate = Rate.FALLING;
                }
                SharpTemp.Properties.Settings.Default.T0Alarm = tmp;
            }
            else
            {
                _t0AlarmTemp = null;
                btnAlarm1.BackColor = Color.White;
            }
        }

        private void btnAlarm2_Click(object sender, EventArgs e)
        {
            double tmp;
            if (!_t1AlarmTemp.HasValue && double.TryParse(textBox2.Text, out tmp))
            {
                _t1AlarmTemp = tmp;
                btnAlarm2.BackColor = Color.Red;
                if (_lastT1 < _t1AlarmTemp)
                {
                    _t1Rate = Rate.RISING;
                }
                else
                {
                    _t1Rate = Rate.FALLING;
                }
                SharpTemp.Properties.Settings.Default.T1Alarm = tmp;
            }
            else
            {
                _t1AlarmTemp = null;
                btnAlarm2.BackColor = Color.White;
            }

        }

    }
}
