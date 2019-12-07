namespace TwitchBotConsole.BotApi.Requests
{
    public class ProcessRequest : RequestBase
    {
        public string Message { get; set; }

        public string Username { get; set; }

        public ProcessRequest(string message, string username)
        {
            Message = message;
            Username = username;
        }
    }
}