using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kalinda.Extensions
{
    public static class HttpListenerExtensions
    {
        private static string ContentType_TextPlain = "text/plain";
        private static string ContentType_ApplicationJson = "application/json";

        public async static Task<string> ReadInputStringAsync(this HttpListenerRequest request)
        {
            var encoding = request.ContentEncoding;
            var buffer = new byte[request.ContentLength64];
            var bytesToRead = buffer.Length;

            int bytesRead, totalBytesRead = 0;
            do
            {
                bytesRead = await request.InputStream.ReadAsync(buffer, totalBytesRead, bytesToRead - totalBytesRead);
                totalBytesRead += bytesRead;
            } while (bytesRead > 0 && totalBytesRead < bytesToRead);

            if (totalBytesRead == bytesToRead)
            {
                if (encoding != null)
                {
                    return encoding.GetString(buffer);
                }
                else
                {
                    return Encoding.UTF8.GetString(buffer);
                }
            }

            return null;
        }

        public static void WriteOutputString(this HttpListenerResponse response, string entity)
        {
            var byteArray = Encoding.UTF8.GetBytes(entity);
            response.ContentType = ContentType_TextPlain;
            response.ContentLength64 = byteArray.Length;
            response.OutputStream.Write(byteArray, 0, byteArray.Length);
        }

        public async static Task WriteOutputStringAsync(this HttpListenerResponse response, string entity)
        {
            var byteArray = Encoding.UTF8.GetBytes(entity);
            response.ContentType = ContentType_TextPlain;
            response.ContentLength64 = byteArray.Length;
            await response.OutputStream.WriteAsync(byteArray, 0, byteArray.Length);
        }

        public static void WriteJson(this HttpListenerResponse response, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                response.StatusCode = 204; // NoContent
                return;
            }

            var jsonByteArray = Encoding.UTF8.GetBytes(json);

            response.ContentType = ContentType_ApplicationJson;
            response.ContentLength64 = jsonByteArray.Length;
            response.OutputStream.Write(jsonByteArray, 0, jsonByteArray.Length);
        }

        public async static Task WriteJsonAsync(this HttpListenerResponse response, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                response.StatusCode = 204; // NoContent
                return;
            }

            var jsonByteArray = Encoding.UTF8.GetBytes(json);

            response.ContentType = ContentType_ApplicationJson;
            response.ContentLength64 = jsonByteArray.Length;
            await response.OutputStream.WriteAsync(jsonByteArray, 0, jsonByteArray.Length);
        }

        public static void WriteJsonCompressed(this HttpListenerResponse response, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                response.StatusCode = 204; // NoContent
                return;
            }

            var jsonByteArray = Encoding.UTF8.GetBytes(json);

            response.ContentType = ContentType_ApplicationJson;
            response.AddHeader("Content-Encoding", "gzip");

            byte[] byteArray;
            using (var memStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(jsonByteArray, 0, jsonByteArray.Length);
                }

                byteArray = memStream.ToArray();
            }

            response.ContentLength64 = byteArray.Length;
            response.OutputStream.Write(byteArray, 0, byteArray.Length);
        }

        public async static Task WriteJsonCompressedAsync(this HttpListenerResponse response, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                response.StatusCode = 204; // NoContent
                return;
            }

            var jsonByteArray = Encoding.UTF8.GetBytes(json);

            response.ContentType = ContentType_ApplicationJson;
            response.AddHeader("Content-Encoding", "gzip");

            byte[] byteArray;
            using (var memStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(jsonByteArray, 0, jsonByteArray.Length);
                }

                byteArray = memStream.ToArray();
            }

            response.ContentLength64 = byteArray.Length;
            await response.OutputStream.WriteAsync(byteArray, 0, byteArray.Length);
        }
    }
}