

using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
            public IPEndPoint endp