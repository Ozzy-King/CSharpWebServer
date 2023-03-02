//+-==============================================-+
//|Arthor: OsbourneClark
//|Creation: WebServer
//|Description: webserver that can deliver pages as well as support backend scripting
//|Date: 02/03/2023
//|license: MIT License
//+-==============================================-+


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Xml;

namespace cSharpHttpServer
{


    class HttpHandler
    {
        struct RequestLayout
        {
            public string requestType = "";
            public string requestDirect = "";
            public string RequestDirectExtension = "";
            public string RequestFileName = "";
            public string requestHttpVersion = "";
            public RequestLayout(string requestType, string requestDirect, string requestHttpVersion)
            {
                this.requestType = requestType;
                this.requestDirect = requestDirect;
                this.requestHttpVersion = requestHttpVersion;
                if (requestDirect.Split(".", 2).Length > 1)
                {
                    string[] parts = requestDirect.Split(".", 2);
                    this.RequestDirectExtension = parts[1].ToLower();
                    parts = parts[0].Split("/");
                    RequestFileName = parts[parts.Length-1];
                }
            }

        }
        struct HeaderLayout
        {
            public string headerType;
            public string headerValue;
            public HeaderLayout(string headerType, string headerValue)
            {
                this.headerType = headerType;
                this.headerValue = headerValue;
            }
        }

        public Dictionary<string, HttpServer.BacksideFunction> serverSideFunctions;
        string ip = "", port = "";
        public HttpHandler(string IP, string PORT, Dictionary<string, HttpServer.BacksideFunction> functions) { 
            serverSideFunctions = functions;
            ip = IP;
            port = PORT;
        }


        public byte[] fullHeaderResponse = new byte[0];
        string stringHeaderResponse = "";

        public byte[] fullDataResponse = new byte[0];
        string stringDataResponse = "";

        string httpTrailer = "\r\n";
        readonly string httpVersion = "HTTP/1.1";

        readonly Dictionary<string, string> responceCode = new Dictionary<string, string>() {
            {"OK","200 OK" },
            {"NOT FOUND", "404 NOT FOUND"}
        };
        readonly Dictionary<string, string> contentTypes = new Dictionary<string, string>() {
            { "func", "text/html; charset=utf-8"}, //server side function
            { "html", "text/html; charset=utf-8" },
            { "js", "text/javascript; charset=utf-8" },
            { "css", "text/css; charset=utf-8" },
            { "png", "image/png"},
            { "jpg", "image/jpg"},
            { "jpeg", "image/jpeg"}
        };
        readonly string[] imageExtensions = new string[] { "png", "jpeg", "jpg" };

        //return of false stops the recv loop (if "Connection: close")
        public bool parseHTTP(string request)
        {
            bool returningBool = true;
            //split data[1](if there), and headers[0]
            string[] requestSplit = request.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries);//splits into data and headers
            if (requestSplit.Length <= 0) { return false; } //chck there are headers and data


            //get allheaders seperate
            string[] headers = requestSplit[0].Split("\r\n", StringSplitOptions.RemoveEmptyEntries);//sperate header lines ,the header section
            if (headers.Length <= 0) { return false; } //check there are headers

            //gets the request types
            RequestLayout requestHeader = getRequestLayout(headers[0]);
            //if it ends in just "/" it the current directory index
            if (requestHeader.requestDirect[^1] == '/')
            {
                requestHeader.requestDirect += "index.html";
                requestHeader.RequestDirectExtension = "html";
            }

            //gets all the headers and lay them out
            HeaderLayout[] layedOutHeaders = GetHeaderLayout(headers);


            //get data
            string data = requestSplit.Length > 1 ? requestSplit[1] : "No Data"; //gets the data attached

            bool isImage = false;
            bool failedToFind = false;



            //try and simplify this ;)
            switch (requestHeader.requestType)
            {
                case "GET":
                    if (contentTypes.ContainsKey(requestHeader.RequestDirectExtension))
                    {
                        if (requestHeader.RequestDirectExtension == "func")//if it serverside function
                        {
                            if (serverSideFunctions.ContainsKey(requestHeader.RequestFileName))
                            {
                                stringHeaderResponse += httpVersion + " " + responceCode["OK"] + httpTrailer;
                                stringHeaderResponse += "Server: OzzysServer" + httpTrailer;
                                stringHeaderResponse += "Content-Type: " + contentTypes["func"] + httpTrailer;
                                //stringHeaderResponse += httpTrailer;

                                HttpServer.BacksideFunction calledFunc = serverSideFunctions[requestHeader.RequestFileName];
                                string outData = calledFunc(ip, port, requestSplit[0]);

                                stringDataResponse = outData;
                                stringHeaderResponse += "Content-Length: " + stringDataResponse.Length + httpTrailer;

                            }
                            else { failedToFind = true; }
                        }
                        else//if its anything else
                        {
                            if (File.Exists("./" + requestHeader.requestDirect))
                            {
                                stringHeaderResponse += httpVersion + " " + responceCode["OK"] + httpTrailer;
                                stringHeaderResponse += "Server: OzzysServer" + httpTrailer;
                                stringHeaderResponse += "Content-Type: " + contentTypes[requestHeader.RequestDirectExtension] + httpTrailer;

                                //if image change to allow for jpeg and jpg 
                                if (IsFileAnImage(requestHeader.RequestDirectExtension))
                                {
                                    isImage = true;
                                    fullDataResponse = File.ReadAllBytes("./" + requestHeader.requestDirect);
                                    stringHeaderResponse += "Content-Length: " + fullDataResponse.Length + httpTrailer;
                                }
                                else//else if text
                                {
                                    stringDataResponse = File.ReadAllText("./" + requestHeader.requestDirect, Encoding.UTF8);
                                    stringHeaderResponse += "Content-Length: " + stringDataResponse.Length + httpTrailer;
                                }

                                //stringHeaderResponse += httpTrailer;
                            }
                            else
                            {
                                failedToFind = true;
                            }
                        }
                    }
                    else
                    {
                        failedToFind = true;
                    }
                    break;
                case "POST":
                    if (requestHeader.RequestDirectExtension == "func")//if it serverside function
                    {
                        if (serverSideFunctions.ContainsKey(requestHeader.RequestFileName))
                        {
                            stringHeaderResponse += httpVersion + " " + responceCode["OK"] + httpTrailer;
                            stringHeaderResponse += "Server: OzzysServer" + httpTrailer;
                            stringHeaderResponse += "Content-Type: " + contentTypes["func"] + httpTrailer;
                            //stringHeaderResponse += httpTrailer;

                            HttpServer.BacksideFunction calledFunc = serverSideFunctions[requestHeader.RequestFileName];
                            string outData = calledFunc(ip, port, requestSplit[0]);

                            stringDataResponse = outData;
                            stringHeaderResponse += "Content-Length: " + stringDataResponse.Length + httpTrailer;

                        }
                        else { failedToFind = true; }
                    }
                    break;

            }

