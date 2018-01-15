using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Crawler
{
    public class Client
    {
        private Socket _MainSocket = null;
        private string _Host = String.Empty;
        private string _Port = "80";

        public string Xml { get; set; }
        public string Page { get; set; }
        public int DepthOfCrawling { get; set; }
        public string HTMLResult { get; set; }
        public Client(string host) : this(host, 3) { }
        public Client(string host, int depth)
        {
            if (String.IsNullOrEmpty(host)) throw new Exception("It's nessesary to add http-server.");
            if (depth <= 0) depth = 1;
            // --
            _Host = host;
            _Port = "80";
            DepthOfCrawling = depth;
            Page = "/";
            Connect();
        }
        /// <summary>
        /// Connection to server.
        /// </summary>
        public void Connect()
        {
            Xml = "<? xml version = '1.0' encoding = 'utf‐8' ?>\r\n";
            Xml += "<SITE url='http://" + _Host + "' depth='" + DepthOfCrawling.ToString() + "'>\r\n";

            IPHostEntry myIPHostEntry = Dns.GetHostEntry(_Host);

            if (myIPHostEntry == null || myIPHostEntry.AddressList == null || myIPHostEntry.AddressList.Length <= 0)
            {
                throw new Exception("Can't connect to IP.");
            }
            IPEndPoint myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], int.Parse(_Port));

            _MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _MainSocket.ReceiveBufferSize = 512; 
            
            WriteToLog("Connect to server: {0}:{1}", _Host, _Port);
            _MainSocket.Connect(myIPEndPoint);
            Command("GET "+Page+" HTTP/1.1\r\nHost: "+_Host+":"+_Port.ToString()+"\r\nConnection: keep - alive\r\n\r\n");
            HTMLResult = ReadToEnd();
        }
        #region Close(),Command(string)


        /// <summary>
        /// Close connection with server.
        /// </summary>
        public void Close()
        {
            if (_MainSocket == null) { return; }
            Command("QUIT");
            ReadToEnd();
            _MainSocket.Close();
        }
        /// <summary>
        /// Sending command to server
        /// </summary>
        /// <param name="cmd">Command</param>
        public void Command(string cmd)
        {
            if (_MainSocket == null) throw new Exception("Connection is interrupt.");
            WriteToLog("Command {0}", cmd);// logging
            byte[] b = System.Text.Encoding.ASCII.GetBytes(String.Format("{0}\r\n", cmd));
            if (_MainSocket.Send(b, b.Length, SocketFlags.None) != b.Length)
            {
                throw new Exception("An error has occured");
            }
        }
        #endregion
        public string CreateXML(int depth)
        {
            foreach (string item in ParseHTML("images"))
            {
                string[] buff = item.Split('"');
                for (int i = 0; i < buff.Length; i++)
                {
                    if (buff[i].Contains("src="))
                    {
                        Xml += "<IMAGE>" + buff[i+1] + "</IMAGE>\r\n";
                    }
                    
                }
            }//add images to xml
            foreach (string item in ParseHTML("mails"))
            {
                Xml += "<EMAIL>" + item + "</EMAIL>\r\n";
            }//have to add email
            foreach (string item in ParseHTML("links"))
            {
                Xml += "<FILE " + item + "</FILE>\r\n";
                if (item.Contains("index"))
                {
                    string[] buff = item.Split('"');
                    for (int i = 0; i < buff.Length; i++)
                    {
                        if (buff[i].Contains("/"))
                        {
                            int ind = buff[i].IndexOf("index.html");
                            Page += buff[i].Substring(0,ind);
                        }
                    }
                    Command("GET " + Page + " HTTP/1.1\r\nHost: " + _Host + ":" + _Port.ToString() + "\r\nConnection: keep - alive\r\n\r\n");
                    Page = "/";
                    if (depth > 0)
                    {
                        HTMLResult = ReadToEnd();
                        CreateXML(depth - 1);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    string[] buff = item.Split('"');
                    for (int i = 0; i < buff.Length; i++)
                    {
                        if (buff[i].Contains("http"))
                        {
                            _Host = buff[i].Substring(buff[i].LastIndexOf("/")+1);                            
                        }
                    }
                    Command("GET " + Page + " HTTP/1.1\r\nHost: " + _Host + ":" + _Port.ToString() + "\r\nConnection: keep - alive\r\n\r\n");
                    _Host = "detox.wi.ps.pl";
                    if (depth > 0)
                    {
                        HTMLResult = ReadToEnd();
                        CreateXML(depth - 1);
                    }
                    else
                    {
                        break;
                    }
                }//go to out pages. not work with extended references. made to limit outer crawling.
            }
            return Xml;
        }
        #region ReadToEnd()
        /// <summary>
        /// Read and take back all data from socket.
        /// </summary>
        public string ReadToEnd()
        {
            byte[] b = new byte[_MainSocket.ReceiveBufferSize];
            StringBuilder result = new StringBuilder(_MainSocket.ReceiveBufferSize);
            int s = 0;

            while (_MainSocket.Poll(1000000, SelectMode.SelectRead) && ((s = _MainSocket.Receive(b, _MainSocket.ReceiveBufferSize, SocketFlags.None)) > 0))
            {
                result.Append(Encoding.ASCII.GetChars(b, 0, s));
            }
            WriteToLog("Answer: {0}",result.ToString());
            return result.ToString();
        }
        #endregion
        /// <summary>
        /// Parse from html to list of links,images or mail addresses.
        /// </summary>
        /// <param name="parser">Parser "links","images","mails".</param>
        /// <returns></returns>
        public List<string> ParseHTML(string parser)
        {
            string[] tags = HTMLResult.Split('<');
            List<string> result = new List<string>();
            switch (parser)
            {
                case ("links"):
                    foreach (string item in tags)
                    {
                        if (item.StartsWith("a"))
                        {
                            result.Add(item);
                        }
                    };
                    break;
                case ("images"):
                    foreach (string item in tags)
                    {
                        if (item.StartsWith("img"))
                        {
                            result.Add(item);
                        }
                    };
                    break;
                case ("mails"):
                    foreach (string item in tags)
                    {
                        if (item.Contains("@"))
                        {
                            result.Add(item);
                        }
                    };
                    break;
                default:
                    result.Add("Wrong parametres.");
                    break;
            }
            return result;
        }
        /// <summary>
        /// Запись лога.
        /// </summary>
        /// <param name="msg">Строчное сообщение</param>
        /// <param name="args">Объект для вывода</param>
        private void WriteToLog(string msg, params object[] args)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now, String.Format(msg, args));
        }

        #region ReadToEnd(socket)
        /// <summary>
        /// Read and take back alldata from socket.
        /// </summary>
        /// <param name="socket">Out socket for reading from it.</param>
        /// <returns></returns>
        public string ReadToEnd(Socket socket)
        {
            byte[] b = new byte[socket.ReceiveBufferSize];
            StringBuilder result = new StringBuilder(socket.ReceiveBufferSize);
            int s = 0;

            while (socket.Poll(100000, SelectMode.SelectRead) && ((s = socket.Receive(b, socket.ReceiveBufferSize, SocketFlags.None)) > 0))
            {
                result.Append(System.Text.Encoding.ASCII.GetChars(b, 0, s));
            }
            if (result.Length > 0 && result.ToString().IndexOf("\r\n") != -1)
            {
                WriteToLog(result.ToString().Substring(0, result.ToString().IndexOf("\r\n")));
            }
            return result.ToString();
        }
        #endregion
    }
}
