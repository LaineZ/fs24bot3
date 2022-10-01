using fs24bot3.Core;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class UnPpcable : CheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        return context.PerformPpc
            ? CheckResult.Failed("Эта команда не может использовать быть ппцнута")
            : CheckResult.Successful;
    }

    public override string ToString()
    {
        return "Не использовать p";
    }
}
