using System.Text;

namespace webapi.Extensions;

public static class FileExtensions
{
    public static byte[]? FileBytes(this IFormFile? file, int len = 1)
    {
        byte[]? ret = null;

        if (file != null && len > 0 && file.Length >= len) {
            ret = new byte[len];

            using (BinaryReader fs = new (file.OpenReadStream())) {
                fs.Read(ret, 0, len-1);
            }
        }
        
        return ret;
    }

    public static string? FileBytesAsString(this IFormFile? file, int len = 1) {
        string? ret = null;

        byte[]? bytes = file.FileBytes(len);

        if (bytes != null)
            ret = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

        return ret;
    }

    public static bool IsSqlite(this IFormFile? file) => file.FileBytesAsString(16) == "SQLite format 3\0";
}