using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Writes a BenchRunSpec to temp JSON and launches the .NET bench runner in a new terminal.</summary>
public static class BenchLauncher
{
    private static string BenchDir => Path.Combine(Path.GetDirectoryName(Application.dataPath), "bench");
    private static string ResourcesRoot => Path.Combine(Application.dataPath, "Resources");

    public static void Launch(BenchRunSpec spec)
    {
        spec.resourcesRoot = ResourcesRoot;
        string cfg = Path.Combine(Path.GetTempPath(), "exec_magica_bench_run.json");
        File.WriteAllText(cfg, Newtonsoft.Json.JsonConvert.SerializeObject(spec, Newtonsoft.Json.Formatting.Indented));

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k dotnet run -c Release --project \"{BenchDir}\" -- \"{cfg}\"",
            UseShellExecute = true,            // opens its own terminal window (survives Unity)
            WorkingDirectory = BenchDir
        };
        Process.Start(psi);
        UnityEngine.Debug.Log($"[Bench] launched {spec.mode} → {cfg}");
    }
}
