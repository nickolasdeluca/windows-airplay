using WindowsAirplay.Core.Packaging;
using Xunit;
namespace WindowsAirplay.Tests.Packaging;
public sealed class RuntimeLayoutTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"airplay payload "+Guid.NewGuid());
    [Fact] public void MissingRuntimeIsRejected() => Assert.Throws<FileNotFoundException>(()=>RuntimeLayout.Validate(directory));
    [Theory] [InlineData(0x014c)] [InlineData(0xaa64)]
    public void WrongArchitectureIsRejected(int machine) { Directory.CreateDirectory(directory); var file=Path.Combine(directory,"native.exe"); var data=new byte[128];data[0]=77;data[1]=90;BitConverter.GetBytes(64).CopyTo(data,60); data[64]=80;data[65]=69;BitConverter.GetBytes((ushort)machine).CopyTo(data,68);File.WriteAllBytes(file,data); Assert.Throws<InvalidDataException>(()=>RuntimeLayout.ValidateX64(file)); }
    [Fact] public void ForgedMachineWithoutPeSignatureIsRejected() {Directory.CreateDirectory(directory);var file=Path.Combine(directory,"native.exe");var data=new byte[128];BitConverter.GetBytes(64).CopyTo(data,60);BitConverter.GetBytes((ushort)0x8664).CopyTo(data,68);File.WriteAllBytes(file,data);Assert.Throws<InvalidDataException>(()=>RuntimeLayout.ValidateX64(file));}
    public void Dispose() {if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
