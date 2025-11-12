using System.CommandLine;

var program = CreateCommand();

// Execute the command line parser
return await program.Parse(args).InvokeAsync();

static RootCommand CreateCommand()
{
    var listWindowsCommand = new Command("list", "List all top-level windows with their titles")
    {
    };
    listWindowsCommand.SetAction((parse) =>
    {
        Cmd.Commands.WindowCommands.ListWindows();
    });
    var windowsCommands = new Command("window", "HWND tools")
    {
        listWindowsCommand,
    };

    var listTreeCommand = new Command("list", "List all top-level UI Automation elements")
    {
    };
    listTreeCommand.SetAction((parse) =>
    {
        Cmd.Commands.TreeCommands.ListTopLevelTree();
    });
    var treeCommands = new Command("tree", "UIA tree tools")
    {
        listTreeCommand,
    };

    var eventWatchCommand = new Command("watch", "Watch UIA events")
    {
    };
    eventWatchCommand.SetAction((parse) =>
    {
        Cmd.Commands.EventCommands.WatchEvents();
    });
    var eventCommands = new Command("event", "UIA event")
    {
        eventWatchCommand,
    };

    var program = new RootCommand("Simple accessibility tools")
    {
        windowsCommands,
        treeCommands,
        eventCommands,
    };
    return program;
}