            //if resources canot be found populate header and data with either 404 in current direct or a default 404
            if (failedToFind)
            {
                //get the directory path
                string directoryPath = "";
                for (int i = 0; i < requestHeader.requestDirect.Split("/").Length - 1; i++)
                {
                    directoryPath += requestHeader.requestDirect.Split("/")[i] + (i == requestHeader.requestDirect.Split("/").Length - 2 ? "" : "/");
                }

                if (File.Exists("./" + directoryPath + "/404.html"))
                {
                    stringDataResponse = File.ReadAllText("./" + directoryPath + "/404.html", Encoding.UTF8);
                }
                else { stringDataResponse = "<html><body><h1>404 ERROR</h1><p>File Not Found</p></body></html>"; }

                //stringDataResponse = File.ReadAllText("./404.html", Encoding.UTF8);
                stringHeaderResponse += httpVersion + " " + responceCode["NOT FOUND"] + httpTrailer;
                stringHeaderResponse += "Server: OzzysServer" + httpTrailer;
                stringHeaderResponse += "Content-Type: " + contentTypes["html"] + httpTrailer;
                stringHeaderResponse += "Content-Length: " + stringDataResponse.Length + httpTrailer;
                //stringHeaderResponse += httpTrailer;
            }


            //set the return connection thype
            //if the connection isnt persistent
            if (GetHeaderValueFromArray(ref layedOutHeaders, "connection") == "close" || GetHeaderValueFromArray(ref layedOutHeaders, "connection") == "NULL")
            {
                stringHeaderResponse += "Connection: close" + httpTrailer;
                returningBool = false;
            }
            else
            {
                stringHeaderResponse += "Connection: keep-alive" + httpTrailer;
                stringHeaderResponse += "Keep-Alive: timeout=5, max=1000" + httpTrailer;
            }
            stringHeaderResponse += httpTrailer;
            //Console.WriteLine(requestHeader.requestDirect + requestHeader.RequestDirectExtension);
            //Console.WriteLine(stringHeaderResponse);

            if (!isImage) //if i isnt an image encode the response to bytes, image is already bytes
            {
                fullDataResponse = Encoding.ASCII.GetBytes(stringDataResponse);
            }
            //encode the actual header part to be sent
            fullHeaderResponse = Encoding.ASCII.GetBytes(stringHeaderResponse);

            return returningBool;
        }

        bool IsFileAnImage(string ext)
        {
            for (int i = 0; i < imageExtensions.Length; i++)
            {
                if (imageExtensions[i] == ext) { return true; }
            }
            return false;
        }

        RequestLayout getRequestLayout(string requestString)
        {
            string[] headerVal = requestString.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (headerVal.Length < 3) { return new RequestLayout("NULL", "NULL", "NULL"); }//checks if all is there
            return new RequestLayout(headerVal[0], headerVal[1], headerVal[2]);
        }

        HeaderLayout[] GetHeaderLayout(string[] headerStrings)
        {
            HeaderLayout[] layedOutHeaders = new HeaderLayout[headerStrings.Length - 1];
            for (int i = 1; i < headerStrings.Length; i++)
            {
                string[] headerVal = headerStrings[i].Split(":", 2);
                layedOutHeaders[i - 1] = new HeaderLayout(headerVal[0].Trim().ToLower(), headerVal[1].Trim().ToLower());
            }
            return layedOutHeaders;
        }

        string GetHeaderValueFromArray(ref HeaderLayout[] headers, string headerName)
        {
            for (int i = 0; i < headers.Length; ++i)
            {
                if (headers[i].headerType == headerName)
                {
                    return headers[i].headerValue;
                }
            }
            return "NULL";
        }

        public void resetResponse()
        {
            fullHeaderResponse = new byte[0];
            stringHeaderResponse = "";

            fullDataResponse = new byte[0];
            stringDataResponse = "";
        }

    }
}