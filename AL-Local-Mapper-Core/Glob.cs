using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AL_Local_Mapper_Core
{
    internal static class Glob
    {
        public static Settings Settings { get; private set; } = new Settings();
        public static PathFinder PathFinder { get; private set; } = new PathFinder();
        public static WebServer WebServer { get; private set; } = new WebServer();
        public static Logger Logger { get; private set; } = new Logger();
    }
}
