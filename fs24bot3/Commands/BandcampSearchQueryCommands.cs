using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;

namespace fs24bot3.Commands;
public sealed class BandcampSearchQueryCommands : ModuleBase<BandcampSearchCommandProcessor.CustomCommandContext>
{

    [Command("page")]
    public void FilterPage(uint page)
    {
        Context.Page = (int)Math.Clamp(page, 1, 200); ;
    }

    [Command("max")]
    public void FilterMaxDepth(uint maxdepth)
    {
        Context.Max = (int)Math.Clamp(maxdepth, 1, 5);
    }

    [Command("limit")]
    public void FilterLimit(uint maxdepth)
    {
        Context.Limit = (int)Math.Clamp(maxdepth, 1, 5);
    }

    [Command("format")]
    public void FilterFormat(string format)
    {
        Context.Format = format;
    }

    [Command("sort")]
    public void FilterSort(string sort)
    {
        Context.Sort = sort;
    }

    [Command("location")]
    public void Location(int loc)
    {
        Context.Location = loc;
    }
}
