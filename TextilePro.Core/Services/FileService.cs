using System.IO;

namespace TextilePro.Core.Services;

public class FileService : IFileService
{
    private readonly string _baseDirectory;

    public FileService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _baseDirectory = Path.Combine(appData, "WinsomeTextile");
        Directory.CreateDirectory(_baseDirectory);
    }

    public string SaveFile(byte[] fileData, string originalFileName, string subFolder = "Documents")
    {
        var folder = Path.Combine(_baseDirectory, subFolder);
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(originalFileName);
        var newFileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(folder, newFileName);

        File.WriteAllBytes(fullPath, fileData);
        return fullPath;
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public string GetBaseDirectory() => _baseDirectory;
}