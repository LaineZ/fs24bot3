using System.Threading.Tasks;

namespace fs24bot3.ItemTraits;

public interface IItem
{
    public string Name { get; }
    public int Price { get; set; }
    public bool Sellable { get; set; }
    public Models.ItemInventory.ItemRarity Rarity { get; set; }

    public async Task<bool> OnUseMyself(Bot botCtx, string channel, Core.User user)
    {
        await botCtx.Client.SendMessage(channel, "Этот предмет невозможно использовать на себе!");
        return false;
    }
    public async Task<bool> OnUseOnUser(Bot botCtx, string channel, Core.User user, Core.User targetUser)
    {
        await botCtx.Client.SendMessage(channel, "Этот предмет невозможно применить на другом пользователе!");
        return false;
    }

    public bool OnAdd(Bot botCtx, string channel, Core.User user, Core.User targetUser)
    {
        return false;
    }

    public bool OnDel(Bot botCtx, string channel, Core.User user, Core.User targetUser)
    {
        return false;
    }
}