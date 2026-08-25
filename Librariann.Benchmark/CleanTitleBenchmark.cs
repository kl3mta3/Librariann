using BenchmarkDotNet.Attributes;
using Librariann.Services.Scanner;

namespace Librariann.Benchmark;

[MemoryDiagnoser]
public class CleanTitleBenchmarks
{
    private static IList<string> _names;

    [GlobalSetup]
    public static void LoadData() => _names = File.ReadAllLines("Data/Comics.txt");

    [Benchmark]
    public static void TestCleanTitle()
    {
        foreach (var name in _names)
        {
            Parser.CleanTitle(name, true);
        }
    }
}
