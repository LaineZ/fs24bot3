using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class FullAccount : CheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        return !context.FromBridge && !context.User.UserIsIgnored() && context.User != null && context.IsAuthorizedAction
            ? CheckResult.Successful
            : CheckResult.Failed("Эта команда требует аккаунт fs24_bot и авторизацию через NickServ!");
    }

    public override string ToString()
    {
        return "Аккаунт пользователя";
    }
}
