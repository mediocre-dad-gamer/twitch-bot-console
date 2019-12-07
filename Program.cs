using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TwitchBotConsole.Configuration;

namespace TwitchBotConsole
{
    class Program
    {
        private static IConfigurationRoot Configuration { get; set; }

        private static readonly HttpClient _webClient = new HttpClient();

        private static TwitchSettings _twitchSettings { get; set; }

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();

            builder.AddJsonFile("appsettings.json");

            Configuration = builder.Build();
            var twitchSettings = new TwitchSettings();
            Configuration.GetSection("TwitchSettings").Bind(twitchSettings);
            _twitchSettings = twitchSettings;
            var password = Configuration["Twitch:OAuthSecret"];
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new IrcDotNet.TwitchIrcClient())
            {
                client.FloodPreventer = new IrcStandardFloodPreventer(4, 2000);
                client.Disconnected += IrcClient_Disconnected;
                client.Registered += IrcClient_Registered;
                // Wait until connection has succeeded or timed out.
                using (var registeredEvent = new ManualResetEventSlim(false))
                {
                    using (var connectedEvent = new ManualResetEventSlim(false))
                    {
                        client.Connected += (sender2, e2) => connectedEvent.Set();
                        client.Registered += (sender2, e2) => registeredEvent.Set();
                        client.Connect(_twitchSettings.Server, false,
                            new IrcUserRegistrationInfo()
                            {
                                NickName = _twitchSettings.Username,
                                Password = password,
                                UserName = _twitchSettings.Username
                            });
                        if (!connectedEvent.Wait(10000))
                        {
                            Console.WriteLine($"Connection to '{_twitchSettings.Server}' timed out.");
                            return;
                        }
                    }
                    Console.Out.WriteLine($"Now connected to '{_twitchSettings.Server}'.");
                    client.SendRawMessage($"JOIN #{_twitchSettings.ChannelToJoin}");
                    if (!registeredEvent.Wait(10000))
                    {
                        Console.WriteLine($"Could not register to '{_twitchSettings.Server}'.");
                        return;
                    }
                }

                Console.Out.WriteLine($"Now registered to '{_twitchSettings.Server}' as '{_twitchSettings.Username}'.");
                HandleEventLoop(client);
            }
        }

        private static void HandleEventLoop(IrcDotNet.IrcClient client)
        {
            bool isExit = false;
            while (!isExit)
            {
                Console.Write("> ");
                var command = Console.ReadLine();
                switch (command)
                {
                    case "exit":
                        isExit = true;
                        break;
                    default:
                        if (!string.IsNullOrEmpty(command))
                        {
                            client.SendRawMessage(command);
                        }
                        break;
                }
            }
            client.Disconnect();
        }

        private static void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;
        }

        private static void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined -= IrcClient_Channel_UserJoined;
            e.Channel.UserLeft -= IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived -= IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;

            Console.WriteLine($"You left the channel {e.Channel.Name}.");
        }

        private static void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined += IrcClient_Channel_UserJoined;
            e.Channel.UserLeft += IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived += IrcClient_Channel_MessageReceived;
            e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;

            Console.WriteLine($"You joined the channel {e.Channel.Name}.");
        }

        private static void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;

            Console.WriteLine($"[{channel.Name}] Notice: {e.Text}.", channel.Name, e.Text);
        }

        private static void IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            if (e.Source is IrcUser)
            {
                Console.WriteLine($"[{channel.Name}]({e.Source.Name}): {e.Text}.");
                try
                {
                    var requestBody = new
                    {
                        Message = e.Text,
                        Username = e.Source.Name
                    };

                    var serializedBody = JsonConvert.SerializeObject(requestBody);

                    var postRequest = _webClient.PostAsync(
                        "https://localhost:5001/process",
                        new StringContent(serializedBody, Encoding.UTF8, "application/json"));

                    Task.WaitAll(postRequest);

                    var asyncResult = postRequest.Result.Content.ReadAsStringAsync();

                    Task.WaitAll(asyncResult);

                    if (!string.IsNullOrEmpty(asyncResult.Result))
                    {
                        channel.Client.SendRawMessage($"privmsg #{_twitchSettings.ChannelToJoin} :{asyncResult.Result}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                }
            }
            else
            {
                Console.WriteLine($"[{channel.Name}]({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private static void IrcClient_Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            Console.WriteLine($"[{channel.Name}] User {e.ChannelUser.User.NickName} left the channel.");
        }

        private static void IrcClient_Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            Console.WriteLine($"[{channel.Name}] User {e.ChannelUser.User.NickName} joined the channel.");
        }

        private static void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            if (e.Source is IrcUser)
            {
                // Read message.
                Console.WriteLine($"({e.Source.Name}): {e.Text}.");
            }
            else
            {
                Console.WriteLine($"({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private static void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            Console.WriteLine($"Notice: {e.Text}.");
        }

        private static void IrcClient_Disconnected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
        }

        private static void IrcClient_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
        }
    }
}
