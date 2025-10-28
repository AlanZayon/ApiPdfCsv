using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;

namespace ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;

public class FileService : IFileService
{
    private readonly string _baseOutputDir;
    private readonly string _baseUploadDir;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, System.Timers.Timer> _cleanupTimers;

    public FileService(IOptions<FileServiceOptions> options, IHttpContextAccessor httpContextAccessor)
    {
        _baseOutputDir = options.Value.OutputDir;
        _baseUploadDir = options.Value.UploadDir;
        _httpContextAccessor = httpContextAccessor;
        _cleanupTimers = new Dictionary<string, System.Timers.Timer>();

        EnsureDirectoryExists(_baseOutputDir);
        EnsureDirectoryExists(_baseUploadDir);
    }

    public string GetUserFile(string userSessionId)
    {
        var userOutputDir = GetUserOutputDir(userSessionId);
        EnsureDirectoryExists(userOutputDir);

        var files = Directory.GetFiles(userOutputDir);
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No files available for download");
        }

        return files[0];
    }

    public void ClearUserFiles(string userSessionId)
    {
        var userOutputDir = GetUserOutputDir(userSessionId);
        var userUploadDir = GetUserUploadDir(userSessionId);

        ClearDirectory(userOutputDir);
        ClearDirectory(userUploadDir);

        TryRemoveDirectory(userOutputDir);
        TryRemoveDirectory(userUploadDir);

        RemoveCleanupTimer(userSessionId);
    }

    public async Task ScheduleCleanup(string userSessionId, TimeSpan delay)
    {
        await Task.Delay(delay);
        ClearUserFiles(userSessionId);
    }

    private void RemoveCleanupTimer(string userSessionId)
    {
        if (_cleanupTimers.TryGetValue(userSessionId, out var timer))
        {
            timer.Stop();
            timer.Dispose();
            _cleanupTimers.Remove(userSessionId);
        }
    }

    public string SaveUserFile(IFormFile file, string userSessionId)
    {
        var userUploadDir = GetUserUploadDir(userSessionId);
        EnsureDirectoryExists(userUploadDir);

        ClearDirectory(userUploadDir);

        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileNameWithoutExtension(file.FileName)}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(userUploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return filePath;
    }

    public string GetUserOutputDir(string userSessionId)
    {
        return Path.Combine(_baseOutputDir, userSessionId);
    }

    public string GetUserUploadDir(string userSessionId)
    {
        return Path.Combine(_baseUploadDir, userSessionId);
    }

    public List<string> GetUserSessions(string userId)
    {
        var userSessions = new List<string>();

        if (Directory.Exists(_baseOutputDir))
        {
            var allOutputDirs = Directory.GetDirectories(_baseOutputDir);
            foreach (var dir in allOutputDirs)
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.StartsWith(userId + "_"))
                {
                    userSessions.Add(dirName);
                }
            }
        }

        return userSessions;
    }

    public void CleanupOldSessions(string userId)
    {
        var userSessions = GetUserSessions(userId);
        var now = DateTime.Now;

        foreach (var session in userSessions)
        {
            try
            {
                var parts = session.Split('_');
                if (parts.Length >= 3 && DateTime.TryParseExact(parts[2], "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var sessionTime))
                {
                    if ((now - sessionTime).TotalHours > 1)
                    {
                        var outputDir = Path.Combine(_baseOutputDir, session);
                        var uploadDir = Path.Combine(_baseUploadDir, session);

                        ClearDirectory(outputDir);
                        ClearDirectory(uploadDir);
                        TryRemoveDirectory(outputDir);
                        TryRemoveDirectory(uploadDir);
                    }
                }
            }
            catch
            {
            }
        }
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private void ClearDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        foreach (var file in Directory.GetFiles(directoryPath))
        {
            try
            {
                System.IO.File.Delete(file);
            }
            catch
            {
            }
        }
    }

    private void TryRemoveDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath) &&
                !Directory.GetFiles(directoryPath).Any() &&
                !Directory.GetDirectories(directoryPath).Any())
            {
                Directory.Delete(directoryPath);
            }
        }
        catch
        {
        }
    }
}