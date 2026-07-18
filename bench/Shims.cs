// Minimal stand-ins so LadderStandings compiles without Unity ScriptableObjects.
public class OpponentModelDefinition { public string DisplayName; }
public static class OpponentModelCatalog { public static OpponentModelDefinition GetById(string id) => null; }

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object m) => System.Console.WriteLine(m);
        public static void LogWarning(object m) => System.Console.WriteLine("WARN: " + m);
        public static void LogError(object m) => System.Console.Error.WriteLine("ERROR: " + m);
    }

    public sealed class TextAsset
    {
        public string name { get; }
        public string text { get; }
        public byte[] bytes { get; }
        public TextAsset(string name, string text, byte[] bytes)
        { this.name = name; this.text = text; this.bytes = bytes; }
    }

    // Reads TextAssets from the real Assets/Resources folder on disk.
    public static class Resources
    {
        public static string Root = "../Assets/Resources";

        public static T Load<T>(string path) where T : class
        {
            string full = Resolve(System.IO.Path.Combine(Root, path));
            if (full == null) return null;
            byte[] b = System.IO.File.ReadAllBytes(full);
            return (T)(object)new TextAsset(
                System.IO.Path.GetFileNameWithoutExtension(full),
                System.Text.Encoding.UTF8.GetString(b), b);
        }

        public static T[] LoadAll<T>(string path) where T : class
        {
            string dir = System.IO.Path.Combine(Root, path);
            var list = new System.Collections.Generic.List<T>();
            if (System.IO.Directory.Exists(dir))
            {
                foreach (var f in System.IO.Directory.GetFiles(dir))
                {
                    if (f.EndsWith(".meta")) continue;
                    byte[] b = System.IO.File.ReadAllBytes(f);
                    list.Add((T)(object)new TextAsset(
                        System.IO.Path.GetFileNameWithoutExtension(f),
                        System.Text.Encoding.UTF8.GetString(b), b));
                }
            }
            else { var one = Load<T>(path); if (one != null) list.Add(one); }
            return list.ToArray();
        }

        private static string Resolve(string noExt)
        {
            if (System.IO.File.Exists(noExt)) return noExt;
            foreach (var e in new[] { ".json", ".txt", ".bytes", ".asset" })
                if (System.IO.File.Exists(noExt + e)) return noExt + e;
            return null;
        }
    }
}