using NDesk.Options;
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ExploreQRS
{
    class Program
    {
        static void Main(string[] args)
        {

            bool help = false;
            bool version = false;
            string hostname = null, path = null, filename = null, username = null, cert = null;


            try
            {

                var p = new OptionSet()
                            {
                                {"h|host=", "{Hostname} of environment to export app from, ex. http://server.domain.com", v => hostname = v },
                                {"u|username=", "{Username} of user to connect as, ex: UserDirectory:Internal;UserId:sa_repository", v => username = v },
                                {"c|cert=", "{Certificate} FriendlyName as per MMC configuration", v => cert = v },
                                {"p|path=", "{Path} of the endpoint to get, ex. /qrs/[type]/d47b454b-b2f3-4a23-b6f9-fa74926e0234", v => path =v },
                                {"f|filename=", "{Filename} of the output QRS API response, ex. C:\\Folder\\Filename", v => filename =v },
                                {"V|version", "Show version information", v => version = v != null},
                                {"?|help", "Show usage information", v => help = v != null}
                                
                            };

                p.Parse(args);

                if (help || args.Length == 0)
                {
                    ShowHelp(p);
                    return;
                }

            }
            catch (Exception)
            {
                //LogHelper.Log(LogLevel.Error, ex.Message.Replace(Environment.NewLine, " "), new LogProperties { TaskNameOrId = t.TaskNameOrId, ExecId = "-1" });
                Environment.ExitCode = 2;
                return;
            }

            if (version)
            {
                Console.WriteLine("ExploreQRS version 20160330\n");
                Console.WriteLine("This program comes with ABSOLUTELY NO WARRANTY.");
                Console.WriteLine("This is free software, and you are welcome to redistribute it");
                Console.WriteLine("under certain conditions.\n");
                Console.WriteLine("Code: git clone git://github.com/xyz");
                Console.WriteLine("Home: <https://github.com/xyz>");
                Console.WriteLine("Bugs: <https://github.com/xyz>\n");
                return;
            }


            Environment.ExitCode = RunUserCommand(hostname, path, filename, cert, username);
            Console.WriteLine(Environment.ExitCode);

        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: QRSExplorer [options]");
            Console.WriteLine("Explore the QRS repository in Qlik Sense via command line and export to XML.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Options can be in the form -option, /option or --long-option");
        }



        public static int RunUserCommand(string qlikHost, string path, string filename, string cert, string username)
        {


            string xrfkey = "0123456789abcdef";

            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            // try
            X509Certificate myCert = null;
            //{
            var thumbprint = cert.Replace("\u200e", string.Empty).Replace("\u200f", string.Empty).Replace(" ", string.Empty);
            store.Open(OpenFlags.ReadOnly);
            foreach (X509Certificate2 mCert in store.Certificates)
            {
                if (mCert.FriendlyName.Contains(cert))
                {
                    myCert = mCert;
                }
            }


            store.Close();
            if (myCert == null)
            {
                return 1;
                //Console.WriteLine("Error: No certificate found containing that Friendly Name ");
            }

            Console.WriteLine(myCert);

            // locate the client certificate and accept it as trusted
            //X509Certificate2 myCert = new X509Certificate2("client.pfx");
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(@"" + qlikHost + ":4242" + path + "?xrfkey=" + xrfkey);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers.Add("X-Qlik-Xrfkey", xrfkey);
            request.Headers.Add("X-Qlik-User", @""+username);
            if (myCert != null) { request.ClientCertificates.Add(myCert); }
            //request.UserAgent = "Client Cert Sample";

            Console.WriteLine(request.Headers);

            // specify to run as the current Microsoft Windows user
            //request.UseDefaultCredentials = true;

            try
            {

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                
                var reader = new StreamReader(stream);
                var objText = reader.ReadToEnd();

                string rootJson = "{root:" + objText + "}";

                XNode node = JsonConvert.DeserializeXNode(rootJson, "root");

                System.IO.File.WriteAllText(@filename + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", node.ToString());

                return 0;

            }
            catch (Exception ex)
            {

                System.IO.File.WriteAllText(@"ExploreQRSLog_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", ex.ToString());

                return 9;
            }



        }

    }
}
