using AxWMPLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Profile;
using System.Windows.Forms;
using System.IO;

using NewClient = System.Collections.Generic.KeyValuePair<int, string>;
using System.Net.NetworkInformation;
using System.Xml;
using System.Xml.XPath;
using System.Windows.Forms.VisualStyles;

namespace Blaze_Music
{

    public partial class Form1 : Form
    {
        string thistitle;
        #region TCPSERVER
        TcpListener server;
        int pollcount = 0;

        IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 8080);
        Thread tcpsrv;
        List<NewClient> clientlist = new List<NewClient>();
        List<NewClient> clientnames = new List<NewClient>();
        Boolean servrun = true;
        String vbcrlf = System.Environment.NewLine;

        List<TcpClient> clients = new List<TcpClient>();
        List<String> inccmds = new List<String>();
        int clientcount = 0;

        //i was going to use the below code for data pairs but i couldnt overwrite. leaving it in incase you need reference to it for any future projects.
        public struct Client
        {
            public Client(int Identifier, EndPoint RemoteEndPoint, string ClientName)
            {
                ClientID = Identifier;
                Name = ClientName;
                ClientIP = RemoteEndPoint;
            }
            public int ClientID { get; set; }
            public string Name { get; set; }
            public EndPoint ClientIP { get; set; }

        }

        static string configpath = Application.StartupPath + "\\Config\\Config.xml";
        static string myzoneip = "192.168.200.45";
        static string musicserverip = "192.168.200.46";
        static string Dir45 = "C:\\Music\\45 Minutes\\";
        static string Dir55 = "C:\\Music\\55 Minutes\\";
        static Boolean runMusic = true;
        static Boolean runBlaze = true;

        public static void readconfig()
        {
            if(!Directory.Exists(Application.StartupPath + "\\Config"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\Config");

            }

            if(!File.Exists(configpath))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<config>");
                sb.AppendLine("<myzoneip>192.168.200.45</myzoneip>");
                sb.AppendLine("<musicserverip>192.168.200.46</musicserverip>");
                sb.AppendLine("<Dir45>C:\\Music\\45 Minutes\\</Dir45>");
                sb.AppendLine("<Dir55>C:\\Music\\55 Minutes\\</Dir55>");
                sb.AppendLine("<RunBlaze>True</RunBlaze>");
                sb.AppendLine("<RunMusic>True</RunMusic>");
                sb.AppendLine("</config>");
                File.WriteAllText(configpath, sb.ToString());
            }

            XmlReaderSettings xrs = new XmlReaderSettings();
            XmlReader xr = XmlReader.Create(File.OpenRead(configpath),xrs);
            xr.MoveToContent();
            while (xr.Read())
            {
                if(xr.NodeType == XmlNodeType.Element)
                {
                    switch (xr.Name.ToLower()){
                        case "myzoneip":

                           myzoneip =  xr.ReadInnerXml().ToString();    
                        break;
                        case "musicserverip":
                            musicserverip = xr.ReadInnerXml().ToString();
                        break;
                        case "dir45":
                            Dir45 = xr.ReadInnerXml().ToString();
                            break;
                        case "dir55":
                            Dir55 = xr.ReadInnerXml().ToString();
                            break;
                        case "runmusic":
                            try
                            {
                                runMusic = Boolean.Parse(xr.ReadInnerXml().ToString());
                            }catch(Exception ex)
                            {
                                runMusic = true;

                            }
                        break;
                        case "runblaze":
                            try
                            {
                                runBlaze = Boolean.Parse(xr.ReadInnerXml().ToString());
                            }catch(Exception ex)
                            {
                                runBlaze = false;
                            }
                            
                        break;


                    }
                    
                }
            }
           
            xr.Close();
            xr.Dispose();

        }



