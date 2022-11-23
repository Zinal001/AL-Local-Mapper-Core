namespace AL_Local_Mapper_Core
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            if(!Glob.Settings.Load())
            {
                Glob.Logger.Error("Failed to load settings, application will now exit.");
                return 1;
            }

            Glob.Logger.Info("Initializing PathFinder, please wait...");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            await Task.Run(() => {
                Glob.PathFinder.Initialize();
            });
            sw.Stop();
            Glob.Logger.Debug($"Pathfinding service initialized in {sw.Elapsed.TotalSeconds} seconds.");

            WebServer.PathingAPI.ApiRequest req = new WebServer.PathingAPI.ApiRequest() {
                FromMap = "goobrawl",
                ToX = -398,
                ToY =  -1261,
                ToMap = "desertland",
                FromX = 66,
                FromY = -120
            };

            Newtonsoft.Json.Linq.JObject resObj = Glob.PathFinder.FindPath(Newtonsoft.Json.Linq.JObject.FromObject(req));

            if(!Glob.WebServer.Start())
            {
                Glob.Logger.Error("Failed to initialize Web Server, application will now exit.");
                return 2;
            }

            Glob.Logger.Info($"Pathfinding service has been initialized on http://{Glob.Settings.HostName}:{Glob.Settings.ServerPort}/FindPath/");

            while(true)
            {
                String? cmd = Console.ReadLine();
                if (!String.IsNullOrEmpty(cmd))
                {
                    if((cmd.ToLower() == "exit" || cmd.ToLower() == "quit"))
                        break;
                    else if(cmd.ToLower().StartsWith("testpath"))
                    {
                        String[] props = new string[] { "cmd", "fromMap", "fromX", "fromY", "toMap", "toX", "toY" };
                        String[] parts = cmd.Split(' ');

                        var d = new Newtonsoft.Json.Linq.JObject() {
                            { "fromMap", "goobrawl" },
                            { "fromX", 66 },
                            { "fromY", -120 },
                            { "toMap", "desertland" },
                            { "toX", -398 },
                            { "toY", -1261 },
                            { "runspd", 50 },
                            { "nocache", true },
                            { "consoleLog", true }
                        };

                        if(parts.Length > 1)
                        {
                            for(int i = 1; i < parts.Length; i++)
                            {
                                if (i >= props.Length)
                                    break;

                                d[props[i]] = parts[i];
                            }
                        }

                        Newtonsoft.Json.Linq.JObject fullPath = Glob.PathFinder.FindPath(d);
                        Console.WriteLine("Result: " + fullPath.Value<Newtonsoft.Json.Linq.JArray>("path").ToString());
                    }
                    else if(cmd.ToLower() == "access_list")
                    {
                        if (WebServer.PathingAPI.LatestInstance?.AccessList != null)
                        {
                            foreach (KeyValuePair<String, DateTime> pair in WebServer.PathingAPI.LatestInstance.AccessList.OrderBy(p => p.Value))
                                Console.WriteLine($"{pair.Key}: {pair.Value:yyyy-MM-dd HH:mm:ss}.");
                        }
                        else
                            Console.WriteLine("No access list found.");
                        
                    }
                }
            }

            Glob.Logger.Info("Pathfinding service is closing.");

            return 0;
        }
    }
}