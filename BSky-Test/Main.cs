using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static BSkyLive.BSkyLive;

namespace BSky_Test {
    class BSky_Test {
        const string Version = "0.1-beta";
        public static int Main(string[] args) {
            Console.WriteLine("LumKitty's BlueSky GoLive tool v" + Version);
            Console.WriteLine();
            string ConfigFile = System.AppDomain.CurrentDomain.BaseDirectory + "BSkyLive.json";
            string Title;
            JObject Config;
            if (File.Exists(ConfigFile)) {
                Config = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(ConfigFile));
            } else {
                File.WriteAllText(ConfigFile, @"{
    ""Username"": ""yourname.bsky.social"",
    ""Password"": ""USE AN APP PASSWORD NOT YOUR MAIN!"",
    ""URL"": ""https://twitch.tv/LumKitty"",
    ""Title"": ""This will pop up when someone hovers over your live profile pic. Default is e.g. 'LumKitty - Twitch'. You can change this on the command line if you like"",
    ""Description"": ""I don't think this is actually used but put your channel desc here I guess""
}");
                Console.WriteLine("No config file detected");
                Console.WriteLine("Writing default config to: " + ConfigFile);
                Console.WriteLine("Please edit it and then run this app again");
                return 1;
            }

            if (args.Length > 0) {
                Title = String.Join(" ", args);
                Console.WriteLine("Overriding title to: "+Title);
            } else {
                Title = Config["Title"].ToString();
            }

            Console.WriteLine("Connecting to BlueSky...");
            ConnectBSky(Config["Username"].ToString(), Config["Password"].ToString());

            Console.WriteLine("Going live");
            GoLive(Config["URL"].ToString(), Title, Config["Description"].ToString());

            Console.WriteLine();
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Press enter to end live");
            Console.ReadLine();
            Console.WriteLine("Ending go live");
            EndGoLive();
            Console.WriteLine("Done. Goodbye");
            return 0;
        }
    }
}