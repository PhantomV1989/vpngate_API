using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace VirtualGold_checkpt_2
{
    static class vpngate
    {
        

        public static List<Dictionary<string, string>> getConnectionInfoList()
        {
            string uri = "http://www.vpngate.net/en/";
            WebResponse wr = WebRequest.Create(uri).GetResponse();
            StreamReader sr = new StreamReader(wr.GetResponseStream());
            string resp = sr.ReadToEnd();
           

            List<Dictionary<string, string>> connectionList = new List<Dictionary<string, string>>();
            var blockList = GetBlock(resp);
            foreach (string block in blockList)
            {
                Dictionary<string, string> connectionInfo = new Dictionary<string, string>();
                Dictionary<string, string> portProtocol;
                GetPortNumber(block, out portProtocol);
                connectionInfo.Add("ip", GetIP_address(block));
                connectionInfo.Add("tcp", portProtocol["tcp"]);
                connectionInfo.Add("udp", portProtocol["udp"]);
                if (connectionInfo["ip"] != "")
                {
                    connectionList.Add(connectionInfo);
                }
            }
            return connectionList;
        }

        public static List<string> GetBlock(string resp)
        {
            string subC;
            List<string> blockList = new List<string>();
            blockList.Add(ExtractString(resp, "<tr>", "</tr>", out subC));
            while (subC.IndexOf("<tr>") > 0)
            {
                blockList.Add(ExtractString(subC, "<tr>", "</tr>", out subC));
            }
            return blockList;
        }

        public static string GetIP_address(string resp)
        {
            string iden = "</span></b><br><span style='font-size: 10pt;'>";
            return ExtractString(resp, iden, "</span>");
        }

        public static void GetPortNumber(string resp, out Dictionary<string, string> portProtocol)
        {
            string tcpIdentifier = "tcp=";
            string udpIdentifier = "udp=";
            string port, protocol;
            portProtocol = new Dictionary<string, string>();


            port = ExtractString(resp, tcpIdentifier, "&");
            protocol = "tcp";
            portProtocol.Add(protocol, port);

            port = ExtractString(resp, udpIdentifier, "&");
            protocol = "udp";
            portProtocol.Add(protocol, port);

        }

        public static string ExtractString(string str, string start, string end)
        {
            string x;
            return ExtractString(str, start, end, out x);
        }

        public static string ExtractString(string str, string start, string end, out string subC)
        {
            subC = "";
            if (str.IndexOf(start) < 0) { return ""; }
            var subB = str.Substring(str.IndexOf(start) + start.Length);
            subC = str.Substring(str.IndexOf(end) + 5);
            if (subB.IndexOf(end) < 0) { return ""; }
            return subB.Substring(0, subB.IndexOf(end));
        }


        public static void ConnectToServer(Dictionary<string, string> connectionInfo)
        {
            //vpncmd localhost /client /cmd AccountCreate vpn_new /server:123.1.22.7:1217 /hub:vpngate /username:vpn /nicname:vpngate
            //vpncmd localhost /client /cmd AccountConnect vpn_new

            //or using text file
            //vpncmd localhost /client /in:C:\Users\PhantomV\Desktop\asd.txt
            //asd.txt:
            //AccountCreate vpn_new /server:123.1.22.7:1217 /hub:vpngate /username:vpn /nicname:vpngate
            //AccountConnect vpn_new
           
                string ipPort = connectionInfo["ip"] + ":";
                if (connectionInfo["tcp"] != "0")
                { ipPort += connectionInfo["tcp"]; }
                else if (connectionInfo["udp"] != "0")
                { ipPort += connectionInfo["udp"]; }
                var x = @"2""localhost""AccountCreate""vpn_new""" + ipPort + @"""vpngate""vpn""anyname""";

                if (GetStatus() != "nonexistant")
                {
                    Disconnect();
                    Delete();
                };
                //create connection
                Process.Start(new ProcessStartInfo(@"C:\vpncmd.exe")
                {
                    Arguments = @"localhost /client /cmd AccountCreate vpn_new /server:" + ipPort + @" /hub:vpngate /username:vpn /nicname:vpngate",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });



                var procConnect = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\vpncmd.exe",
                        Arguments = @"localhost /client /cmd AccountConnect vpn_new",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                procConnect.Start();
                while (!procConnect.StandardOutput.EndOfStream)
                {
                    string line = procConnect.StandardOutput.ReadLine();
                    // error code:42, 35
                }
            


        }

        public static void Disconnect()
        {
            Process.Start(new ProcessStartInfo(@"C:\vpncmd.exe")
            {
                Arguments = @"localhost /client /cmd AccountDisconnect vpn_new",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Thread.Sleep(500);
        }

        public static void Delete()
        {
            Process.Start(new ProcessStartInfo(@"C:\vpncmd.exe")
            {
                Arguments = @"localhost /client /cmd AccountDelete vpn_new",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }

        public static string GetStatus()
        {
            string status = "";
            var procConnect = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"C:\vpncmd.exe",
                    Arguments = @"localhost /client /cmd AccountStatusGet vpn_new",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            procConnect.Start();
            while (!procConnect.StandardOutput.EndOfStream)
            {
                string line = procConnect.StandardOutput.ReadLine();
                // error code:42, 35

                switch (line)
                {
                    case "The specified VPN Connection Setting is not connected.":
                        status = "disconnected";
                        break;
                    case "Session Status                            |Connection Completed (Session Established)":
                        status = "connected";
                        break;
                    case "The specified VPN Connection Setting does not exist.":
                        status = "nonexistant";
                        break;
                }
            }

            return status;
        }

    }
}
