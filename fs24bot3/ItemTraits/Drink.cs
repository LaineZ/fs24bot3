using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits;

public class Drink : IItem
{
    public string Name { get; }
    public int Price { get; set; }
    public bool Sellable { get; set; }
    private int DrunkLevel { get; }
    public ItemInventory.ItemRarity Rarity { get; set; }

    public Drink(string name, int drunk, int price, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Common)
    {
        Name = name;
        Price = price;
        Sellable = true;
        Rarity = rarity;
        DrunkLevel = drunk;
    }
    public async Task<bool> OnUseMyself(Bot botCtx, string channel, User user)
    {
        string randStr = string.Join(" ", RandomMsgs.DrunkMessages.Random());
        await botCtx.Client.SendMessage(channel, $"{user.Username} выпил {Name} ({DrunkLevel}) и сказал: {randStr}");
        return true;
    }
}