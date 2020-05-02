using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using NetIRC.Messages;
using NetIRC.Connection;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace fs24bot3
{
    public class CommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public PrivMsgMessage Message { get; }
            public NetIRC.Client Client { get; }
            public SQLiteConnection CacheConnection;

            public string Channel;

            public SQLiteConnection CacheConnetion;

            public SQLiteConnection Connection;

            HttpTools http = new HttpTools();

            public VkApi VKApi;

            // Pass your service provider to the base command context.
            public CustomCommandContext(PrivMsgMessage message, NetIRC.Client client, SQLiteConnection connection, SQLiteConnection connectCache, VkApi api, IServiceProvider provider = null) : base(provider)
            {
                Message = message;
                Client = client;
                Connection = connection;
                CacheConnection = connectCache;
                VKApi = api;

                if (Message.To == Configuration.name)
                {
                    Channel = Message.From;
                }
                else
                {
                    Channel = Message.To;
                }

            }
            // created just for compatibilty
            public async void SendMessage(string channel, string message)
            {
                // this check will make because if message len < 480 this function return incorrect values
                if (Encoding.Unicode.GetByteCount(message) > 449)
                {
                    foreach (var slice in Core.MessageUtils.SplitMessage(message, 449))
                    {
                        await Client.SendAsync(new PrivMsgMessage(channel, slice));
                    }
                }
                else
                {
                    await Client.SendAsync(new PrivMsgMessage(channel, message));
                }
            }

            public async void SendMultiLineMessage(string content)
            {
                int count = 0;
                foreach (string outputstr in content.Split("\n"))
                  {
                    if (!string.IsNullOrWhiteSpace(outputstr))
                    {
                        await Client.SendAsync(new PrivMsgMessage(this.Channel, this.Message.From + ": " + outputstr));
                    }
                    count++;
                    if (count > 4)
                    {
                        string link = await http.UploadToPastebin(content);
                        await Client.SendAsync(new PrivMsgMessage(this.Channel, this.Message.From + ": Полный вывод здесь: " + link));
                        break;
                    }
                }
            }
        }
    }
}
