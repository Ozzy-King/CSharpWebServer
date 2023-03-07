# My WebServer

## example to navigate webserer

- url = \<domainName\>/index.html
  - path = \<exeCurrentDirect\>/index.html
  - if url ends in just "/" implicetly means index.html
  - if no exstention is added it is seen as a file and will return either a custom 404 error in the current folder, if there isnt one it will return a default 404 error

------

### example of how web page is layedout

|- "/" (root directory)

|    |- 404.html (html file)

|    |- index.html (html file)

|- "/folder1/" (sub directory)

    |-404.html (html file)

calling to \<domainName\>/

    |-called the \<exeCurrentDirect\>/index.html

calling to \<domainName\>/hiphop

    |-sends \<exeCurrentDirect\>/404.html

calling to \<domainName\>/folder1/

    |-sends \<exeCurrentDirect\>/folder1/404.html

---

## Server side functions

when createing the server in code you will be asked to input function list, these are the server side functions.

there are 4 different function template that looks like this:

```csharp
public delegate string BacksideFunctionGET(string IP, string PORT);
public delegate string BacksideFunctionPOST(string IP, string PORT, string DATA);
public delegate bool BacksideFunctionPUT(string IP, string PORT, string DATA);
public delegate bool BacksideFunctionDELETE(string IP, string PORT, string DATA);
```

The connecting ip, port and data(if it was sent or used) will be passed in and then processed by your function. To pass the actual functions into the web server to be used, you must declare the function static along with the same perameters and return type (string or bool) and create a dictionary type that will be given as an agument to the httpserver constructor.

an example on how that might look:

```csharp
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
```

To then access the function from the webpage youll use the root directory along with the \<functionName\>.func. The exstention must be .func as that is how the server can tell its calling a function.

The functions can be used for all GET, POST, PUT and DELETE requests, this allows for server side proccesing of data. HEAD is also supported but is essintially a GET without a body. 

It is recommended to use the:

```csharp
Thread printingThings = new Thread(() => { newServer.printQueueEvent(); });
```

this is because there is no way to turn off the event queueing system at the moment and leaving them unaffected will cause a continous in memory.

## Supported files for transport

- html
- css
- javascript
- png
- jpg
- jpeg (not tested but should be implimented)

### Need to add

- server side functions (will use delegates)
  - pass data through url data to delegates