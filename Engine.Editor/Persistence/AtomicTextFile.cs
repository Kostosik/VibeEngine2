using System.Text;

namespace Engine.Editor.Persistence;

internal static class AtomicTextFile
{
    public static void WriteAllText(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(contents);

        var fullPath = Path.GetFullPath(path);

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException(
                "Cannot determine the destination directory.");

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            // Сначала полностью записываем новое содержимое
            // в отдельный файл на том же диске.
            File.WriteAllText(
                temporaryPath,
                contents,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            // Заменяем целевой файл только после успешной записи.
            File.Move(
                temporaryPath,
                fullPath,
                overwrite: true);
        }
        finally
        {
            // Удаляем временный файл, если замена не удалась.
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
                // Не маскируем исходную ошибку сохранения.
            }
        }
    }
}