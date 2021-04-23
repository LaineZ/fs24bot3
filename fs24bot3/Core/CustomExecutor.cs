using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using NLua;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace fs24bot3.Core
{
    class CustomExecutor
    {
        private Client Client { get; }
        private SQLite.SQLiteConnection Connect { get; }
        private List<ParsedIRCMessage> MessageBus { get; }
        private SQL.CustomUserCommands Command { get; }

        public CustomExecutor(Client client, SQLite.SQLiteConnection connect, List<ParsedIRCMessage> messageBus, SQL.CustomUserCommands command)
        {
            Client = client;
            Connect = connect;
            MessageBus = messageBus;
            Command = command;
        }

        public async void Execute(string senderNick, string channel, string message, string args)
        {
            var random = new Random();
            string[] outputs = Command.Output.Split("||");
            int index = 0;
            var arr = Connect.Table<SQL.UserStats>().ToList();
            var nick = MessageUtils.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);

            if (int.TryParse(args, out int result))
            {
                if (result > outputs.Length - 1 || result < 0)
                {
                    await Client.SendAsync(new PrivMsgMessage(channel, $"Учтите в следующий раз, здесь максимум: {outputs.Length - 1}, поэтому показано рандомное сообщение"));
                    index = random.Next(outputs.Length - 1);
                }
                else
                {
                    index = result;
                }
            }
            else
            {
                if (args.Any())
                {
                    Log.Verbose("Args string is not empty!");
                    random = new Random(args.GetHashCode());
                }
                index = random.Next(outputs.Length - 1);
            }

            StringBuilder argsFinal = new StringBuilder(outputs[index]);
            argsFinal.Replace("#USERINPUT", args);
            argsFinal.Replace("#USERNAME", senderNick);
            argsFinal.Replace("#RNDNICK", nick);
            argsFinal.Replace("#RNG", random.Next(int.MinValue, int.MaxValue).ToString());

            await Client.SendAsync(new PrivMsgMessage(channel, argsFinal.ToString()));
        }
    }
}
