using Rhino;
using System.IO;
namespace RhinoContextAssist;
internal static class Diagnostics { internal static string LogPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RhinoContextAssist","ContextAssist.log"); internal static void Log(string text){try{Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);File.AppendAllText(LogPath,$"{DateTime.Now:O} | Rhino {RhinoApp.Version} | {text}{Environment.NewLine}");}catch{}} }
