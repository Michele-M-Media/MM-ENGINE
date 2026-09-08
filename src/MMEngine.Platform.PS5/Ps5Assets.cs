using MMEngine.Assets;
using MMEngine.Graphics2D;
using SharpProspero.Storage;

namespace MMEngine.Platform.PS5;

public static class Ps5Assets
{
    public static byte[] ReadBytes(string packagePath) => PackageFile.ReadAllBytes(packagePath);
    public static Texture2D LoadPng(string packagePath) => Texture2D.FromPng(ReadBytes(packagePath));
    public static Texture2D LoadTga(string packagePath) => Texture2D.FromTga(ReadBytes(packagePath));
    public static Font2D LoadTrueType(string packagePath, float pixelSize) => Font2D.FromTrueType(ReadBytes(packagePath), pixelSize);
    public static MmMeshAsset LoadMesh(string packagePath) => MmMeshCodec.Decode(ReadBytes(packagePath));
}
