using System.Net.Http.Headers;
using System.Text;

namespace tests.Extensions;

public static class ByteArrayExtensions
{
    public static bool IsSqlite(this byte[]? bytes) => 
        bytes != null && bytes.Length >= 16 ? 
            Encoding.ASCII.GetString(bytes, 0, 16) == "SQLite format 3\0"
            : false;

    public static ByteArrayContent? ToByteArrayContent(this byte[]? bytes) {
        ByteArrayContent? byteArrayContent = null;

        if (bytes != null) {
            byteArrayContent = new ByteArrayContent(bytes);
            byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        }

        return byteArrayContent;
    }
}