using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class PreProcess : CheckAttribute
{
    public PreProcess()
    { }

    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as SearchCommandProcessor.CustomCommandContext;

        return context.PreProcess
            ? CheckResult.Successful
            : CheckResult.Failed(string.Empty);
    }
}
