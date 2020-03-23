using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fs24bot3
{
    public sealed class InventoryCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("inv", "inventory")]
        [Qmmands.Description("Инвентарь")]
        public void Userstat()
        {
            UserOperations usr = new UserOperations(Context.Message.User, Context.Connection);
            var userinfo = usr.GetUserInfo();
            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);

            Context.Socket.SendMessage(Context.Channel, Context.Message.User + ": " + string.Join(" ", userInv.Items.Select(x => $"{x.Name} x{x.Count}")));
        }

        [Command("buy")]
        [Qmmands.Description("Купить товар")]
        public void Buy(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            if (user.RemItemFromInv("money", Shop.getItem("money").Price * count))
            {
                user.AddItemToInv(itemname, count);
                Context.Socket.SendMessage(Context.Channel, "Вы успешно купили " + Shop.getItem(itemname).Name + " за " + Shop.getItem(itemname).Price * count + " денег");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Недостаточно денег: " + Shop.getItem("money").Price * count);
            }
        }

        [Command("sell")]
        [Qmmands.Description("Продать товар")]
        public void Sell(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            if (user.RemItemFromInv(Shop.getItem(itemname).Name, count))
            {
                user.AddItemToInv("money", Shop.getItem(itemname).Price * count);
                Context.Socket.SendMessage(Context.Channel, "Вы успешно продали " + Shop.getItem(itemname).Name + " за " + Shop.getItem(itemname).Price * count + " денег");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Вы не можете это продать!");
            }
        }
    }
}
