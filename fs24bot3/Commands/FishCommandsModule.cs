using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fs24bot3
{
    public sealed class FishCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("buyrod")]
        [Description("Купить удочочку - если параметр rodname пуст, напишет список удочек")]
        public void Buyrod(string rodname = "")
        {
            if (rodname.Length > 0)
            {
                // TODO: buy code
            }
            else
            {
                var query = Context.Connection.Table<Models.SQL.UserFishingRods>().ToList();

                Context.SendMessage(Context.Channel, string.Join(" ", query));
            }
        }
    }
}
