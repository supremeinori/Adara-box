using System.Collections.Generic;
using System.Linq;
using AdaraBox.Core.Common;
using AdaraBox.Core.Modules;
using Xunit;

namespace AdaraBox.Core.Tests;

public class ModuleTests
{
    [Fact]
    public void Modules_Have_Correct_Order_And_Honest_Status()
    {
        var modules = new IAppModule[]
        {
            new WorkspaceModule(),
            new MediaModule(),
            new StatsModule(),
            new SettingsModule()
        };

        var ordered = modules.OrderBy(m => m.Order).ToList();

        // 1. Check order: Workspace (1), Media (2), Stats (3), Settings (4)
        Assert.Equal("workspace", ordered[0].Id);
        Assert.Equal("media", ordered[1].Id);
        Assert.Equal("stats", ordered[2].Id);
        Assert.Equal("settings", ordered[3].Id);

        // 2. Check honest status
        Assert.Equal(ModuleStatus.Available, ordered[0].Status);
        Assert.Null(ordered[0].StatusMessage);

        Assert.Equal(ModuleStatus.Available, ordered[1].Status);
        Assert.Null(ordered[1].StatusMessage);

        Assert.Equal(ModuleStatus.Available, ordered[2].Status);
        Assert.Null(ordered[2].StatusMessage);

        Assert.Equal(ModuleStatus.Available, ordered[3].Status);
        Assert.Null(ordered[3].StatusMessage);
    }
}
