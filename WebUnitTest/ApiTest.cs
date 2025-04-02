using System.Net;
using System.Text.Json;
using Website;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
namespace WebUnitTest
{
    //[Ignore] //TODO: Fix http tests, not workink(
    [TestClass]
    public class ApiTest
    {
        private string Url = "http://localhost:5230";

        [TestInitialize]
        public async Task Setup()
        {
            //      await Program.SetupServer([]);
            //      await Task.Run(() => Program.Application.Run());
            //Url = Program.Application.Urls.First();
        }
        [TestCleanup]
        public async Task Cleanup()
        {
            try
            {
                await Program.Application.StopAsync();
            }
            catch { /* ignored */ }
        }

        [TestMethod]
        public async Task ReloadConfig()
        {
            var http = new HttpClient();
            var response = await http.PostAsync($"{Url}/debug/reloadConfig", new StringContent(""));
            IsTrue(response.IsSuccessStatusCode);
        }
        [TestMethod]
        public async Task LoginAsUser()
        {
            var http = new HttpClient();
            var response = await http.PostAsync($"{Url}/test/login/DimucaTheDev", new StringContent(""));
            IsTrue(response.IsSuccessStatusCode);
            AreNotEqual(response.Content.ReadAsStringAsync().Result, "User not exists!");

        }
        [TestMethod]
        public async Task LoginAsAdmin()
        {
            var http = new HttpClient();
            var response = await http.PostAsync($"{Url}/test/login/DimucaTheDev", new StringContent(""));
            IsTrue(response.IsSuccessStatusCode);
            IsTrue(response.Content.ReadAsStringAsync().Result.Length > 10);
        }
        [TestMethod]
        public async Task JwtTokenGetName()
        {
            var http = new HttpClient();
            var jsonToken = await (await http.PostAsync($"{Url}/test/login/DimucaTheDev", new StringContent(""))).Content.ReadAsStringAsync();
            var token = JsonDocument.Parse(jsonToken).RootElement.GetProperty("token").ToString();
            http.DefaultRequestHeaders.Authorization = new("Bearer", token);
            var response = await http.PostAsync($"{Url}/test/jwt", new StringContent(""));
            IsTrue(response.IsSuccessStatusCode);
            var result = await response.Content.ReadAsStringAsync();
            AreEqual(result.Split("!")[0].Split(" ")[1], "DimucaTheDev");
        }
        [TestMethod]
        public async Task TestBadToken()
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new("Bearer", "FakeTokenGoesHere!");
            var response = await http.PostAsync($"{Url}/test/jwt", new StringContent(""));
            IsFalse(response.IsSuccessStatusCode);
        }
        [TestMethod]
        public async Task GetCookieResult()
        {
            var http = new HttpClient();
            var result = await http.PostAsync($"{Url}/test/setcookie", new StringContent(""));
            var cookieContainer = new CookieContainer();
            var cookies = result.Headers.SingleOrDefault(s => s.Key == "Set-Cookie").Value;
            foreach (var cookie in cookies)
                cookieContainer.SetCookies(new(Url), cookie);
            http = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer });
            var testResponse = await http.PostAsync($"{Url}/test/getcookie", new StringContent(""));
            var content = await testResponse.Content.ReadAsStringAsync();
            AreEqual(content, "Hello, World!");
        }
    }
}
