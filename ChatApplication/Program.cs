using Newtonsoft.Json;
using SharedLibrary;
using SharedModels;
using System;
using System.Net.Http;
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
                RequestUri = new Uri("https://localhost:44375/api/v1/SupportRequest"),
                Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(httpRequestMessage);

            if (response.IsSuccessStatusCode)
            {
                var readAsStringAsync = await response.Content.ReadAsStringAsync();

                var supportResponse = JsonConvert.DeserializeObject<SupportResponse>(readAsStringAsync);
                Console.WriteLine(JsonConvert.SerializeObject(supportResponse));

                //Once we receive a successfull "OK" response from the SupportRequestAccepter, the chat application starts polling
                while (true)
                {
                    var httpRequestMessageForPolling = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri("https://localhost:44375/api/v1/Poll?requestId="+ supportResponse.RequestId),
                        Content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json")
                    };

                    await client.SendAsync(httpRequestMessageForPolling);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Successfully polled!");
                    }
                    else
                    {
                        Console.WriteLine("Polling failed!");
                    }

                    //We wait for 1 second before polling again
                    await Task.Delay(1000);
                }
            }
            else
            {
                Console.WriteLine("Error while receiving response from Support Request Acceptor");
            }
        }
    }
}
