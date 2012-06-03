using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bend.Util;
using System.IO;
using System.Windows.Forms;

namespace SharpTemp
{
    public class MyHttpServer : HttpServer
    {
        public MyHttpServer(int port)
            : base(port)
        {
        }

        public class TempsAndAlarms
        {
            public double[] Temps = null;
            public double?[] Alarms = null;

        }

        public delegate void AlarmChangeHandler(int index, double? temp);

        // Define an Event based on the above Delegate
        public event AlarmChangeHandler NewAlarm;
        public TempsAndAlarms TempInfo = new TempsAndAlarms();

        public override void handleGETRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();

            //p.outputStream.WriteLine("<html><body><h1>test server</h1>");
            //p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
            //p.outputStream.WriteLine("url : {0}", p.http_url);

            //p.outputStream.WriteLine("<form method=post action=/form>");
            //p.outputStream.WriteLine("<input type=text name=foo value=foovalue>");
            //p.outputStream.WriteLine("<input type=submit name=bar value=barvalue>");
            //p.outputStream.WriteLine("</form>");

            if (p.http_url.Contains("www"))
            {
                string appPath = Path.GetDirectoryName(Application.ExecutablePath);
                string fileName = appPath + "/" + p.http_url.Substring(p.http_url.IndexOf("www"));
                FileInfo fi = new FileInfo(fileName);
                using (StreamReader sr = new StreamReader(fi.OpenRead()))
                {
                    p.outputStream.Write(sr.ReadToEnd());
                }
            }
            else
            {
                System.Web.Script.Serialization.JavaScriptSerializer j = new System.Web.Script.Serialization.JavaScriptSerializer();

                p.outputStream.WriteLine(j.Serialize(TempInfo));
            }
        }

        public class SetAlarmArgs
        {
            public int index;
            public string temp;
        }
        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            if (p.http_url.ToLower().Contains("setalarm"))
            {
                string args = inputData.ReadToEnd();
                System.Web.Script.Serialization.JavaScriptSerializer j = new System.Web.Script.Serialization.JavaScriptSerializer();
                SetAlarmArgs saa = j.Deserialize<SetAlarmArgs>(args);

                if (NewAlarm != null)
                {
                    if(saa.temp.Length == 0){
                        NewAlarm(saa.index, null);
                    }else{
                        double dbl = 0;
                        if(double.TryParse(saa.temp, out dbl)){
                            NewAlarm(saa.index, dbl);
                        }else{
                            NewAlarm(saa.index, null);
                        }
                    }
                }
                //NewAlarm(p.httpHeaders["temp"]);
                 p.outputStream.WriteLine( "ok");
            }
            else
            {
                Console.WriteLine("POST request: {0}", p.http_url);
                string data = inputData.ReadToEnd();

                p.outputStream.WriteLine("<html><body><h1>test server</h1>");
                p.outputStream.WriteLine("<a href=/test>return</a><p>");
                p.outputStream.WriteLine("postbody: <pre>{0}</pre>", data);
            }
        }
    }
}
