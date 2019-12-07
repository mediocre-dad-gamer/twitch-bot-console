using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace TwitchBotConsole.BotApi.Requests
{
    public abstract class RequestBase
    {
        public virtual StringContent GetJsonContent()
        {
            var serializedSelf = JsonConvert.SerializeObject(this);
            return new StringContent(serializedSelf, Encoding.UTF8, "application/json");
        }
    }
}