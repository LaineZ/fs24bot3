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
        private Random Random;
        private SQL.CustomUserCommands Command { get; }

        public CustomExecutor(Client client, SQLite.SQLiteConnection connect, SQL.CustomUserCommands command)
        {
            Random = new Random();
            Client = client;
            Connect = connect;
            Command = command;
        }

        public async void Execute(string senderNick, string channel, string message, string args)
        {
            string[] outputs = Command.Output.Split("||");
            var arr = Connect.Table<SQL.UserStats>().ToList();
            var nick = MessageUtils.AntiHightlight(arr[Random.Next(0, arr.Count - 1)].Nick);
            int index = Random.Next(outputs.Length - 1);

            if (int.TryParse(args, out int result))
            {
                if (result > outputs.Length - 1 || result < 0)
                {
                    await Client.SendAsync(new PrivMsgMessage(channel, $"Учтите в следующий раз, здесь максимум: {outputs.Length - 1}, поэтому показано рандомное сообщение"));
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
                    Random = new Random(args.GetHashCode());
                }
            }

            StringBuilder argsFinal = new StringBuilder(outputs[index]);
            argsFinal.Replace("#USERINPUT", args);
            argsFinal.Replace("#USERNAME", senderNick);
            argsFinal.Replace("#RNDNICK", nick);
            argsFinal.Replace("#RNG", Random.Next(int.MinValue, int.MaxValue).ToString());

            await Client.SendAsync(new PrivMsgMessage(channel, argsFinal.ToString()));
        }
    }
}
