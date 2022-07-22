using fs24bot3.Systems;
using fs24bot3.Core;
using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace fs24bot3.EventProcessors;
public class OnMsgEvent
{
    private readonly Bot BotContext;
    private readonly MessageGeneric Message;
    private readonly Random Rand = new Random();
    private readonly Regex YoutubeRegex = new Regex(@"(?:https?:)?(?:\/\/)?(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube(?:-nocookie)?\.com\S*?[^\w\s-])([\w-]{11})(?=[^\w-]|$)(?![?=&+%\w.-]*(?:['\""][^<>]*>|<\/a>))[?=&+%\w.-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public OnMsgEvent(Bot botCtx, in MessageGeneric message)
    {
        BotContext = botCtx;
        Message = message;
    }

    public async void LevelInscrease(Shop shop)
    {
        Message.Sender.CreateAccountIfNotExist();
        Message.Sender.SetLastMessage();
        bool newLevel = Message.Sender.IncreaseXp(Message.Body.Length * new Random().Next(1, 3));
        if (newLevel)
        {
            var report = Message.Sender.AddRandomRarityItem(shop, ItemInventory.ItemRarity.Rare);
            await BotContext.SendMessage(Message.Target, $"{Message.Sender.Username}: У вас теперь {Message.Sender.GetUserInfo().Level} уровень. Вы получили за это: {report.First().Value.Name}!");
        }
    }

    public async void DestroyWallRandomly(Shop shop)
    {
        if (Rand.Next(0, 10) == 1 && await Message.Sender.RemItemFromInv(shop, "wall", 1))
        {
            Log.Information("Breaking wall for {0}", Message.Sender.Username);
        }
    }

    public async void HandleYoutube()
    {
        foreach (var match in YoutubeRegex.Matches(Message.Body))
        {
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = ConfigurationProvider.Config.YoutubeDlPath;
                p.StartInfo.Arguments = "--simulate --print-json \"" + match + "\"";
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var jsonOutput = JsonConvert.DeserializeObject<Youtube.Root>(output, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                if (jsonOutput != null)
                {
                    var ts = TimeSpan.FromSeconds(jsonOutput.duration);
                    await BotContext.SendMessage(Message.Target, $"{IrcClrs.Bold}{jsonOutput.title}{IrcClrs.Reset} от {IrcClrs.Bold}{jsonOutput.channel}{IrcClrs.Reset}. Длительность: {IrcClrs.Bold}{ts:hh\\:mm\\:ss}{IrcClrs.Reset} {IrcClrs.Green}👍{jsonOutput.like_count} {IrcClrs.Reset}Просмотров: {jsonOutput.view_count}");
                }
            }
            catch (Exception e)
            {
                Log.Error("Cannot handle youtube: {0}:{1}", e.GetType().Name, e.Message);
            }
        }
    }

    public async void PrintWarningInformation()
    {
        if (!BotContext.AcknownUsers.Any(x => x == Message.Sender.Username) && Message.Sender.GetWarnings().Any())
        {
            await BotContext.SendMessage(Message.Target, 
            $"{IrcClrs.Gray}{Message.Sender.Username}: У вас есть предупреждения используйте {Message.Sender.GetUserPrefix()}warnings чтобы их прочесть!");
            BotContext.AcknownUsers.Add(Message.Sender.Username);
        }
    }
}