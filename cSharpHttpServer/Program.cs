//+-==============================================-+
//|Arthor: OsbourneClark
//|Creation: WebServer
//|Description: webserver that can deliver pages as well as support backend scripting
//|Date: 02/03/2023
//|license: MIT License
//+-==============================================-+
using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using cSharpHttpServer;

namespace cSharpHttpServer {

    //http1.1 server
    class HttpServer {

        //socket doo dads
        IPEndPoint localEndPoint;
        Socket ServerListenSocket;
        List<Thread> OpenSockets = new List<Thread>();

        //backend scripts
        //the connecting ip, the connecting port, data sent(if any)

        //TODO implement this so porper functions can be defined for each request type<-------------OVERHERE---------------------------------------------------------------------------------------------
        public delegate string BacksideFunctionGET(string IP, string PORT); //get resources
        public delegate string BacksideFunctionPOST(string IP, string PORT, string DATA); //create and process
        public delegate bool BacksideFunctionPUT(string IP, string PORT, string DATA); //update resources succeds if 0
        public delegate bool BacksideFunctionDELETE(string IP, string PORT, string DATA); //deletes precess succeds if 0

        Dictionary<string, BacksideFunctionGET> ServerSideFunctionsGET = new Dictionary<string, BacksideFunctionGET>();
        Dictionary<string, BacksideFunctionPOST> ServerSideFunctionsPOST = new Dictionary<string, BacksideFunctionPOST>();
        Dictionary<string, BacksideFunctionPUT> ServerSideFunctionsPUT = new Dictionary<string, BacksideFunctionPUT>();
        Dictionary<string, BacksideFunctionDELETE> ServerSideFunctionsDELETE = new Dictionary<string, BacksideFunctionDELETE>();

        //initial set up
        bool successfulCreation = false;

        //queue stuff
        Queue<SocketEvent> _EventQueue = new Queue<SocketEvent>(); //needs to add so messages will dissplay properly
        private Object queueLock = new Object();

        static ConsoleColor[] backgroundColourSelecting = { ConsoleColor.DarkBlue, ConsoleColor.DarkGreen, ConsoleColor.DarkRed, ConsoleColor.DarkMagenta, ConsoleColor.DarkYellow, ConsoleColor.DarkGray, ConsoleColor.DarkCyan };
        struct SocketEvent
        {
            public ConsoleColor backgroundColour;
            public string message;
            public IPEndPoint endpoint;
            public SocketEvent(ConsoleColor BackgoundColor, string Message, IPEndPoint ipPort)
            {
                this.backgroundColour = BackgoundColor;
                this.message = Message;
                this.endpoint = ipPort;
            }
        }


        public HttpServer(IPAddress localIP, int localPort, Dictionary<string, BacksideFunctionGET> funcGET,
            Dictionary<string, BacksideFunctionPOST> funcPOST,
            Dictionary<string, BacksideFunctionPUT> funcPUT,
            Dictionary<string, BacksideFunctionDELETE> funcDELETE) {

            //creates local end point
            ServerSideFunctionsGET = funcGET;
            ServerSideFunctionsPOST = funcPOST;
            ServerSideFunctionsPUT = funcPUT;
            ServerSideFunctionsDELETE = funcDELETE;
            localEndPoint = new IPEndPoint(localIP, localPort);
            ServerListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            successfulCreation = true;
        }


        public bool Start() {
            if (!successfulCreation) { return false; }

            ServerListenSocket.Bind(localEndPoint);
            ServerListenSocket.Listen(100);
            while (true) {

                Socket acceptedSocket = ServerListenSocket.Accept();

                OpenSockets.Add(new Thread(() => { connectionHandler(acceptedSocket); }));
                OpenSockets[^1].Start();

            }

            return true;
        }

