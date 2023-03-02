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



the function template looks like this:

```csharp
public delegate string BacksideFunction(string IP, string PORT, string DATA);
```

The connecting ip, port and data(if it was sent) will be passed in and then porcecssed by you function. To pass the actual functions into the web server to be used, you must declare the function static along with the same perameters and return type (string) and create a dictionary type that will be given as an agument to the httpserver constructor.

an example on how that might look:

```csharp
        static string test(string IP, string PORT, string DATA)
        {
            string returnstr = "<html><body><h1>connected with Ip: " + IP + "</h1><h1>on Port: " + PORT + "</h1></body></html>";
            return returnstr;
        }

        public static int Main()
        {
            Console.WriteLine("hello there world");


            Dictionary<string, HttpServer.BacksideFunction> serverFunctions = new Dictionary<string, HttpServer.BacksideFunction>(){
                { "func1", new HttpServer.BacksideFunction(test)}
            };

            IPAddress localIp = IPAddress.Parse("192.168.1.70");
            HttpServer newServer = new HttpServer(localIp, 80, serverFunctions);

            Thread printingThings = new Thread(() => { newServer.printQueueEvent(); });
            printingThings.Start();

            newServer.Start();

            return 0;
        }
```

To then access the function from the webpage youll use the root directory along with the \<functionName\>.func. The exstention must be .func as that is how the server can tell its calling a function.

The functions can be used for both GET and POST requests, this allows for server side proccesing of data.

## Supported files for transport

- html
- css
- javascript
- png
- jpg
- jpeg (not tested but should be implimented)



### Need to add

- server side functions (will use delegates)
  - pass both body data and just url data to delegates
  - extension for server sind function - .func