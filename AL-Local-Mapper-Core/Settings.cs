using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AL_Local_Mapper_Core
{
    public class Settings
    {
        public String HostName { get; set; } = "almapper.zinals.tech";
        public String? BindIP { get; set; } = null;
        public int ServerPort { get; set; } = 42805;
        public Classes.LogLevel LogLevel { get; set; } = Classes.LogLevel.Info;
        public String LogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Zinals.tech", "AL-Mapper", "Logs");
        public bool StartWithWindows { get; set; } = true;
        public bool OptimizePath { get; set; } = true;
        public bool CheckTown { get; set; } = true;
        public bool CachePaths { get; set; } = true;
        public int CacheTime { get; set; } = 5;
        public List<String> ExcludedMaps { get; set; } = new List<String>() { "bank_b", "bank_u" };
        public Dictionary<String, List<Classes.LocalPath>> LocalPaths { get; set; } = new Dictionary<String, List<Classes.LocalPath>>();

        public bool Load()
        {
            if (!ReadBaseSettings())
                return false;

            ReadExcludedMaps();

            ReadLocalPaths();

            return true;
        }

        private bool ReadBaseSettings()
        {
            try
            {
                String settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Settings.json");
                if (File.Exists(settingsFile))
                {
                    JObject settingsObj = JObject.Parse(File.ReadAllText(settingsFile));

                    if (settingsObj != null)
                    {
                        if (settingsObj.TryGetValue("LogLevel", out JToken logLevelToken))
                        {
                            if (logLevelToken.Type == JTokenType.String)
                                LogLevel = (Classes.LogLevel)Enum.Parse(typeof(Classes.LogLevel), logLevelToken.Value<String>());
                            else if (logLevelToken.Type == JTokenType.Integer)
                                LogLevel = (Classes.LogLevel)logLevelToken.Value<int>();
                        }

                        if (settingsObj.ContainsKey("HostName"))
                        {
                            try
                            {
                                HostName = settingsObj.Value<String>("HostName");
                            }
                            catch { }
                        }

                        if (settingsObj.ContainsKey("ServerPort"))
                        {
                            try
                            {
                                ServerPort = settingsObj.Value<int>("ServerPort");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting ServerPort", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("LogPath"))
                        {
                            String logPath = Environment.ExpandEnvironmentVariables(settingsObj.Value<String>("LogPath"));
                            try
                            {
                                Directory.CreateDirectory(logPath);
                                LogPath = logPath;
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting LogPath", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("StartWithWindows"))
                        {
                            try
                            {
                                StartWithWindows = settingsObj.Value<bool>("StartWithWindows");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting StartWithWindows", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("OptimizePath"))
                        {
                            try
                            {
                                OptimizePath = settingsObj.Value<bool>("OptimizePath");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting OptimizePath", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("CheckTown"))
                        {
                            try
                            {
                                CheckTown = settingsObj.Value<bool>("CheckTown");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting CheckTown", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("CachePaths"))
                        {
                            try
                            {
                                CachePaths = settingsObj.Value<bool>("CachePaths");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting CachePaths", ex);
                            }
                        }

                        if (settingsObj.ContainsKey("CacheTime"))
                        {
                            try
                            {
                                CacheTime = settingsObj.Value<int>("CacheTime");
                            }
                            catch (Exception ex)
                            {
                                Glob.Logger.Error("Failed to load setting CacheTime", ex);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Glob.Logger.Error("Failed to load settings.json", ex);
            }

            return false;
        }

        private bool ReadExcludedMaps()
        {
            try
            {
                String settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ExcludedMaps.json");
                if (File.Exists(settingsFile))
                {
                    String[] excludedMaps = JsonConvert.DeserializeObject<String[]>(File.ReadAllText(settingsFile));
                    if (excludedMaps != null)
                        ExcludedMaps = new List<string>(excludedMaps);
                }

                return true;
            }
            catch (Exception ex)
            {
                Glob.Logger.Warning("Failed to load ExcludedMaps.json", ex);
            }

            return false;
        }

        private bool ReadLocalPaths()
        {
            try
            {
                String settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "LocalPaths.json");
                if (File.Exists(settingsFile))
                {
                    JArray paths = JsonConvert.DeserializeObject<JArray>(File.ReadAllText(settingsFile));
                    if (paths != null)
                    {
                        int i = 0;
                        int j = paths.Count;
                        foreach (JObject pathObj in paths)
                        {
                            i++;
                            try
                            {
                                Classes.LocalPath path = Classes.LocalPath.ReadFromJObject(pathObj);
                                if (path != null)
                                {
                                    if (!LocalPaths.ContainsKey(path.MapName))
                                        LocalPaths[path.MapName] = new List<Classes.LocalPath>();

                                    LocalPaths[path.MapName].Add(path);
                                }
                            }
                            catch (Exception loadEx)
                            {
                                Glob.Logger.Warning($"Failed to load Local path {i}", loadEx);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Glob.Logger.Warning("Failed to load LocalPaths.json", ex);
            }

            return false;
        }

    }
}