        private void Tcpserver()
        {
            server = new TcpListener(ipe);
            server.Start();
            server.Server.ReceiveTimeout = 10;
            server.Server.SendTimeout = 10;

            while (servrun == true)
            {
                try
                {

                    //if server has a pending client connection executes as below
                    if (server.Pending() == true)
                    {

                        //starts a new thread and passes the accepttcpclient into that thread to manage along with the current clientid and a name if you want to assign one here, names could be pulled from XML file by the client below if required.
                        Thread runclient = new Thread(() => Tcpclientmgr(server.AcceptTcpClient(), clientcount, ""));
                        //determine thread as background so we can close it later
                        runclient.IsBackground = true;
                        //duh start the thread.
                        runclient.Start();


                    }
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
                Thread.Sleep(10);
            }
            server.Stop();
            Thread.CurrentThread.Abort();
        }

        private void Tcpclientmgr(TcpClient tcpc, int clientid, string clientname)
        {
            //adds 1 to the program clientcount once this has been established so sync is maintained.
            clientcount++;
            int myid = clientid;
            //tcrun is used on a per thread basis to determine if this individual thread should still run
            Boolean tcrun = true;



            int cmdreadcnt = inccmds.Count();
            NewClient thisclient = new NewClient(myid, tcpc.Client.RemoteEndPoint.ToString());

            //add our id and remote endpoint to the clientlist
            clientlist.Add(thisclient);
            //add our id and name to the clientlist - this is the list we will use later for our friendly name

            //overwrites endpoint client identifier with our naming client that can be modified later
            if (clientname != "")
            {
                thisclient = new NewClient(myid, clientname);
            }

            //adds the client to the list
            clientnames.Add(thisclient);


            byte[] nbuff = Encoding.ASCII.GetBytes("Client Connected:" + myid + vbcrlf + "To Send Shared Server Messages Use Chat:<Message>" + vbcrlf);
            //tcpc.Client.Send(nbuff);
            tcpc.ReceiveTimeout = 10;
            tcpc.SendTimeout = 10;
            //servrun is main boolean to determine if whole program should execute
            //tcrun is the boolean to determine if each individual threaded tcpclient should run or not
        
                //sends data to tcpclient on each cycle of the while loop to determine if it is still open, if not it closes so as not to hog threads.
                try
                {
                    byte[] tempbuff = new byte[1];
                    //peek must be used so that we dont tie up the main receive buffer
                    if (tcpc.Client.Receive(tempbuff, SocketFlags.Peek) < 1)
                    {
                        tcpc.Close();
                        tcpc.Client.Close();
                        tcrun = false;
                        Thread.CurrentThread.Abort();


                    }

                }
                catch (Exception excnct)
                {
                //WriteLog("TCP Server Error: " + excnct.ToString());
            }
                try
                {
                    //this.Invoke(new Action(() => this.Text = string.Format("Hutchison Technologies - Fiit Manager {0}", pollcount) + " - " + cmdreadcnt + "-" + inccmds.Count()));
                    //open network stream from tcp client
                    NetworkStream ns = tcpc.GetStream();
                    // must set timeouts so that thread does not hang on read
                    ns.ReadTimeout = 10;
                    ns.WriteTimeout = 10;

                    byte[] buffer = new byte[1024];

                    String unencbuff = null;


                    //makes sure data is available in the network stream before trying to read
                    if (ns.DataAvailable == true)
                    {
                        //makes sure reading is possible before trying to read
                        if (ns.CanRead == true)
                        {
                            // reads buffer value to byte
                            ns.Read(buffer, 0, 1024);

                            //steps through each byte in the byte array to make sure no dead data is held in memory if byte = 0 then it is not commited
                            foreach (byte b in buffer)
                            {
                                if (b != 0)
                                {
                                    unencbuff += Convert.ToChar(b);


                                }

                            }

                       // writecmd(unencbuff);

                        try
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\" ?>");
                            sb.AppendLine("<root>");
                            
                            string respbody = "<html><body><h1> Hello, Richard!</h1></body></html> ";
                            string response = "HTTP/1.1 200 OK\r\nDate: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nServer: Graeme Hind / 1.0 (Win64)\r\nLast - Modified: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nContent - Length: " + respbody.Length + "\r\nContent - Type: text / xml\r\nConnection: Closed";

                            //tries to split the incoming commands, this stops us having to user .compare and creates a more specific environment where we can specify command[1] = a result
                            String[] command = new string[2];
                        
                            command = unencbuff.Split(Convert.ToChar(":"));

                            

                            string[] responselines;
                            string[] separators = { "\r\n" };

                            responselines = unencbuff.Split(separators, StringSplitOptions.None);

                       
                            string searchline = "GET /requests/status.xml";
                            string actcmd = "";
                            string input = "";
                            try
                            {
                                string cururl = "http://127.0.0.1:8080" + responselines[0].Replace("GET ", "").Replace("HTTP/1.1", "");
                               
                                Uri nuri = new Uri(cururl);
                                 actcmd = HttpUtility.ParseQueryString(nuri.Query).Get("command");
                                 input = HttpUtility.ParseQueryString(nuri.Query).Get("input");
                            }
                            catch(Exception ex)
                            {
                                
                            }



                            try
                            {
                                if (actcmd.ToLower() == "in_play")
                                {
                                    input = HttpUtility.UrlDecode(input);

                                    WMP.URL = HttpUtility.HtmlDecode(input);


                                    Thread.Sleep(200);

                                    if (WMP.playState.ToString() == "wmppsPlaying")
                                    {
                                        skpTime.Maximum = Convert.ToInt32(WMP.currentMedia.duration);
                                        skpTime.Minimum = 0;
                                        skpTime.Enabled = true;
                                        btnSkip.Enabled = true;
                                        this.Invoke(new Action(() => this.Text = thistitle + " - Playing:" + WMP.currentMedia.name));
                                        Thread ntrd = new Thread(() => MusicFade("play"));
                                        ntrd.IsBackground = true;
                                        ntrd.Start();
                                    }

                                }
                                else if (actcmd.ToLower() == "pl_stop")
                                {
                                    //Thread ntrd = new Thread(() => MusicFade("stop"));
                                    //ntrd.IsBackground = true;
                                    //ntrd.Start();
                                    WMP.Invoke(new Action(() => WMP.Ctlcontrols.stop()));

                                }
                                else if (actcmd.ToLower() == "pl_empty")
                                {
                                    // Thread ntrd = new Thread(() => MusicFade("stop"));
                                    //ntrd.IsBackground = true;
                                    //ntrd.Start();

                                    WMP.Invoke(new Action(() => WMP.Ctlcontrols.stop()));
                                    WMP.Invoke(new Action(() => WMP.URL = ""));

                                }
                                else if (actcmd.ToLower() == "pause")
                                {
                                    //Thread ntrd = new Thread(()=> MusicFade("pause"));
                                    //ntrd.IsBackground = true;
                                    //ntrd.Start();
                                    WMP.Invoke(new Action(() => WMP.Ctlcontrols.pause()));

                                }
                                else if (actcmd.ToLower() == "play")
                                {
                                    Thread ntrd = new Thread(() => MusicFade("play"));
                                    ntrd.IsBackground = true;
                                    ntrd.Start();


                                }
                                else if (actcmd.ToLower() == "loop")
                                {
                                    if (input == "1")
                                    {
                                        WMP.settings.setMode("loop", true);
                                    }
                                    else if (input == "0")
                                    {
                                        WMP.settings.setMode("loop", false);
                                    }


                                }

                            }catch(Exception ex)
                            {

                            }
                            

                            try
                                {
                                    sb.AppendLine("<length>" + Convert.ToInt32(WMP.currentMedia.duration) + "</length>");
                                }catch(Exception ex)
                                {
                                    sb.AppendLine("<length>0</length>");
                                }
                                
                                sb.AppendLine("<time>" + Convert.ToInt32(WMP.Ctlcontrols.currentPosition) + "</time>");

                                if (WMP.playState.ToString() == "wmppsUndefined")
                                {

                                    sb.AppendLine("<state>stopped</state>");
                                }
                                else if (WMP.playState.ToString() == "wmppsPlaying")
                                {
                                    sb.AppendLine("<state>playing</state>");
                                }
                                else if (WMP.playState.ToString() == "wmppsStopped")
                                {

                                    sb.AppendLine("<state>stopped</state>");
                                }
                                else
                                {
                                    sb.AppendLine("<state>stopped</state>");
                                }
                            try
                            {
                                sb.AppendLine("<track>" + WMP.currentMedia.name + "</track>");
                            }  catch(Exception ex)
                            {

                            }
                                sb.AppendLine("</root>");
                                byte[] nbuffinc = Encoding.ASCII.GetBytes(response + "\r\n\r\n" + sb.ToString());
                                ns.Write(nbuffinc, 0, nbuffinc.Length);

                           

                        }
                            catch (Exception ex)
                            {
                               // MessageBox.Show(ex.ToString());
                            }


                        }

                    }

                    try
                    {

                        // for loop to cycle through all messages sitting in our list of messages which we earlier removed dead bytes from and commited 
                        /*  for (int i = cmdreadcnt; i < inccmds.Count; i++)
                          {
                              byte[] nbuffinc = Encoding.ASCII.GetBytes(inccmds[i]);

                              //keeps the regional variable in sync
                              cmdreadcnt++;

                              //write messages from the previous messages buffer to the connected client
                              if (ns.CanWrite == true)
                              {
                                  ns.Write(nbuffinc, 0, nbuffinc.Length);

                              }
                          }
                          // ns.Write()
                          */

                    }
                    catch (Exception ex)
                    {
                    WriteLog("TCP Client Manager: " + ex.ToString());


                }





                }
                catch (Exception mainex)
                {
                WriteLog("TCP Client Manager:" + mainex.ToString());
            }
                //sleep the thread for 1ms so it does not hog resources
                Thread.Sleep(1);




            
            //if exited from the while loop close the tcp client and abort the thread
            tcpc.Close();
            Thread.CurrentThread.Abort();
        }


        private string processrequest(string[] parameter)
        {

            string cmd = parameter[0];

            if (cmd == "End")
            {
                servrun = false;

                Application.Exit();


            }
            else if (cmd == "")
            {
                cmd = "NAK";


            }
            else if (cmd == "EndClient")
            {
                cmd = "<ENDCLIENT>";
            }
            else if (cmd == "Restart")
            {
                Application.Restart();
            }


            return cmd;

        }
        #endregion
        public Form1()
        {
            InitializeComponent();
        }

  
        HTProgressBar htpb = new HTProgressBar();
        static string logdir = Application.StartupPath + "\\Logs";

        private static void WriteLog(string log)
        {
            //a
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("----------------------------" + DateTime.Now.ToString("HH:mm:ss") + "---------------------------");
                sb.AppendLine(log);
                sb.AppendLine("---------------------------------------------------------------");
                File.AppendAllText(logdir + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".log", sb.ToString());
            }catch(Exception WriteLogError)
            {

            }
        }

        private void WMP_OnError(object sender, EventArgs args)
        {
            WriteLog(args.ToString());
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            readconfig();
            thistitle = this.Text;
            WriteLog("Program Started");
            
            
            if (runBlaze == true)
            {
                MyzoneServer mz = new MyzoneServer();
                mz.StartServer();
            }
            
            
            if(!Directory.Exists(logdir))
            {
                Directory.CreateDirectory(logdir);
                WriteLog("Created Log Dir");
            }

            if (runMusic == true)
            {
                this.FormClosing += new FormClosingEventHandler(form_closing);
                htpb.Width = 393;
                htpb.Height = 10;
                htpb.MouseClick += new MouseEventHandler(htpb_click);
                htpb.Location = new Point(4, 210);
                htpb.MouseMove += new MouseEventHandler(htpb_hover);

                WMP.ErrorEvent += new EventHandler(WMP_OnError);

                htpb.BringToFront();
                this.Controls.Add(htpb);
                try
                {
                    SetCurrentEffectPreset(4);
                    WMP.BeginInit();


                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }

                
                WMP.settings.volume = 0;
                WMP.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(player_PlayStateChange);

                WMP.enableContextMenu = false;
                WMP.uiMode = "none";
                try
                {
                    Thread nthread = new Thread(Tcpserver);
                    nthread.IsBackground = true;
                    nthread.Start();
                }catch(Exception ex)
                {
                    WriteLog("Cannot Start Music Web Server");
                }
                }
            else
            {
                WMP.Visible = false;
                btnSkip.Visible = false;
                skpTime.Visible = false;
                wmpTimer.Enabled = false;
                lblCurTime.Visible = false;
                if(runBlaze==true)
                {
                    this.Text = thistitle + " - Running Blaze Server Only";
                }
                else
                {
                    this.Text = thistitle + " - No Services Running";
                }
            }

        }
        ToolTip newtt = new ToolTip();
        private void htpb_hover(object sender, MouseEventArgs args)
        {

            
            try
            {
                int progperc = Convert.ToInt32(args.X / (3.93));



                double trackperc = WMP.currentMedia.duration / 100;


                DateTime dt1 = new DateTime();

                dt1 = dt1.AddSeconds(progperc * trackperc + 2);
                DateTime dt2 = new DateTime();
                dt2 = dt2.AddSeconds(Convert.ToInt32(WMP.currentMedia.duration));


                newtt.Show(dt1.ToString("mm:ss") + "/" + dt2.ToString("mm:ss"),htpb,args.X + 10,args.Y + 10,1000);

            }
            catch (Exception ex)
            {
                WriteLog("Player Progress Error - " + ex.ToString());

            }
        }

        private void htpb_click(object sender, MouseEventArgs args)
        {
            try
            {
                int progperc = Convert.ToInt32(args.X / (3.93));

              

                double trackperc = WMP.currentMedia.duration / 100;



                WMP.Ctlcontrols.currentPosition = Convert.ToDouble(progperc * trackperc + 2);

                htpb.Maximum = Convert.ToInt32(WMP.currentMedia.duration);
                htpb.Value = Convert.ToInt32(WMP.Ctlcontrols.currentPosition);
            }catch(Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }

        private void WMP_Enter(object sender, EventArgs e)
        {

        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            WMP.Ctlcontrols.currentPosition = Convert.ToDouble(skpTime.Value);

        }


        private void player_PlayStateChange(object sender, _WMPOCXEvents_PlayStateChangeEvent e)
        {
            try
            {
                if (e.newState != 3)
                {
                    this.Text = thistitle;
                    btnSkip.Enabled = false;
                    skpTime.Enabled = false;
                }
                else
                {
                    btnSkip.Enabled = true;
                    skpTime.Enabled = true;
                    this.Invoke(new Action(() => this.Text = thistitle + " - Playing:" + WMP.currentMedia.name));
                }

                if (e.newState == 8)
                {
                    WMP.URL = "";
                }
            }catch(Exception ChangeError)
            {
                WriteLog("Change Error:" + ChangeError.ToString());
            }
        }

        private void form_closing(object sender, EventArgs args)
        {
        
            this.Text = thistitle + " Fading Output to 0";
            MusicFade("stop");
            WriteLog("Closing Application");
        }

        private void MusicFade(string cmd)
        {
            try
            {
                Boolean fadein = false;
                int curvol = WMP.settings.volume;
                if (WMP.settings.volume > 1 && cmd != "play")
                {
                    while (curvol > 1)
                    {
                        WMP.settings.volume -= 1;
                        Thread.Sleep(25);
                        curvol = WMP.settings.volume;

                    }
                    switch (cmd)
                    {
                        case "pause":
                            WMP.Ctlcontrols.pause();
                            break;
                        case "stop":
                            WMP.Ctlcontrols.stop();
                            WMP.URL = "";
                            break;
                    }

                }
                else
                {
                    if (cmd != "stop" && cmd != "pause")
                    {
                        WMP.Ctlcontrols.play();
                        while (curvol < 100)
                        {
                            WMP.settings.volume += 1;
                            Thread.Sleep(25);
                            curvol = WMP.settings.volume;
                        }
                    }
                }
            }catch(Exception FadeError)
            {
                WriteLog("Fade in/out Error: " + FadeError.ToString());
            }
           


        }
        public void SetCurrentEffectPreset(int value)
        {
            try
            {
                WindowsIdentity identiry = WindowsIdentity.GetCurrent();
                String path = String.Format(@"{0}\Software\Microsoft\MediaPlayer\Preferences", identiry.User.Value);
                RegistryKey key = Registry.Users.OpenSubKey(path, true);
                if (key == null)
                    throw new Exception("Registry key not found!");
                key.SetValue("CurrentEffectPreset", value, RegistryValueKind.DWord);
            }catch(Exception EffectPresetError)
            {
                WriteLog("Visualisation Error: " + EffectPresetError.ToString());
            }
        }

        private void wmpTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                DateTime ts = new DateTime();
                ts = ts.AddSeconds(WMP.Ctlcontrols.currentPosition);

                if (lblCurTime.Text != ts.ToString("mm:ss"))
                {
                    lblCurTime.Text = ts.ToString("mm:ss");

                    try
                    {
                        htpb.Maximum = Convert.ToInt32(WMP.currentMedia.duration);
                        htpb.Value = Convert.ToInt32(WMP.Ctlcontrols.currentPosition);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.ToString());
                    }
                }
            }
            catch (Exception WMPTimerError)
            {
                WriteLog(WMPTimerError.ToString());
            }
            }

        public class HTProgressBar : ProgressBar
        {
            public HTProgressBar()
            {
                this.SetStyle(ControlStyles.UserPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                try
                {
                    Rectangle rec = e.ClipRectangle;

                    rec.Width = (int)(rec.Width * ((double)Value / Maximum)) - 4;
                    if (ProgressBarRenderer.IsSupported)
                        ProgressBarRenderer.DrawHorizontalBar(e.Graphics, e.ClipRectangle);
                    rec.Height = rec.Height - 4;
                    Brush htblue = new SolidBrush(Color.FromArgb(255, 83, 192, 231));

                    e.Graphics.FillRectangle(htblue, 2, 2, rec.Width, rec.Height);
                }catch(Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
        }

        public partial class MyzoneServer
        {
            string thistitle;
            #region TCPSERVER
            static TcpListener server;
            static int pollcount = 0;
            
            static IPEndPoint ipe = new IPEndPoint(IPAddress.Any, 8888);
            static Thread tcpsrv;
            static List<NewClient> clientlist = new List<NewClient>();
            static List<NewClient> clientnames = new List<NewClient>();
            static Boolean servrun = true;
            static String vbcrlf = System.Environment.NewLine;

            static List<TcpClient> clients = new List<TcpClient>();
            static List<String> inccmds = new List<String>();
            static int clientcount = 0;

            public void StartServer()
            {
          
                if(!Directory.Exists(logdir + "\\Play-Logs\\"))
                {
                    Directory.CreateDirectory(logdir + "\\Play-Logs");
                }

                if(!File.Exists(logdir + "\\Play-Logs\\ClassLog-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv"))
                {
                    File.WriteAllText(logdir + "\\Play-Logs\\ClassLog-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv", "Date,Class Type,Class Number\r\n");

                }
                tcpsrv = new Thread(Tcpserver);
                tcpsrv.IsBackground = true;
                if (!tcpsrv.IsAlive)
                {
                    tcpsrv.Start();
                }
                }

            //i was going to use the below code for data pairs but i couldnt overwrite. leaving it in incase you need reference to it for any future projects.
            public struct Client
            {
                public Client(int Identifier, EndPoint RemoteEndPoint, string ClientName)
                {
                    ClientID = Identifier;
                    Name = ClientName;
                    ClientIP = RemoteEndPoint;
                }
                public int ClientID { get; set; }
                public string Name { get; set; }
                public EndPoint ClientIP { get; set; }

            }



            public static void Tcpserver()
            {
                server = new TcpListener(ipe);
                server.Start();
                server.Server.ReceiveTimeout = 10;
                server.Server.SendTimeout = 10;

                while (servrun == true)
                {
                    try
                    {

                        //if server has a pending client connection executes as below
                        if (server.Pending() == true)
                        {

                            //starts a new thread and passes the accepttcpclient into that thread to manage along with the current clientid and a name if you want to assign one here, names could be pulled from XML file by the client below if required.
                            Thread runclient = new Thread(() => Tcpclientmgr(server.AcceptTcpClient(), clientcount, ""));
                            //determine thread as background so we can close it later
                            runclient.IsBackground = true;
                            //duh start the thread.
                            runclient.Start();


                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.ToString());
                    }
                    Thread.Sleep(10);
                }
                server.Stop();
                Thread.CurrentThread.Abort();
            }

            private static void Tcpclientmgr(TcpClient tcpc, int clientid, string clientname)
            {
                //adds 1 to the program clientcount once this has been established so sync is maintained.
                clientcount++;
                int myid = clientid;
                //tcrun is used on a per thread basis to determine if this individual thread should still run
                Boolean tcrun = true;



                int cmdreadcnt = inccmds.Count();
                NewClient thisclient = new NewClient(myid, tcpc.Client.RemoteEndPoint.ToString());

                //add our id and remote endpoint to the clientlist
                clientlist.Add(thisclient);
                //add our id and name to the clientlist - this is the list we will use later for our friendly name

                //overwrites endpoint client identifier with our naming client that can be modified later
                if (clientname != "")
                {
                    thisclient = new NewClient(myid, clientname);
                }

                //adds the client to the list
                clientnames.Add(thisclient);


                byte[] nbuff = Encoding.ASCII.GetBytes("Client Connected:" + myid + vbcrlf + "To Send Shared Server Messages Use Chat:<Message>" + vbcrlf);
                //tcpc.Client.Send(nbuff);
                tcpc.ReceiveTimeout = 10;
                tcpc.SendTimeout = 10;
                //servrun is main boolean to determine if whole program should execute
                //tcrun is the boolean to determine if each individual threaded tcpclient should run or not

                //sends data to tcpclient on each cycle of the while loop to determine if it is still open, if not it closes so as not to hog threads.
                try
                {
                    byte[] tempbuff = new byte[1];
                    //peek must be used so that we dont tie up the main receive buffer
                    if (tcpc.Client.Receive(tempbuff, SocketFlags.Peek) < 1)
                    {
                        tcpc.Close();
                        tcpc.Client.Close();
                        tcrun = false;
                        Thread.CurrentThread.Abort();


                    }

                }
                catch (Exception excnct)
                {
                    //WriteLog("TCP Server Error: " + excnct.ToString());
                }
                try
                {
                    //this.Invoke(new Action(() => this.Text = string.Format("Hutchison Technologies - Fiit Manager {0}", pollcount) + " - " + cmdreadcnt + "-" + inccmds.Count()));
                    //open network stream from tcp client
                    NetworkStream ns = tcpc.GetStream();
                    // must set timeouts so that thread does not hang on read
                    ns.ReadTimeout = 10;
                    ns.WriteTimeout = 10;

                    byte[] buffer = new byte[1024];

                    String unencbuff = null;

                   

                    //makes sure data is available in the network stream before trying to read
                    if (ns.DataAvailable == true)
                    {
                        //makes sure reading is possible before trying to read
                        if (ns.CanRead == true)
                        {
                            // reads buffer value to byte
                            ns.Read(buffer, 0, 1024);

                            //steps through each byte in the byte array to make sure no dead data is held in memory if byte = 0 then it is not commited
                            foreach (byte b in buffer)
                            {
                                if (b != 0)
                                {
                                    unencbuff += Convert.ToChar(b);


                                }

                            }

                            // writecmd(unencbuff);

                            string[] responselines;
                            string[] separators = { "\r\n" };

                            responselines = unencbuff.Split(separators, StringSplitOptions.None);

                            string cururl = responselines[0].Replace("GET ", "").Replace("HTTP/1.1", "");
                            string[] sepqueries = cururl.Split('?');
                            byte[] nbuffinc = { 1, 2, 3 };
                            string response = "";
                            try
                            {
                                
                                switch (sepqueries[0].ToLower().Trim())
                                {
                                    case "/getmyzonestatus":
                                        string status = PingMyzone().ToString();
                                        response = "HTTP/1.1 200 OK\r\nDate: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nServer: Graeme Hind / 1.0 (Win64)\r\nLast - Modified: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nContent - Length: " + status.Length + "\r\nContent-Type: text/plain\r\nConnection: Closed";


                                        StringBuilder sb = new StringBuilder();
                                        sb.AppendLine();
                                        
                                        nbuffinc = Encoding.ASCII.GetBytes(response + "\r\n\r\n" + PingMyzone().ToString());
                                        ns.Write(nbuffinc, 0, nbuffinc.Length);
                                        break;
                                    case "/getsongs":
                                        String[] tracks45 = Directory.GetFiles(Dir45, "*.mp3");
                                        String[] tracks55 = Directory.GetFiles(Dir55, "*.mp3");
                                        StringBuilder sb45 = new StringBuilder();
                                        sb45.Append("{\"Tracks45min\":[");
                                        foreach(string s in tracks45)
                                        {
                                            if (s != tracks45.Last())
                                            {
                                                string s2 = s.Replace("\\", "/");
                                                sb45.Append("{\"name\":\"" + Path.GetFileNameWithoutExtension(s) + "\",\"filePath\":\"" + Uri.EscapeDataString(s2.Replace("C:", "")).ToString() + "\"},");
                                            }
                                            else
                                            {
                                                string s2 = s.Replace("\\", "/");
                                                sb45.Append("{\"name\":\"" + Path.GetFileNameWithoutExtension(s) + "\",\"filePath\":\"" + Uri.EscapeDataString(s2.Replace("C:", "")).ToString() + "\"}],\"Tracks55min\":[");

                                            }
                                        }


                                        foreach (string s in tracks55)
                                        {
                                           
                                            if (s != tracks55.Last())
                                            {
                                                string s2 = s.Replace("\\", "/");
                                                sb45.Append("{\"name\":\"" + Path.GetFileNameWithoutExtension(s) + "\",\"filePath\":\"" + Uri.EscapeDataString(s2.Replace("C:", "")).ToString() + "\"},");
                                            }
                                            else
                                            {
                                                string s2 = s.Replace("\\", "/");
                                                sb45.Append("{\"name\":\"" + Path.GetFileNameWithoutExtension(s) + "\",\"filePath\":\"" + Uri.EscapeDataString(s2.Replace("C:", "")).ToString() + "\"}]}");

                                            }
                                        }
                                        response = "HTTP/1.1 200 OK\r\nDate: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nServer: Graeme Hind / 1.0 (Win64)\r\nLast - Modified: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nContent - Length: " + sb45.Length + "\r\nContent-Type: application/json\r\nConnection: Closed";

                                        nbuffinc = Encoding.ASCII.GetBytes(response + "\r\n\r\n" + sb45.ToString());
                                        ns.Write(nbuffinc, 0, nbuffinc.Length);
                                        break;
                                    case "/get55track":
                                        //MessageBox.Show("55 Tracks");
                                        break;
                                    case "/gettotalclassesinfo":
                                        //MessageBox.Show("Get Total Classes Info");
                                        break;
                                    case "/addlog":
                                        //MessageBox.Show("Add Log");
                                        Uri nuri = new Uri("http://127.0.0.1:8888" + sepqueries[0] + "?" + sepqueries[1]);
                                        //MessageBox.Show(nuri.ToString());
                                        string cont = "Added Log - " + HttpUtility.ParseQueryString(nuri.Query).Get(0);
                                        response = "HTTP/1.1 200 OK\r\nDate: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nServer: Graeme Hind / 1.0 (Win64)\r\nLast - Modified: " + DateTime.Now.ToString("ddd MMM yyy HH:mm:SS") + "\r\nContent - Length: " + cont.Length + "\r\nContent-Type: text/plain\r\nConnection: Closed";
                                        string[] splitquery = cont.Split('-');
                                        File.AppendAllText(logdir + "\\Play-Logs\\ClassLog-" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv", DateTime.Now.ToString("dd/MM/yyyy @ HH:mm:ss") + "," +  splitquery[0] + "," + splitquery[1] + "\r\n");

                                        nbuffinc = Encoding.ASCII.GetBytes(response + "\r\n\r\n" + cont.ToString());
                                        ns.Write(nbuffinc, 0, nbuffinc.Length);
                                        break;



                                }
                            }catch(Exception ex)
                            {
                                //MessageBox.Show(ex.ToString());
                            }

                            
       


                        }

                    }

                  




                }
                catch (Exception mainex)
                {
                    WriteLog("TCP Client Manager:" + mainex.ToString());
                }
                //sleep the thread for 1ms so it does not hog resources
                Thread.Sleep(1);





                //if exited from the while loop close the tcp client and abort the thread
                tcpc.Close();
                Thread.CurrentThread.Abort();
            }

            private static int PingMyzone()
            {
                //  Ping nping = new Ping();
                // PingReply pr = nping.Send(myzoneip, 30);

                TcpClient tcpc = new TcpClient();
                tcpc.Client.SendTimeout = 30;
                tcpc.Client.ReceiveTimeout = 30;
                int retstatus = 0;
                try
                {
                    tcpc.Connect(new IPEndPoint(IPAddress.Parse(myzoneip), 8080));
         
                    if(tcpc.Client.Connected==true)
                    {
                        tcpc.Client.Close();
                        tcpc.Close();
                        retstatus = 1;
                    }

                }catch(Exception ex)
                {
                    retstatus = 0;
                }



                return retstatus;
            }


            private string processrequest(string[] parameter)
            {

                string cmd = parameter[0];

                if (cmd == "End")
                {
                    servrun = false;

                    Application.Exit();


                }
                else if (cmd == "")
                {
                    cmd = "NAK";


                }
                else if (cmd == "EndClient")
                {
                    cmd = "<ENDCLIENT>";
                }
                else if (cmd == "Restart")
                {
                    Application.Restart();
                }


                return cmd;

            }
            #endregion
        }

    }
}
