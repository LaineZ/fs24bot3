using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class FishCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }

    [Command("nest")]
    [Description("Установить место рыбалки - если параметр nestname пуст, напишет список мест")]
    [Remarks("RLF - требуемый размер лески F - количество рыбы")]
    [Checks.FullAccount]
    public async Task SetNest(string nestname = "")
    {
        Context.User.SetContext(Context);

        if (nestname.Any())
        {
            var state = Context.User.SetNest(nestname);

            if (state == null)
            {
                await Context.SendMessage(Context.Channel, "[gray]Такого места для рыбалки не сущесвует");
                return;
            }
            if (state.FishingLineRequired <= Context.User.CountItem("line"))
            {
                await Context.SendMessage(Context.Channel, $"Установлено место рыбалки {nestname}");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"[gray]Слишком маленькая длинна лески! {state.FishingLineRequired} > {Context.User.CountItem("line")}");
            }
        }
        else
        {
            var queryFish = Context.BotCtx.Connection.Table<SQL.FishingNests>().ToList();

            string link = await Helpers.InternetServicesHelper.UploadToTrashbin(string.Join("\n", queryFish.Select(x => $"{x.Name}\tТребуемая длинна лески:{x.FishingLineRequired}\tКоличество рыбы:{x.FishCount} ")), "addplain");
            await Context.SendMessage(Context.Channel, "Все места для рыбалки: " + link);
        }
    }
}
