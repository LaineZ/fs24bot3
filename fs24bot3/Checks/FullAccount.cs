using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;

public sealed class FullAccount : CheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        if (context?.User is null)
        {
            return CheckResult.Failed("Нет аккаунта пользователя");
        }

        return !context.FromBridge && context.IsAuthorizedAction
            ? CheckResult.Successful
            : CheckResult.Failed("Эта команда требует аккаунт fs24_bot и авторизацию через NickServ!");
    }

    public override string ToString()
    {
        return "Аккаунт пользователя";
    }
}