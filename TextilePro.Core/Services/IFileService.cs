namespace TextilePro.Core.Services;

public interface IFileService
{
    string SaveFile(byte[] fileData, string originalFileName, string subFolder = "Documents");
    void DeleteFile(string filePath);
    string GetBaseDirectory();
}
