
using BuildTools.Core.Runtime;

return await RunAsync(args);

static async Task<int> RunAsync(string[] arguments)
{
    if (arguments.Length == 0 ||
        arguments[0] is "--help" or "-h" or "help")
    {
        PrintUsage();
        return 0;
    }

    if (!string.Equals(
            arguments[0],
            "runtime",
            StringComparison.OrdinalIgnoreCase))
    {
        PrintUsage();
        return 2;
    }

    try
    {
        return await RunRuntimeCommandAsync(arguments);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        return 1;
    }
}

static async Task<int> RunRuntimeCommandAsync(
    string[] arguments)
{
    if (arguments.Length < 2)
    {
        PrintUsage();
        return 2;
    }

    switch (arguments[1].ToLowerInvariant())
    {
        case "build":
            {
                if (arguments.Length is < 4 or > 5)
                {
                    PrintUsage();
                    return 2;
                }

                var configuration =
                    arguments.Length == 5
                        ? arguments[4]
                        : "Debug";

                await EngineRuntimeBuilder.BuildAsync(
                    arguments[2],
                    arguments[3],
                    configuration,
                    onOutput: static (message, isError) =>
                    {
                        if (isError)
                        {
                            Console.Error.WriteLine(message);
                        }
                        else
                        {
                            Console.WriteLine(message);
                        }
                    });

                return 0;
            }

        case "verify":
            {
                if (arguments.Length != 3)
                {
                    PrintUsage();
                    return 2;
                }

                var directory = Path.GetFullPath(arguments[2]);

                if (!Directory.Exists(directory))
                {
                    Console.Error.WriteLine(
                        $"Runtime directory does not exist: {directory}");

                    return 1;
                }

                var missing =
                    EngineRuntimeBuilder.GetMissingAssemblies(directory);

                if (missing.Length > 0)
                {
                    Console.Error.WriteLine(
                        "Runtime is incomplete. Missing assemblies:");

                    foreach (var assembly in missing)
                    {
                        Console.Error.WriteLine($"  {assembly}");
                    }

                    return 1;
                }

                Console.WriteLine(
                    $"Runtime validation passed: {directory}");

                return 0;
            }

        default:
            PrintUsage();
            return 2;
    }
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        VibeEngine2 BuildTools

        Commands:

          runtime build <sourceRoot> <outputDirectory> [Debug|Release]
          runtime verify <runtimeDirectory>

        Examples:

          BuildTools.CLI runtime build . artifacts/engine-runtime/win-x64 Release

          BuildTools.CLI runtime verify artifacts/engine-runtime/win-x64
        """);
}
