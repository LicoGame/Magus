// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Reflection;
using System.Text;

ConsoleApp.Run<Runner>(args);

public class Runner : ConsoleAppBase
{
    public void CopyToUnity([Option(0)] string directory)
    {
        var replaceSet = new Dictionary<string, string>
        {
            // Remove generics cuz removed by MemoryPack
            { "<ByteBufferWriter>", "" },
            { "new MemoryPackWriter(ref _bufferWriter", "new MemoryPackWriter(ref Unsafe.As<ByteBufferWriter, IBufferWriter<byte>>(ref _bufferWriter)"}
        };
        
        System.Console.WriteLine("Start to modify code.");
        var noBomUtf8 = new UTF8Encoding(false);

        foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path, Encoding.UTF8);

            // replace
            foreach (var item in replaceSet)
            {
                text = text.Replace(item.Key, item.Value);
            }
            File.WriteAllText(path, text, noBomUtf8);
        }
        
        System.Console.WriteLine("Copy complete.");
    }

    public void BundleJsonDll([Option(0)] string projectPath,
        [Option(1)] string tempDirectory,
        [Option(2)] string ilmergePath,
        [Option(3)] string targetDirectory)
    {
        try
        {
            var publishDir = Path.Combine(tempDirectory, "published");
            if (Directory.Exists(publishDir))
            {
                Directory.Delete(publishDir, true);
            }
            Directory.CreateDirectory(publishDir);
            
            var publishProcess = Process.Start("dotnet", $"publish {projectPath} -f netstandard2.1 -o {publishDir}");
            publishProcess.WaitForExit();
            if (publishProcess.ExitCode != 0)
            {
                throw new Exception("dotnet publish failed");
            }
            
            // Gather dlls
            var dlls = Directory.GetFiles(publishDir, "*.dll");
            dlls = dlls.Where(p => !Path.GetFileName(p).StartsWith("Magus")).ToArray();
            
            // Merge dlls
            var mergeDir = Path.Combine(tempDirectory, "merged");
            if (Directory.Exists(mergeDir))
            {
                Directory.Delete(mergeDir, true);
            }
            Directory.CreateDirectory(mergeDir);
            
            var mainDll = Path.Combine(mergeDir, "Magus.Json.dll");
            var sourceDll = Path.Combine(publishDir, "Magus.Json.dll");
            
            var mergeProcess = Process.Start(ilmergePath, $"/ndebug /out:{mainDll} {sourceDll} {string.Join(" ", dlls.Select(d => $"\"{d}\""))}");
            mergeProcess.WaitForExit();
            if (mergeProcess.ExitCode != 0)
            {
                throw new Exception("ilmerge failed");
            }
            
            // Copy
            // Directory.CreateDirectory(targetDirectory);
            // File.Copy(mainDll, Path.Combine(targetDirectory, "Magus.Json.dll"), true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}