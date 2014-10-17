using System;
using System.IO;
using System.Text;
using Nancy;

namespace PommaLabs.KVLite.Nancy
{
    /// <summary>
    /// Wraps a regular response in a cached response
    /// The cached response invokes the old response and stores it as a string.
    /// Obviously this only works for ASCII text based responses, so don't use this 
    /// in a real application :-)
    /// </summary>
    public class CachedResponse : Response
    {
        public CachedResponse(Response response)
        {
            string oldResponseOutput;

            ContentType = response.ContentType;
            Headers = response.Headers;
            StatusCode = response.StatusCode;

            using (var memoryStream = new MemoryStream())
            {
                response.Contents.Invoke(memoryStream);
                oldResponseOutput = Encoding.ASCII.GetString(memoryStream.GetBuffer());
            }

            Contents = GetContents(oldResponseOutput);
        }

        protected static Action<Stream> GetContents(string contents)
        {
            return stream =>
            {
                var writer = new StreamWriter(stream) { AutoFlush = true };
                writer.Write(contents);
            };
        }
    }
}