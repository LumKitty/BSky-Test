// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace BSkyLive {
    static class BSkyLive {
        private static HttpClient client = new HttpClient();

        private const string BSkyLoginHost = "https://inkcap.us-east.host.bsky.network";

        private static string? BSkyPassword = null;
        private static string? EmbedTitle = null;
        private static string? EmbedDesc = null;
        private static string? EmbedURL = null;
        private static string? BSkyHost = null;
        private static string? AccessToken = null;
        private static string? RefreshToken = null;
        private static string? DID = null;

        private static System.Timers.Timer SessionRefreshTimer = new System.Timers.Timer();
        private static System.Timers.Timer LiveRefreshTimer = new System.Timers.Timer();

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
                // Log(Response);
                return JsonConvert.DeserializeObject<JObject>(Response);
            } catch (Exception e) {
                ErrorHandler(e);
                return null;
            }
        }

        public static void ConnectBSky(string Username, string Password) {
            BSkyPassword = Password;
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
            
            SessionRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(SessionRefreshHandler);
            SessionRefreshTimer.Interval = 1 * 60000;
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

        private static void GoLive() {
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
        }
        public static void GoLive(string URL, string Title, string Desc) {
            EmbedURL = URL;
            EmbedTitle = Title;
            EmbedDesc = Desc;
            GoLive();
            LiveRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(LiveRefreshHandler);
            LiveRefreshTimer.Interval = 1 * 60000;
        }
        public static void EndGoLive() {
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
            GoLive();
        }
    }
}