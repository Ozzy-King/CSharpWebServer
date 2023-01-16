

using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using cSharpHttpServer;

namespace cSharpHttpServer{

    //http1.1 server
    class HttpServer {

        //socket doo dads
        IPEndPoint localEndPoint;
        Socket ServerListenSocket;
        List<Thread> OpenSockets = new List<Thread>();

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
            public SocketEvent( ConsoleColor BackgoundColor, string Message, IPEndPoint ipPort)
            {
                this.backgroundColour = BackgoundColor;
                this.message = Message;
                this.endpoint = ipPort;
            }
        }


        public HttpServer(IPAddress localIP, int localPort) { 

            //creates local end point
            localEndPoint = new IPEndPoint(localIP, localPort);
            ServerListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            successfulCreation = true;
        }


        public bool Start() {
            if (!successfulCreation) { return false;}

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

            IPEndPoint remoteEnd = handleingSocket.RemoteEndPoint != null ? handleingSocket.RemoteEndPoint as IPEndPoint : new IPEndPoint(IPAddress.Parse("0.0.0.0"), 1111) ;
            ConsoleColor pickedBackgroundColour = (ConsoleColor)(new Random().Next(1, 100)%15) + 1;
            if (remoteEnd == null) { return false; }

            HttpHandler httpHandler = new HttpHandler();
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
                QueueSocketEvent(new SocketEvent( pickedBackgroundColour, "Connection already closed", remoteEnd));
                return false;
            }
            QueueSocketEvent(new SocketEvent( pickedBackgroundColour, "Connection closed", remoteEnd));

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

        public static int Main() {
            Console.WriteLine("hello there world");

            IPAddress localIp = IPAddress.Parse("192.168.35.6");

            HttpServer newServer = new HttpServer(localIp, 80);

            Thread printingThings = new Thread(() => { newServer.printQueueEvent(); });
            printingThings.Start();

            newServer.Start();

            return 0;
        }
    
    }


}