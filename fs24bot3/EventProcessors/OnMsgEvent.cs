using fs24bot3.Systems;
using fs24bot3.Core;
using fs24bot3.Models;
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
    private readonly Random Rand = new Random();
    private readonly Regex YoutubeRegex = new Regex(@"(?:https?:)?(?:\/\/)?(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube(?:-nocookie)?\.com\S*?[^\w\s-])([\w-]{11})(?=[^\w-]|$)(?![?=&+%\w.-]*(?:['\""][^<>]*>|<\/a>))[?=&+%\w.-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public OnMsgEvent(Bot botCtx)
    {
        BotContext = botCtx;
    }

    public async void LevelInscrease(Shop shop, MessageGeneric message)
    {
        message.Sender.CreateAccountIfNotExist();
        message.Sender.SetLastMessage();
        bool newLevel = message.Sender.IncreaseXp(message.Body.Length * new Random().Next(1, 3));
        if (newLevel)
        {
            var report = message.Sender.AddRandomRarityItem(shop, ItemInventory.ItemRarity.Rare);
            await BotContext.Client.SendMessage(message.Target, $"{message.Sender.Username}: У вас теперь {message.Sender.GetUserInfo().Level} уровень. Вы получили за это: {report.First().Value.Name}!");
        }
    }

    public async void DestroyWallRandomly(Shop shop, MessageGeneric message)
    {
        if (Rand.Next(0, 10) == 1 && await message.Sender.RemItemFromInv(shop, "wall", 1))
        {
            Log.Information("Breaking wall for {0}", message.Sender.Username);
        }
    }

    public async void HandleYoutube(MessageGeneric message)
    {
        foreach (var match in YoutubeRegex.Matches(message.Body))
        {
            try
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = ConfigurationProvider.Config.Services.YoutubeDlPath;
                p.StartInfo.Arguments = "--simulate --print-json \"" + match + "\"";
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                var jsonOutput = JsonConvert.DeserializeObject<Youtube.Root>(output, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                if (jsonOutput != null)
                {
                    var ts = TimeSpan.FromSeconds(jsonOutput.duration);
                    await BotContext.Client.SendMessage(message.Target, 
                        $"[b]{jsonOutput.title}[r] от [b]{jsonOutput.channel}[r]. " +
                        $"[green]Длительность: [r][b]{ts:hh\\:mm\\:ss}[r] " +
                        $"[green]👍[r][b] {jsonOutput.like_count}[r] " +
                        $"[green]Просмотров: [r][b]{jsonOutput.view_count}[r] " +
                        $"[green]Дата загрузки: [r][b]{jsonOutput.upload_date}[r]");
                }
            }
            catch (Exception e)
            {
                Log.Error("Cannot handle youtube: {0}:{1}", e.GetType().Name, e.Message);
            }
        }
    }

    public async void PrintWarningInformation(MessageGeneric message)
    {
        if (BotContext.AcknownUsers.All(x => x != message.Sender.Username) && message.Sender.GetWarnings().Any())
        {
            await BotContext.Client.SendMessage(message.Target, 
            $"[gray]{message.Sender.Username}: " +
            $"У вас есть предупреждения используйте {message.Sender.GetUserPrefix()}warnings чтобы их прочесть!");
            BotContext.AcknownUsers.Add(message.Sender.Username);
        }
    }
}