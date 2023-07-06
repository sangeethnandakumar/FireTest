using Spectre.Console;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Humanizer;
using Spectre.Console.Rendering;
using System.Net;

namespace FireTest
{
    public static class YMLUtility
    {
        public static string Serialize<T>(T instance)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
           return serializer.Serialize(instance);
        }

        public static T Deserialize<T>(string ymlData)
        {
            var deserializer = new DeserializerBuilder()
               .WithNamingConvention(UnderscoredNamingConvention.Instance)
               .Build();
            return deserializer.Deserialize<T>(ymlData);
        }
    }

    public static class Utilities
    {
        public static RunCommandArgs MakeTestData(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    var ymlData = File.ReadAllText(fileName);
                    var runCommandArgs = YMLUtility.Deserialize<RunCommandArgs>(ymlData);
                    return runCommandArgs;
                }
                AnsiConsole.Write(new Markup($"Unable to find YML file with name '[red1]{fileName}[/]' in '[blue]{Directory.GetCurrentDirectory()}[/]'"));
                AnsiConsole.WriteLine();
                Environment.Exit(1);
            }
            catch (Exception)
            {
                AnsiConsole.Write(new Markup($"An unexpected error occured while reading '[red1]{fileName}[/]' in '[blue]{Directory.GetCurrentDirectory()}[/]'"));
                AnsiConsole.WriteLine();
                Environment.Exit(1);
            }
            return null;
        }
    }

    public static class CommandDriver
    {
        public static void RunAsync(string fileName, RunCommandArgs runArgs)
        {
            //Get YML name
            try
            {
               



            AnsiConsole.Status()
            .Start($"Reading [blue]{fileName}[/] ...",  ctx =>
            {
                AnsiConsole.WriteLine("");
                var rows = new List<Markup>()
                {
                    new Markup($"File             : [yellow2]{fileName}[/]"),
                    new Markup($"Parallel Calls   : [blue]{runArgs.Parallelism}[/]"),
                    new Markup($"Iterations       : [blue]{runArgs.Iterations}[/]"),
                    new Markup($"Total Requests   : [blue]{runArgs.Parallelism * runArgs.Iterations}[/] [grey]({runArgs.Parallelism} parallel calls [white]x[/] {runArgs.Iterations} times)[/]"),
                    new Markup($"Env              : [yellow2]{runArgs.Env}[/]"),
                    new Markup($"URL              : [yellow2]{runArgs.Url}[/]"),
                };

                var panel = new Panel(new Rows(rows));
                panel.Width = 80;
                panel.Header = new PanelHeader("[yellow2]API Summary[/]");
                AnsiConsole.Write(panel);


                Thread.Sleep(1000);
                ctx.Status("Starting tests...");
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                Thread.Sleep(1000);
            });


            AnsiConsole.Progress()
                    .Start((Action<ProgressContext>)(ctx =>
                    {
                        var task1 = ctx.AddTask("[green]Progress[/]", new ProgressTaskSettings
                        {
                            MaxValue = runArgs.Parallelism * runArgs.Iterations
                        });

                        while (!ctx.IsFinished)
                        {
                            RunTest(runArgs, () =>
                            {
                                task1.Increment(1);
                            });
                        }
                    }));

            }
            catch (Exception)
            {
                AnsiConsole.Write(new Markup($"An unexpected error occured while creating YML file '[red1]{fileName}[/]' in '[blue]{Directory.GetCurrentDirectory()}[/]'. Check if you have enough security privilages for file write in this directory."));
                AnsiConsole.WriteLine();
                Environment.Exit(1);
            }
        }
        private static void RunTest(RunCommandArgs requestInfo, Action callback)
        {
            var url = requestInfo.Url;

            var testResults = new Dictionary<int, List<TestResult>>();

            for (var i = 0; i < requestInfo.Iterations; i++)
            {
                var testResult = Engine.SendParallelRequestsAsync(url, requestInfo.Parallelism, callback).Result;
                testResults.Add(i, testResult);
            }

            DrawSummaryTable(testResults);
            DrawIterationTree(testResults);
        }
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




        public static string ListCommand()
        {
            try
            {
                //Get all YML files
                var allYmlFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yml")
                                    .Select(Path.GetFileName)
                                    .ToList();

                if(allYmlFiles.Any())
                {
                    allYmlFiles.Add("(Back to menu)");
                    //Ask user to select one
                    var yml = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                           .Title($"YML files in: [blue]{Directory.GetCurrentDirectory()}[/]")
                                           .PageSize(10)
                                           .MoreChoicesText("[grey](Move up and down to select opptions)[/]")
                                           .AddChoices(allYmlFiles)
                                           );
                    return yml;
                }
                else
                {
                    AnsiConsole.Write(new Markup($"There are no YML files in '[blue]{Directory.GetCurrentDirectory()}[/]'. Add a temlplate using create command."));
                    AnsiConsole.WriteLine();
                    Environment.Exit(1);
                }
            }
            catch (Exception)
            {
                AnsiConsole.Write(new Markup($"An unexpected error occured while fetching list of YML files from '[blue]{Directory.GetCurrentDirectory()}[/]'. Check if you have enough security privilages for file read in this directory."));
                AnsiConsole.WriteLine();
                Environment.Exit(1);
            }
            return null;
        }

        public static void CreateCommand()
        {
            //Get YML name
            var file = AnsiConsole.Ask<string>("YML [green]file[/]: ");
            try
            {
                //Write template YML
                File.WriteAllText($"{file}.yml", YMLUtility.Serialize(new RunCommandArgs()));
            }
            catch (Exception)
            {
                AnsiConsole.Write(new Markup($"An unexpected error occured while creating YML file '[red1]{file}[/]' in '[blue]{Directory.GetCurrentDirectory()}[/]'. Check if you have enough security privilages for file write in this directory."));
                AnsiConsole.WriteLine();
                Environment.Exit(1);
            }
        }
    }
}
