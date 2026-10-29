namespace Engine.Tests.Editor;

internal static class ProcessTestCleanup
{
    public static async Task DeleteDirectoryWithRetryAsync(
        string path)
    {
        const int maxAttempts = 20;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
                await Task.Delay(250);
            }
            catch (UnauthorizedAccessException)
                when (attempt < maxAttempts - 1)
            {
                await Task.Delay(250);
            }
        }
    }
}