// See https://aka.ms/new-console-template for more information

using System.Text;

ConsoleApp.Run<Runner>(args);

public class Runner : ConsoleAppBase
{
    [RootCommand]
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
}