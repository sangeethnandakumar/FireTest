using Cocona;
using FireTest;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;
using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;



var app = CoconaApp.Create();



app.AddCommand("run", (
    [Argument(Name = "fileName", Description = "YML filename")] string fileName,
    [Option('p')] int? noOfParallelCalls,
    [Option('i')] int? noOfIterations
    ) =>
{
    FigletHeader();

    //Derive TestData
    var testData = Utilities.MakeTestData( fileName );
    testData.Parallelism = noOfParallelCalls.Value;
    testData.Iterations = noOfIterations.Value;

    //Run
    CommandDriver.RunAsync(fileName, testData);
})
.WithDescription("Runs a test defined in YML file.");





app.AddCommand("list", () =>
{
    FigletHeader();
    var yml = CommandDriver.ListCommand();

    if(yml == "(Back to menu)")
    {
        DisplayMenu();
    }

    //Derive TestData
    var testData = Utilities.MakeTestData(yml);
    testData.Parallelism = AnsiConsole.Ask<int>("No: of parallel [green]requests[/]: ");
    testData.Iterations = AnsiConsole.Ask<int>("No: of [green]iterations[/]: ");

    //Run
    CommandDriver.RunAsync(yml, testData);
})
.WithDescription("List all available YML file in current directory.");






app.AddCommand("create", ([Argument] string fileName) =>
{
    FigletHeader();
    CommandDriver.CreateCommand();
})
.WithDescription("Generates the YML template file.");





app.AddCommand(() =>
{
    FigletHeader();
    DisplayMenu();
})
.WithDescription("Runs a test defined in YML file.");


app.Run();


static void FigletHeader()
{
    AnsiConsole.Write(
    new FigletText("FireTest")
        .LeftJustified()
        .Color(Color.Red1));
}

static void DisplayMenu()
{
    var option = AnsiConsole.Prompt(
              new SelectionPrompt<string>()
                  .Title("Choose an option:")
                  .PageSize(10)
                  .MoreChoicesText("[grey](Move up and down to select opptions)[/]")
                  .AddChoices(new[] {
                    "1. Run Test",
                    "2. List YML",
                    "3. Create YML",
                  }));
    RunCommandArgs testData = new();
    switch (option)
    {
        case "1. Run Test":
            //Derive TestData
            var file = AnsiConsole.Ask<string>("YML [green]file[/]: ");
            var p = AnsiConsole.Ask<int>("No: of parallel [green]requests[/]: ");
            var i = AnsiConsole.Ask<int>("No: of [green]iterations[/]: ");
            testData = Utilities.MakeTestData(file);
            testData.Parallelism = p;
            testData.Iterations = i;
            //Run
            CommandDriver.RunAsync(file, testData);
            break;
        case "2. List YML":
            var yml = CommandDriver.ListCommand();
            if (yml == "(Back to menu)")
            {
                DisplayMenu();
            }
            //Derive TestData
            testData = Utilities.MakeTestData(yml);
            testData.Parallelism = AnsiConsole.Ask<int>("No: of parallel [green]requests[/]: ");
            testData.Iterations = AnsiConsole.Ask<int>("No: of [green]iterations[/]: ");
            //Run
            CommandDriver.RunAsync(yml, testData);
            break;
        case "3. Create YML":
            CommandDriver.CreateCommand();
            break;
    }
}