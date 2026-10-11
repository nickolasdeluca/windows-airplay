namespace WindowsAirplay.Core.Packaging;
public static class RuntimeLayout
{
    public static void Validate(string directory)
    {
        string[] required=["uxplay.exe","gst-inspect-1.0.exe","gst-plugin-scanner.exe","libgstreamer-1.0-0.dll","libglib-2.0-0.dll","libgobject-2.0-0.dll","libssl-3-x64.dll","libcrypto-3-x64.dll","libplist-2.0.dll","plugins/libgstd3d11.dll","plugins/libgstlibav.dll","plugins/libgstvideoparsersbad.dll","manifest.json","packages.txt"];
        foreach(var name in required) if(!File.Exists(Path.Combine(directory,name))) throw new FileNotFoundException("Runtime payload is incomplete: "+name);
        foreach(var file in Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories))
            if(Path.GetExtension(file) is ".exe" or ".dll") ValidateX64(file);
    }
    public static void ValidateX64(string file)
    {
        using var stream=File.OpenRead(file); using var reader=new BinaryReader(stream);
        if(stream.Length<64 || reader.ReadUInt16()!=0x5a4d) throw new InvalidDataException("Invalid Windows executable: "+Path.GetFileName(file));
        stream.Position=60; int offset=reader.ReadInt32();
        if(offset<64 || offset>stream.Length-6) throw new InvalidDataException("Invalid PE header: "+Path.GetFileName(file));
        stream.Position=offset;
        if(reader.ReadUInt32()!=0x00004550 || reader.ReadUInt16()!=0x8664) throw new InvalidDataException("Expected Windows x64: "+Path.GetFileName(file));
    }
}
