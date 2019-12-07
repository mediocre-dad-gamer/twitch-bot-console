using System;
using System.Net.Http;
using System.Threading.Tasks;
using TwitchBotConsole.BotApi.Requests;
using TwitchBotConsole.Configuration;

namespace TwitchBotConsole.BotApi
{
    public class BotApiClient
    {
        private readonly HttpClient _webClient;
        private Uri _baseRequestUri;

        public BotApiClient(BotApiSettings settings)
        {
            _webClient = new HttpClient();
            _baseRequestUri = settings.FullUri;
        }

        public async Task<string> Process(string message, string username)
        {
            try
            {
                var request = new ProcessRequest(message, username);
                var proccessUri = new Uri(_baseRequestUri, "process");
                var postRequest = await _webClient.PostAsync(proccessUri, request.GetJsonContent());
                var response = await postRequest.Content.ReadAsStringAsync();

                return response;
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
            return null;
        }
    }
}