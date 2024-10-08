﻿using fs24bot3.Helpers;
using fs24bot3.Models;
using Qommon.Collections;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace fs24bot3.Core;
class CustomExecutor
{
    private Random Random;
    private readonly List<int> Indices = new List<int>();
    private SQL.CustomUserCommands LastCommand;
    private readonly Bot Bot;

    public CustomExecutor(Bot botCtx)
    {
        Random = new Random();
        Bot = botCtx;
    }

    public async void Execute(SQL.CustomUserCommands command, string senderNick, string channel, string args)
    {
        string[] outputs = command.Output.Split("||");
        var user = User.PickRandomUser(Bot.Connection); 
        var nick = MessageHelper.AntiHightlight(user.Username);

        int index = 0;

        if (outputs.Length > 1)
        {
            if (int.TryParse(args, out int result))
            {
                // if args contains output number
                if (result > outputs.Length - 1 || result < 0)
                {
                    await Bot.Client.SendMessage(channel, $"Учтите в следующий раз, здесь максимум: {outputs.Length - 1}, поэтому показано рандомное сообщение");
                }
                else
                {
                    index = result;
                }
            }
            else
            {
                // if args contains a string
                if (args != null && args.Any())
                {
                    Log.Verbose("Args string is not empty!");
                    Random = new Random(args.GetHashCode() + senderNick.GetHashCode());
                    index = Random.Next(outputs.Length - 1);
                }
                else
                {
                    if (LastCommand == null || command.Command != LastCommand.Command || !Indices.Any())
                    {
                        Random = new Random();
                        Indices.Clear();
                        LastCommand = command;
                        for (int i = 0; i < outputs.Length - 1; i++) { Indices.Add(i); }
                        Indices.Shuffle();
                        Log.Verbose("Regenerating indices...");
                    }

                    index = Indices[0];
                    Indices.RemoveAt(0);
                }
            }
        }

        StringBuilder argsFinal = new StringBuilder(outputs[index]);
        argsFinal.Replace("#USERINPUT", args);
        argsFinal.Replace("#USERNAME", senderNick);
        argsFinal.Replace("#RNDNICK", nick);
        argsFinal.Replace("#RNG", Random.Next(int.MinValue, int.MaxValue).ToString());

        await Bot.Client.SendMessage(channel, argsFinal.ToString());
    }
}