        //handleing individual connections
        bool connectionHandler(Socket handleingSocket) {

            IPEndPoint remoteEnd = handleingSocket.RemoteEndPoint != null ? handleingSocket.RemoteEndPoint as IPEndPoint : new IPEndPoint(IPAddress.Parse("0.0.0.0"), 1111);
            ConsoleColor pickedBackgroundColour = (ConsoleColor)(new Random().Next(1, 100) % 15) + 1;
            if (remoteEnd == null) { return false; }

            HttpHandler httpHandler = new HttpHandler(remoteEnd.Address.ToString(), remoteEnd.Port.ToString(), ServerSideFunctionsGET, ServerSideFunctionsPOST, ServerSideFunctionsPUT, ServerSideFunctionsDELETE);
            byte[] recvBuffer = new byte[2048]; //recived memory 2KB
            //sets recive timeout to 1second
            handleingSocket.ReceiveTimeout = 5000;//timeout for keepalive connections
            bool openConnection = true;

            while (openConnection)
            {
                try//trys to recv
                {
                    handleingSocket.Receive(recvBuffer);
                    QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection", remoteEnd));
                }
                catch (ObjectDisposedException)
                {
                    //if connection closed already
                    QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection already closed", remoteEnd));
                    return false;
                }
                catch (SocketException)
                {
                    //if conection isnt valid
                    handleingSocket.Shutdown(SocketShutdown.Both);
                    handleingSocket.Close();
                    handleingSocket.Dispose();

                    QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection closed", remoteEnd));
                    return false;
                }

                //parses http
                openConnection = httpHandler.parseHTTP(Encoding.UTF8.GetString(recvBuffer));

                //next is response
                try
                {
                    handleingSocket.Send(httpHandler.fullHeaderResponse);
                    handleingSocket.Send(httpHandler.fullDataResponse);
                }
                catch (ObjectDisposedException)
                {
                    //if connection closed already
                    QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection already closed", remoteEnd));
                    return false;
                }
                catch (SocketException)
                {
                    //if conection isnt valid
                    handleingSocket.Shutdown(SocketShutdown.Both);
                    handleingSocket.Close();
                    handleingSocket.Dispose();

                    QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection closed", remoteEnd));
                    return false;
                }
                httpHandler.resetResponse();
            }

            //if the connection ended
            try
            {
                handleingSocket.Shutdown(SocketShutdown.Both);
                handleingSocket.Close();
                handleingSocket.Dispose();
            }
            catch (ObjectDisposedException) {
                //if connection is already cloed
                QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection already closed", remoteEnd));
                return false;
            }
            QueueSocketEvent(new SocketEvent(pickedBackgroundColour, "Connection closed", remoteEnd));

            return false;
        }

        void QueueSocketEvent(SocketEvent newEvent) {
            lock (this.queueLock)
            {
                _EventQueue.Enqueue(newEvent);
            }
        }

        public void printQueueEvent() {
            while (true) {
                if (_EventQueue.Count <= 0) { continue; }
                SocketEvent Event = _EventQueue.Dequeue();
                Console.Write(Event.message);
                Console.BackgroundColor = Event.backgroundColour;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(20, Console.GetCursorPosition().Top);
                Console.Write("{0} : {1}", Event.endpoint.Address, Event.endpoint.Port);
                Console.ResetColor();
                Console.WriteLine("");
            }
        }

    }

    class mainClass{
        public static bool FilePathHasInvalidChars(string path)
        {

            return (!string.IsNullOrEmpty(path) && path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0);
        }
        static string test(string IP, string PORT)
        {
            string returnstr = "<html><body><h1>connected with Ip: " + IP + "</h1><h1>on Port: " + PORT + "</h1></body></html>";
            return returnstr;
        }
        static string echo(string IP, string PORT, string DATA) {
            return "<h1>Hello Im the server. Echoing Back: "+DATA+"</h1>";
        }
        static string echoFile(string IP, string PORT, string DATA)
        {
            string filedata = "not found";
            if (File.Exists(DATA)) {
                filedata = File.ReadAllText(DATA);
            }
            return "<h1>" + filedata + "</h1>";
        }

        public static int Main()
        {
            Console.WriteLine("hello there world");

            Dictionary<string, HttpServer.BacksideFunctionGET> serverFunctionsGET = new Dictionary<string, HttpServer.BacksideFunctionGET>(){
                { "func1", new HttpServer.BacksideFunctionGET(test)}
            };
            Dictionary<string, HttpServer.BacksideFunctionPOST> serverFunctionsPOST = new Dictionary<string, HttpServer.BacksideFunctionPOST>() {
                { "echo", new HttpServer.BacksideFunctionPOST(echo)},
                { "fileFetch", new HttpServer.BacksideFunctionPOST(echoFile)}
            };
            Dictionary<string, HttpServer.BacksideFunctionPUT> serverFunctionsPUT = new Dictionary<string, HttpServer.BacksideFunctionPUT>();
            Dictionary<string, HttpServer.BacksideFunctionDELETE> serverFunctionsDELETE = new Dictionary<string, HttpServer.BacksideFunctionDELETE>();

            IPAddress localIp = IPAddress.Parse("127.0.0.1");
            HttpServer newServer = new HttpServer(localIp, 80, serverFunctionsGET, serverFunctionsPOST, serverFunctionsPUT, serverFunctionsDELETE);

            Thread printingThings = new Thread(() => { newServer.printQueueEvent(); });
            printingThings.Start();

            newServer.Start();

            return 0;
        }
    }


}