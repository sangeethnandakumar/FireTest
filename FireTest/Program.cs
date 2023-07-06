using Cocona;
using FireTest;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;
using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


await CoconaApp.RunAsync(async (string? file, int? p, int? i) =>
{
    AnsiConsole.Write(
    new FigletText("FireTest")
        .LeftJustified()
        .Color(Color.Red1));

    if (file is null)
    {
restart:
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
        switch (option)
        {
            case "1. Run Test":
                file = AnsiConsole.Ask<string>("YML [green]file[/]: ");
                p = AnsiConsole.Ask<int>("No: of parallel [green]requests[/]: ");
                i = AnsiConsole.Ask<int>("No: of [green]iterations[/]: ");
                await RunYmlTests($"{file}.yml", p, i);
                break;
            case "2. List YML":
                string[] yamlFiles =  Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yml")
                .Select(Path.GetFileName)
                .ToArray();
                file = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title($"YML files in: [blue]{Directory.GetCurrentDirectory()}[/]")
                        .PageSize(10)
                        .MoreChoicesText("[grey](Move up and down to select opptions)[/]")
                        .AddChoices(yamlFiles)
                        );
                p = AnsiConsole.Ask<int>("No: of parallel [green]requests[/]: ");
                i = AnsiConsole.Ask<int>("No: of [green]iterations[/]: ");
                await RunYmlTests($"{file}", p, i);
                break;
            case "3. Create YML":
                file = AnsiConsole.Ask<string>("YML [green]file[/]: ");
                var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
                var yaml = serializer.Serialize(new RequestInfo());
                File.WriteAllText($"{file}.yml", yaml);
                AnsiConsole.Write(new Markup($"Successfully created file '[blue]{file}.yml[/]'"));
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine();
                Process.Start("notepad.exe", $"{file}.yml");
                goto restart;
            default:
                goto restart;
        }
    }
    else
    {
        await RunYmlTests(file, p, i);
    }

    async Task RunYmlTests(string? file, int? p, int? i)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var requestInfo = deserializer.Deserialize<RequestInfo>(File.ReadAllText(file));
        requestInfo.Parallelism = p.Value;
        requestInfo.Iterations = i.Value;

        await AnsiConsole.Status()
            .StartAsync($"Reading [blue]{file}[/] ...", async ctx =>
            {
                DrawRequestInfo(requestInfo, file);
                Thread.Sleep(1000);

                ctx.Status("Starting tests...");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                Thread.Sleep(1000);
            });


        await AnsiConsole.Progress()
            .StartAsync((Func<ProgressContext, Task>)(async ctx =>
            {
                var task1 = ctx.AddTask("[green]Progress[/]", new ProgressTaskSettings
                {
                    MaxValue = p.Value * i.Value
                });

                while (!ctx.IsFinished)
                {
                    await RunTest(requestInfo, () =>
                    {
                        task1.Increment(1);
                    });
                }
            }));
    }
});


static void DrawIterationTree(Dictionary<int, List<TestResult>> testResults)
{
    var root = new Tree("[yellow2]Test Results[/]");
    for (var i = 0; i < testResults.Count; i++)
    {
        var foo = root.AddNode($"[mediumspringgreen]{(i + 1).Ordinalize()}[/]");
        var table = foo.AddNode(DrawResponseTable(testResults[i]));
    }
    AnsiConsole.Write(root);
}

static IRenderable DrawResponseTable(List<TestResult> testResults)
{
    var table = new Table();

    table.AddColumn(new TableColumn("Thread").Centered());
    table.AddColumn(new TableColumn("HTTP Status").Centered());
    table.AddColumn(new TableColumn("Response Time").Centered());
    table.AddColumn(new TableColumn("Requested Time").Centered());

    foreach (var response in testResults)
    {
        var httpStatus = "";
        var responseTime = "";

        if (response.HTTPStatus == 200)
        {
            httpStatus = $"[mediumspringgreen]{response.HTTPStatus}[/]";
        }
        else
        {
            httpStatus = $"[red]{response.HTTPStatus}[/]";
        }

        if (response.ResponseTime < 500)
        {
            responseTime = $"[mediumspringgreen]{response.ResponseTime}[/]";
        }
        else if (response.ResponseTime < 1000)
        {
            responseTime = $"[yellow2]{response.ResponseTime}[/]";
        }
        else
        {
            responseTime = $"[red]{response.ResponseTime}[/]";
        }

        table.AddRow(
            $"[grey]{response.Iteration}[/]",
            $"{httpStatus} [mediumpurple4]{(HttpStatusCode)response.HTTPStatus}[/]",
            $"{responseTime} ms",
            $"{response.StartTime.ToString("h:mm:ss tt")} - {response.EndTime.ToString("h:mm:ss tt")}"
            );
    }

    var panel = new Panel(table);
    panel.Width = 80;
    panel.Header = new PanelHeader("[yellow2]Perfomance Details[/]");
    return table;
}

static void DrawSummaryTable(Dictionary<int, List<TestResult>> responses)
{
    var min = responses.Values.SelectMany(x => x).Min(x => x.ResponseTime);
    var avg = responses.Values.SelectMany(x => x).Average(x => x.ResponseTime);
    var max = responses.Values.SelectMany(x => x).Max(x => x.ResponseTime);

    var barChart = new BarChart()
    .Width(60)
    .AddItem("Minimum", min, Color.Grey63)
    .AddItem("Average", avg, Color.Red)
    .AddItem("Maximum", max, Color.Grey58);

    var panel = new Panel(barChart);
    panel.Width = 80;
    panel.Header = new PanelHeader("[yellow2]Perfomance Summary[/]");
    AnsiConsole.Write(panel);
}

void DrawRequestInfo(RequestInfo requestInfo, string filename)
{
    // Create a list of Items
    AnsiConsole.WriteLine("");
    var rows = new List<Markup>(){
        new Markup($"File             : [yellow2]{filename}[/]"),
        new Markup($"Parallel Calls   : [blue]{requestInfo.Parallelism}[/]"),
        new Markup($"Iterations       : [blue]{requestInfo.Iterations}[/]"),
        new Markup($"Total Requests   : [blue]{requestInfo.Parallelism * requestInfo.Iterations}[/] [grey]({requestInfo.Parallelism} parallel calls [white]x[/] {requestInfo.Iterations} times)[/]"),
        new Markup($"Env              : [yellow2]{requestInfo.Env}[/]"),
        new Markup($"URL              : [yellow2]{requestInfo.Url}[/]"),
    };

    var panel = new Panel(new Rows(rows));
    panel.Width = 80;
    panel.Header = new PanelHeader("[yellow2]API Summary[/]");
    AnsiConsole.Write(panel);
}

async Task<int> RunTest(RequestInfo requestInfo, Action callback)
{
    var url = requestInfo.Url;

    var testResults = new Dictionary<int, List<TestResult>>();

    for (var i = 0; i < requestInfo.Iterations; i++)
    {
        var testResult = await Engine.SendParallelRequestsAsync(url, requestInfo.Parallelism, callback);
        testResults.Add(i, testResult);
    }

    DrawSummaryTable(testResults);
    DrawIterationTree(testResults);
    return 0;
}