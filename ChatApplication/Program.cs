using Newtonsoft.Json;
using SharedLibrary;
using SharedModels;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ChatApplication
{
    internal class Program
    {
        private static readonly HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Chat app started!");
            await ProcessRepositories();
        }

        private static async Task ProcessRepositories()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Chat Application");

            var requestData = new SupportRequest()
            {
                FirstName = "Lasitha",
                LastName = "Konara",
                EMail = "lasitha.konara@gmail.com"
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost:44375/api/SupportRequest"),
                Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(httpRequestMessage);


            if (response.IsSuccessStatusCode)
            {
                var readAsStringAsync = await response.Content.ReadAsStringAsync();

                var supportResponse = JsonConvert.DeserializeObject<SupportResponse>(readAsStringAsync);
                Console.WriteLine(JsonConvert.SerializeObject(supportResponse));
            }
            else
            {
                Console.WriteLine("Error while receiving response from Support Request Acceptor");
            }
        }
    }
}
