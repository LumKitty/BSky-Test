// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace BSky_Test {
    static class BSKy_Test {
        private static HttpClient client = new HttpClient();

        private const string BSkyLoginHost = "https://inkcap.us-east.host.bsky.network";
        private const string BSkyUsername = "lumkitty-test.bsky.social";
        private const string BSkyPassword = "<USE AN APP PASSWORD>";
        private const string EmbedTitle = "LumKitty-Test";
        private const string EmbedDesc = "Test test meow";
        private const string EmbedURL = "https://twitch.tv/LumKitty";

        private static string? BSkyHost = null;
        private static string? AccessToken = null;
        private static string? RefreshToken = null;
        private static string? DID = null;

        private static System.Timers.Timer? SessionRefreshTimer = null;
        private static System.Timers.Timer? LiveRefreshTimer = null;


        static void Log(string message) {
            if (AccessToken  != null) { message = message.Replace(AccessToken,  "***ACCESS TOKEN***"); }
            if (RefreshToken != null) { message = message.Replace(RefreshToken, "***REFRESH TOKEN***"); }
            if (BSkyPassword != null) { message = message.Replace(BSkyPassword, "***PASSWORD***"); }
            Console.WriteLine(message);
        }
        static void ErrorHandler(Exception e) {
            Log(e.ToString());
        }

        static JObject? HttpRequest(string Method, string URL, JObject JsonContent, string? Bearer = null) {
            return HttpRequest(Method, URL, JsonContent.ToString(), Bearer);
        }

        static JObject? HttpRequest(string Method, string URL, string Content, string? Bearer = null) {
            try {
                
                var jsonData = new StringContent(Content, Encoding.ASCII);
                jsonData.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                if (Bearer != null) {
                    Log("Adding auth header: " + Bearer);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Bearer);
                }
                string Response = "";
                int httpStatus = 0;

                Log(Method + ": " + URL);
                Log("Headers:" + jsonData.Headers.ToString());
                Log("Content:" + Content);
                
                switch (Method) {
                    case "POST":
                        //Log("POST: " + URL + " : " + Content);
                        var PostResult = client.PostAsync(URL, jsonData).GetAwaiter().GetResult();
                        Response = PostResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PostResult.StatusCode);
                        Console.WriteLine(PostResult.ToString());
                        PostResult.Dispose();
                        break;
                    case "PUT":
                        //Log("PUT: " + URL + " : " + Content);
                        var PutResult = client.PutAsync(URL, jsonData).GetAwaiter().GetResult();
                        Response = PutResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PutResult.StatusCode);
                        PutResult.Dispose();
                        break;
                    case "GET":
                        //Log("GET: " + URL + " : " + Content);
                        var GetResult = client.GetAsync(URL).GetAwaiter().GetResult();
                        Response = GetResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)GetResult.StatusCode);
                        GetResult.Dispose();
                        break;
                    case "PATCH":
                        //Log("PATCH: " + URL + " : " + Content);
                        var request = new HttpRequestMessage(new HttpMethod("PATCH"), URL);
                        request.Content = jsonData;
                        var PatchResult = client.SendAsync(request).GetAwaiter().GetResult();
                        Response = PatchResult.Content.ReadAsStringAsync().Result;
                        httpStatus = ((int)PatchResult.StatusCode);
                        PatchResult.Dispose();
                        break;
                }
                // Log(Response); // *** MAY LEAK PASSWORDS IF ENABLED
                return JsonConvert.DeserializeObject<JObject>(Response);
            } catch (Exception e) {
                ErrorHandler(e);
                return null;
            }
        }


        static void ConnectBSky(string Username, string Password) {
            string URL = BSkyLoginHost + "/xrpc/com.atproto.server.createSession";
            JObject Result;
            JObject Body = new JObject(
                new JProperty("identifier", Username),
                new JProperty("password", Password)
            );
            Result = HttpRequest("POST", URL, Body);
            DID = Result["did"].ToString();
            AccessToken = Result["accessJwt"].ToString();
            RefreshToken = Result["refreshJwt"].ToString();
            Log(Result.ToString());
            BSkyHost = Result["didDoc"]["service"][0]["serviceEndpoint"].ToString();
            Log ("BSky API host: " + BSkyHost);
            SessionRefreshTimer.Enabled = true;
        }

        private static void RefreshBSky() {
            string URL = BSkyHost + "/xrpc/com.atproto.server.refreshSession";
            JObject Result;
            Result = HttpRequest("POST", URL, "", RefreshToken);
            AccessToken = Result["accessJwt"].ToString();
            RefreshToken = Result["refreshJwt"].ToString();
            Log(Result.ToString());
        }

        private static void GoLive(bool FirstRun = true) {
            string URL = BSkyHost + "/xrpc/com.atproto.repo.putRecord";
            JObject Result;

            JObject Body = new JObject(
                new JProperty("repo", DID),
                new JProperty("collection", "app.bsky.actor.status"),
                new JProperty("rkey", "self"),
                new JProperty("record",
                    new JObject(
                        new JProperty("$type", "app.bsky.actor.status"),
                        new JProperty("createdAt", DateTime.UtcNow),
                        new JProperty("status", "app.bsky.actor.status#live"),
                        new JProperty("durationMinutes", 2),
                        new JProperty("embed",
                            new JObject(
                                new JProperty("$type", "app.bsky.embed.external"),
                                new JProperty("external",
                                    new JObject(
                                        new JProperty("$type", "app.bsky.embed.external"),
                                        new JProperty("title", EmbedTitle),
                                        new JProperty("description", EmbedDesc),
                                        new JProperty("uri", EmbedURL)
                                    )
                                )
                            )
                        )
                    )
                )
            );
            Result = HttpRequest("POST", URL, Body, AccessToken);
            Log(Result.ToString());
            if (FirstRun) { LiveRefreshTimer.Enabled = true; }
        }
        private static void EndGoLive() {
            string URL = BSkyHost + "/xrpc/com.atproto.repo.deleteRecord";
            JObject Result;

            JObject Body = new JObject(
                new JProperty("repo", DID),
                new JProperty("collection", "app.bsky.actor.status"),
                new JProperty("rkey", "self")
            );
            Result = HttpRequest("POST", URL, Body, AccessToken);
            Log(Result.ToString());
            LiveRefreshTimer.Enabled = false;
        }

        private static void SessionRefreshHandler(object source, ElapsedEventArgs e) {
            RefreshBSky();
        }
        private static void LiveRefreshHandler(object source, ElapsedEventArgs e) {
            GoLive(false);
        }

        static void Main() {
            SessionRefreshTimer = new System.Timers.Timer();
            SessionRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(SessionRefreshHandler);
            SessionRefreshTimer.Interval = 1 * 60000;

            LiveRefreshTimer = new System.Timers.Timer();
            LiveRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(LiveRefreshHandler);
            LiveRefreshTimer.Interval = 1 * 60000;

            Log("Connecting to BlueSky...");
            ConnectBSky(BSkyUsername, BSkyPassword);

            Console.WriteLine("Press enter to go live");
            Console.ReadLine();
            Log("Going live");
            GoLive();

            Console.WriteLine("Press enter to end live");
            Console.ReadLine();
            Log("Ending go live");
            EndGoLive();
        }
    }
}