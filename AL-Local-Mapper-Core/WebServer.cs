using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GenHTTP.Modules.Security;
using GenHTTP.Modules.Webservices;
using GenHTTP.Modules.Layouting;
using GenHTTP.Engine;
using GenHTTP.Api.Infrastructure;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using System.Security.Cryptography.X509Certificates;
using GenHTTP.Api.Protocol;
using GenHTTP.Api.Content;
using GenHTTP.Modules.IO;

namespace AL_Local_Mapper_Core
{
    internal class WebServer
    {
        private IServerHost? _Server = null;
        private X509Certificate2 _Certificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cert", "Certificate.pfx"), "Express26");

        public bool Start()
        {
            var corsPolicy = CorsPolicy.Permissive().Add("https://adventure.land", new GenHTTP.Modules.Security.Cors.OriginPolicy(new List<FlexibleRequestMethod>() { new FlexibleRequestMethod(RequestMethod.GET), new FlexibleRequestMethod(RequestMethod.POST) }, null, null, true, 5000));

            try
            {
                var service = Layout.Create()
                .AddService<PathingAPI>("FindPath");
                //.AddService<CertAPI>(".well-known")
                //.Add(corsPolicy);

                String hostName = Dns.GetHostName();
                var hostEntry = Dns.GetHostEntry(hostName);

                IPAddress myIP = IPAddress.Any;

                if (!String.IsNullOrEmpty(Glob.Settings.BindIP))
                    myIP = IPAddress.Parse(Glob.Settings.BindIP);
                else
                    myIP = hostEntry.AddressList.FirstOrDefault(a => a.ToString().StartsWith("192.168.0.") || a.ToString().StartsWith("192.168.1."));

                Glob.Logger.Debug($"Starting webserver on {myIP}.");
                _Server = Host.Create()
                    .Bind(myIP, (ushort)Glob.Settings.ServerPort, _Certificate)
                    //.Add(corsPolicy)
                    .Add(CorsPolicy.Permissive())
                    //.Port((ushort)Glob.Settings.ServerPort)
                    .Handler(service)
                    .Start();

                return true;
            }
            catch(Exception ex)
            {
                Glob.Logger.Error("Failed to start WebServer", ex);
            }

            return false;
        }

        public void Stop()
        {
            if(_Server != null)
            {
                _Server.Stop();
                _Server = null;
            }
        }

        /*private HttpListener _Listener;

        public bool Start()
        {
            if (!HttpListener.IsSupported)
            {
                Glob.Logger.Error("HttpListener is not supported on this system.");
                return false;
            }

            if (_Listener != null && _Listener.IsListening)
                _Listener.Close();

            _Listener = new HttpListener();

            _Listener.Prefixes.Add($"http://{Glob.Settings.HostName}:{Glob.Settings.ServerPort}/api/FindPath/");

            _Listener.Start();

            _Listener.BeginGetContext(GetContextCallback, null);
            return true;
        }

        public void Stop()
        {
            if (_Listener != null)
                _Listener.Close();

            _Listener = null;
        }

        private void GetContextCallback(IAsyncResult ar)
        {
            try
            {
                HttpListenerContext context = _Listener.EndGetContext(ar);

                HandleContext(context);
            }
            catch { }
        }

        private static void HandleContext(HttpListenerContext context)
        {
            try
            {
                String? reqJson = null;

                using (System.IO.StreamReader sr = new System.IO.StreamReader(context.Request.InputStream, context.Request.ContentEncoding, true, 2048, true))
                {
                    reqJson = sr.ReadToEnd();
                }

                if (String.IsNullOrEmpty(reqJson))
                {
                    SendResponse(new JObject() {
                        { "path", new JArray() },
                        { "time", 0 },
                        { "error", "No data inputted" }
                    }, context);
                    return;
                }

                JObject resObj = null;

                JObject reqObj = JObject.Parse(reqJson);
                if (reqObj == null)
                {
                    SendResponse(new JObject() {
                        { "path", new JArray() },
                        { "time", 0 },
                        { "error", "No data inputted" }
                    }, context);
                    return;
                }

                resObj = Glob.PathFinder.FindPath(reqObj);
                if (resObj == null)
                {
                    SendResponse(new JObject() {
                        { "path", new JArray() },
                        { "time", 0 },
                        { "error", "Failed to get path from service" }
                    }, context);
                    return;
                }

                Encoding outputEncoding = context.Response.ContentEncoding ?? context.Request.ContentEncoding;

                byte[] respBuffer = outputEncoding.GetBytes(resObj.ToString(Newtonsoft.Json.Formatting.None));

                context.Response.ContentLength64 = respBuffer.LongLength;
                context.Response.OutputStream.Write(respBuffer, 0, respBuffer.Length);

                context.Response.Close();
            }
            catch { }
        }

        private static void SendResponse(JObject response, HttpListenerContext context)
        {
            Encoding outputEncoding = context.Response.ContentEncoding ?? context.Request.ContentEncoding;

            byte[] respBuffer = outputEncoding.GetBytes(response.ToString(Newtonsoft.Json.Formatting.None));

            context.Response.ContentLength64 = respBuffer.LongLength;
            context.Response.OutputStream.Write(respBuffer, 0, respBuffer.Length);

            context.Response.Close();
        }*/

