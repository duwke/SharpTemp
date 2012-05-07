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

        public double[] Temps = null;

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

                p.outputStream.WriteLine(j.Serialize(Temps));
            }
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            Console.WriteLine("POST request: {0}", p.http_url);
            string data = inputData.ReadToEnd();

            p.outputStream.WriteLine("<html><body><h1>test server</h1>");
            p.outputStream.WriteLine("<a href=/test>return</a><p>");
            p.outputStream.WriteLine("postbody: <pre>{0}</pre>", data);
        }
    }
}
