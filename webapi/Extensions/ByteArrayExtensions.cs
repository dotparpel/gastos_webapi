namespace webapi.Extensions;

public static class ByteArrayExtensions
{
    public static byte[] ExtendIfNeeded(this byte[] byteArr, int minLen) {
      byte[] ret = byteArr;

      if (byteArr != null && byteArr.Length < minLen) {
          ret = new byte[minLen]; // zeros by default
          byteArr.CopyTo(ret, 0);
      }

      return ret;
    }
}
