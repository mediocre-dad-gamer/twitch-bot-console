using System;
using System.Threading;
using System.Threading.Tasks;
using IrcDotNet;
using TwitchBotConsole.BotApi;
using TwitchBotConsole.Configuration;

namespace TwitchBotConsole.Irc
{
    public class TwitchChatClient
    {
        private readonly TwitchSettings _twitchSettings;

        private readonly BotApiClient _botApiClient;

        private readonly EventHandler<IrcMessageEventArgs> _channelMessageReceivedHandler;

        private Task _clientTask;

        public TwitchChatClient(TwitchSettings twitchSettings, BotApiClient botApiClient)
        {
            _twitchSettings = twitchSettings;
            _botApiClient = botApiClient;
            _channelMessageReceivedHandler = new EventHandler<IrcMessageEventArgs>(async (sender, e) => await IrcClient_Channel_MessageReceived(sender, e));
            ConnectToIrc();
        }

        private void ConnectToIrc()
        {
            _clientTask = Task.Run(async () =>
            {
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
                                    Password = _twitchSettings.Password,
                                    UserName = _twitchSettings.Username
                                });
                            if (!connectedEvent.Wait(10000))
                            {
                                Console.WriteLine($"Connection to '{_twitchSettings.Server}' timed out.");
                                return;
                            }
                        }
                        Console.Out.WriteLine($"Now connected to '{_twitchSettings.Server}'.");
                        if (!registeredEvent.Wait(10000))
                        {
                            Console.WriteLine($"Could not register to '{_twitchSettings.Server}'.");
                            return;
                        }
                    }

                    Console.Out.WriteLine($"Now registered to '{_twitchSettings.Server}' as '{_twitchSettings.Username}'.");
                    await KeepClientAlive(client);
                }
            });
        }

        private async Task KeepClientAlive(IrcDotNet.TwitchIrcClient client)
        {
            while (true)
            {
                await Task.Delay(1000);
            }
        }

        private void IrcClient_Registered(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;

            client.LocalUser.NoticeReceived += IrcClient_LocalUser_NoticeReceived;
            client.LocalUser.MessageReceived += IrcClient_LocalUser_MessageReceived;
            client.LocalUser.JoinedChannel += IrcClient_LocalUser_JoinedChannel;
            client.LocalUser.LeftChannel += IrcClient_LocalUser_LeftChannel;

            client.SendRawMessage($"JOIN #{_twitchSettings.ChannelToJoin}");
        }

        private void IrcClient_Disconnected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
        }

        private void IrcClient_LocalUser_LeftChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined -= IrcClient_Channel_UserJoined;
            e.Channel.UserLeft -= IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived -= _channelMessageReceivedHandler;
            e.Channel.NoticeReceived -= IrcClient_Channel_NoticeReceived;

            Console.WriteLine($"You left the channel {e.Channel.Name}.");
        }

        private void IrcClient_LocalUser_JoinedChannel(object sender, IrcChannelEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            e.Channel.UserJoined += IrcClient_Channel_UserJoined;
            e.Channel.UserLeft += IrcClient_Channel_UserLeft;
            e.Channel.MessageReceived += _channelMessageReceivedHandler;
            e.Channel.NoticeReceived += IrcClient_Channel_NoticeReceived;

            Console.WriteLine($"You joined the channel {e.Channel.Name}.");
        }

        private void IrcClient_Channel_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;

            Console.WriteLine($"[{channel.Name}] Notice: {e.Text}.", channel.Name, e.Text);
        }

        private async Task IrcClient_Channel_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var channel = (IrcChannel)sender;
            if (e.Source is IrcUser)
            {
                Console.WriteLine($"[{channel.Name}]({e.Source.Name}): {e.Text}.");

                var result = await _botApiClient.Process(e.Text, e.Source.Name);

                if (!string.IsNullOrEmpty(result))
                {
                    channel.Client.SendRawMessage($"privmsg #{_twitchSettings.ChannelToJoin} :{result}");
                }
            }
            else
            {
                Console.WriteLine($"[{channel.Name}]({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private void IrcClient_Channel_UserLeft(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            Console.WriteLine($"[{channel.Name}] User {e.ChannelUser.User.NickName} left the channel.");
        }

        private void IrcClient_Channel_UserJoined(object sender, IrcChannelUserEventArgs e)
        {
            var channel = (IrcChannel)sender;
            Console.WriteLine($"[{channel.Name}] User {e.ChannelUser.User.NickName} joined the channel.");
        }

        private void IrcClient_LocalUser_MessageReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;

            if (e.Source is IrcUser)
            {
                Console.WriteLine($"({e.Source.Name}): {e.Text}.");
            }
            else
            {
                Console.WriteLine($"({e.Source.Name}) Message: {e.Text}.");
            }
        }

        private void IrcClient_LocalUser_NoticeReceived(object sender, IrcMessageEventArgs e)
        {
            var localUser = (IrcLocalUser)sender;
            Console.WriteLine($"Notice: {e.Text}.");
        }

        private void IrcClient_Connected(object sender, EventArgs e)
        {
            var client = (IrcClient)sender;
        }
    }
}