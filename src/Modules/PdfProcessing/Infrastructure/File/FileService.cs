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
    private readonly Dictionary<string, System.Timers.Timer> _cleanupTimers;
    private readonly object _cleanupTimersLock = new();

    public FileService(IOptions<FileServiceOptions> options)
    {
        _baseOutputDir = options.Value.OutputDir;
        _baseUploadDir = options.Value.UploadDir;
        _cleanupTimers = new Dictionary<string, System.Timers.Timer>();

        EnsureDirectoryExists(_baseOutputDir);
        EnsureDirectoryExists(_baseUploadDir);
    }

    public string GetUserFile(string userId, string userSessionId, string? fileName = null)
    {
        var userOutputDir = GetUserOutputDir(userId, userSessionId);
        EnsureDirectoryExists(userOutputDir);

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var safeFileName = Path.GetFileName(fileName);
            var requestedPath = Path.Combine(userOutputDir, safeFileName);
            if (!System.IO.File.Exists(requestedPath))
            {
                throw new FileNotFoundException($"File not found: {safeFileName}");
            }

            return requestedPath;
        }

        var preferredFiles = new[] { "EXTRATO.csv", "PGTO.csv" };
        foreach (var preferred in preferredFiles)
        {
            var preferredPath = Path.Combine(userOutputDir, preferred);
            if (System.IO.File.Exists(preferredPath))
            {
                return preferredPath;
            }
        }

        var files = Directory.GetFiles(userOutputDir);
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No files available for download");
        }

        return files.OrderByDescending(System.IO.File.GetLastWriteTimeUtc).First();
    }

    public void ClearUserFiles(string userId, string userSessionId)
    {
        var userOutputDir = GetUserOutputDir(userId, userSessionId);
        var userUploadDir = GetUserUploadDir(userId, userSessionId);
        var timerKey = BuildTimerKey(userId, userSessionId);

        ClearDirectory(userOutputDir);
        ClearDirectory(userUploadDir);

        TryRemoveDirectory(userOutputDir);
        TryRemoveDirectory(userUploadDir);
        TryRemoveEmptyUserDirectory(userId);

        RemoveCleanupTimer(timerKey);
    }

    public Task ScheduleCleanup(string userId, string userSessionId, TimeSpan delay)
    {
        var timerKey = BuildTimerKey(userId, userSessionId);

        lock (_cleanupTimersLock)
        {
            RemoveCleanupTimer(timerKey);

            var timer = new System.Timers.Timer(delay.TotalMilliseconds)
            {
                AutoReset = false
            };

            timer.Elapsed += (_, _) =>
            {
                try
                {
                    ClearUserFiles(userId, userSessionId);
                }
                finally
                {
                    lock (_cleanupTimersLock)
                    {
                        RemoveCleanupTimer(timerKey);
                    }
                }
            };

            _cleanupTimers[timerKey] = timer;
            timer.Start();
        }

        return Task.CompletedTask;
    }

    private void RemoveCleanupTimer(string timerKey)
    {
        if (_cleanupTimers.TryGetValue(timerKey, out var timer))
        {
            timer.Stop();
            timer.Dispose();
            _cleanupTimers.Remove(timerKey);
        }
    }

    public string SaveUserFile(IFormFile file, string userId, string userSessionId)
    {
        var userUploadDir = GetUserUploadDir(userId, userSessionId);
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

    public string GetUserOutputDir(string userId, string userSessionId)
        => BuildScopedPath(_baseOutputDir, userId, userSessionId);

    public string GetUserUploadDir(string userId, string userSessionId)
        => BuildScopedPath(_baseUploadDir, userId, userSessionId);

    private static string BuildScopedPath(string baseDir, string userId, string userSessionId)
    {
        var safeUserId = SanitizePathSegment(userId);
        var safeSessionId = SanitizePathSegment(userSessionId);
        return Path.Combine(baseDir, safeUserId, safeSessionId);
    }

    private static string SanitizePathSegment(string value)
    {
        var sanitized = value.Replace("..", string.Empty)
            .Replace("/", string.Empty)
            .Replace("\\", string.Empty)
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new InvalidDataException("Identificador inválido.");
        }

        return sanitized;
    }

    private static string BuildTimerKey(string userId, string userSessionId)
        => $"{userId}::{userSessionId}";

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void ClearDirectory(string directoryPath)
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

    private void TryRemoveEmptyUserDirectory(string userId)
    {
        try
        {
            var userDir = Path.Combine(_baseOutputDir, SanitizePathSegment(userId));
            if (Directory.Exists(userDir) &&
                !Directory.GetFiles(userDir, "*", SearchOption.AllDirectories).Any())
            {
                Directory.Delete(userDir, recursive: true);
            }
        }
        catch
        {
        }
    }
}