        private class CertAPI
        {
            [ResourceMethod(GenHTTP.Api.Protocol.RequestMethod.GET)]
            public String Get()
            {
                return @"40BF20C40377F8BDDB38CE7CD5141FE70EFD604317F142E3709A62567936033F
comodoca.com
2da17ef0896ada6";
            }
        }

        internal class PathingAPI
        {
            internal static PathingAPI LatestInstance { get; private set; }

            public Dictionary<String, DateTime> AccessList { get; private set; } = new Dictionary<String, DateTime>();

            public PathingAPI()
            {
                if (File.Exists("access_list.txt"))
                {
                    try
                    {
                        var dict = JsonConvert.DeserializeObject<Dictionary<String, DateTime>>(File.ReadAllText("access_list.txt"));
                        if (dict != null)
                            AccessList = dict;
                    }
                    catch { }
                }

                LatestInstance = this;
            }

            [ResourceMethod(RequestMethod.GET)]
            public IResponseBuilder GetResponse(IRequest request, IHandler handler)
            {
                return request.Respond().Header("Access-Control-Allow-Origin", "*").Content(new JObject() { { "path", new JArray() }, { "error", "Only POST requests allowed." } }.ToString(Formatting.None)).Type(new FlexibleContentType(GenHTTP.Api.Protocol.ContentType.ApplicationJson));
            }

            [ResourceMethod(RequestMethod.POST, IgnoreContent = false)]
            public IResponseBuilder PostResponse(IRequest request, IHandler handler)
            {
                return _GetPath(request, handler);
            }

            [ResourceMethod(RequestMethod.PUT, IgnoreContent = false)]
            public IResponseBuilder PutResponse(IRequest request, IHandler handler)
            {
                return _GetPath(request, handler);
            }

            private IResponseBuilder _GetPath(IRequest request, IHandler handler)
            {
                String json = null;
                try
                {
                    lock (AccessList)
                    {
                        AccessList[request.Client.IPAddress.ToString()] = DateTime.Now;
                        File.WriteAllText("access_list.txt", JsonConvert.SerializeObject(AccessList, Formatting.Indented));
                    }

                    if (request.Content == null)
                    {
                        return request.Respond().Header("Access-Control-Allow-Origin", "*").Content(new JObject() { { "path", new JArray() }, { "error", "No request sent" } }.ToString(Formatting.None)).Type(new FlexibleContentType(GenHTTP.Api.Protocol.ContentType.ApplicationJson));
                    }

                
                    using (StreamReader sr = new StreamReader(request.Content))
                    {
                        json = sr.ReadToEnd();
                        JObject reqObj = JObject.Parse(json);

                        JObject resObj = Glob.PathFinder.FindPath(reqObj);

                        if (resObj == null)
                            return request.Respond().Header("Access-Control-Allow-Origin", "*").Content(new JObject() { { "path", new JArray() }, { "error", "Failed to get path from service" } }.ToString(Formatting.None)).Type(new FlexibleContentType(GenHTTP.Api.Protocol.ContentType.ApplicationJson));

                        return request.Respond().Header("Access-Control-Allow-Origin", "*").Content(resObj.ToString(Formatting.None)).Type(new FlexibleContentType(GenHTTP.Api.Protocol.ContentType.ApplicationJson));
                    }
                }
                catch(Exception ex)
                {
                    return request.Respond().Header("Access-Control-Allow-Origin", "*").Content(new JObject() { { "path", new JArray() }, { "error", $"{ex.Message}\n{ex.StackTrace}\nJSON:\n{json}" } }.ToString(Formatting.None)).Type(new FlexibleContentType(GenHTTP.Api.Protocol.ContentType.ApplicationJson));
                }
            }

            public class ApiResponse
            {
                [JsonProperty("path")]
                [System.Text.Json.Serialization.JsonPropertyName("path")]
                public JArray Path { get; set; } = new JArray();

                [JsonProperty("time")]
                [System.Text.Json.Serialization.JsonPropertyName("time")]
                public int Time { get; set; } = 0;

                [JsonProperty("error")]
                [System.Text.Json.Serialization.JsonPropertyName("error")]
                public String? Error { get; set; } = null;

                [JsonProperty("cached")]
                [System.Text.Json.Serialization.JsonPropertyName("cached")]
                public bool Cached { get; set; } = false;
            }

            public class ApiRequest
            {
                [JsonProperty("fromMap")]
                [System.Text.Json.Serialization.JsonPropertyName("fromMap")]
                public String FromMap { get; set; }

                [JsonProperty("fromX")]
                [System.Text.Json.Serialization.JsonPropertyName("fromX")]
                public double FromX { get; set; }

                [JsonProperty("fromY")]
                [System.Text.Json.Serialization.JsonPropertyName("fromY")]
                public double FromY { get; set; }

                [JsonProperty("toMap")]
                [System.Text.Json.Serialization.JsonPropertyName("toMap")]
                public String ToMap { get; set; }

                [JsonProperty("toX")]
                [System.Text.Json.Serialization.JsonPropertyName("toX")]
                public double ToX { get; set; }

                [JsonProperty("toY")]
                [System.Text.Json.Serialization.JsonPropertyName("toY")]
                public double ToY { get; set; }

                [JsonProperty("apiKey")]
                [System.Text.Json.Serialization.JsonPropertyName("apiKey")]
                public String ApiKey { get; set; }
            }
        }

    }
}
