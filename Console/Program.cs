using Windows.Win32.UI.Accessibility;
using Shared;
using System.CommandLine;

var program = CreateCommand();

// Execute the command line parser
return await program.Parse(args).InvokeAsync();

// List all top-level windows with their titles
var automation = UIAHelpers.CreateUIAutomationInstance();
var root = automation.GetRootElement();
var condition = automation.CreateTrueCondition();
var children = root.FindAll(TreeScope.TreeScope_Children, condition);
for (int i = 0; i < children.Length; i++)
{
    var element = children.GetElement(i);
    var name = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId);
    var automationId = (string)element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId);

    // Check to see if this element has children
    var childCondition = automation.CreateTrueCondition();
    var childElements = element.FindFirst(TreeScope.TreeScope_Children, childCondition);
    var hasChildren = childElements != null;

    Console.WriteLine($"{i}: Name='{name}', AutomationId='{automationId}'");
    if (hasChildren)
    {
        Console.WriteLine("   + [...]");
    }
}

static RootCommand CreateCommand()
{
    var listWindowsCommand = new Command("list", "List all top-level windows with their titles")
    {
    };
    var windowsCommands = new Command("windows", "HWND tools")
    {
        listWindowsCommand,
    };

    var program = new RootCommand("Simple accessibility tools")
    {
        windowsCommands,
    };
    return program;
}