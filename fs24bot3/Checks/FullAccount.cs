using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class FullAccount : CheckAttribute
{
    public FullAccount()
    { }

    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        return !context.FromBridge && !context.User.UserIsIgnored() && context.User != null
            ? CheckResult.Successful
            : CheckResult.Failed("Эта команда требует аккаунт пользователя fs24_bot!");
    }

    public override string ToString()
    {
        return "Аккаунт пользователя";
    }
}
