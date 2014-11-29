using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Knapcode.HttpSandbox
{
    public class HttpSandboxHandler : IHttpHandler
    {
        private const string Line = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Lorem ipsum dolor sit amet. Pellentesque tincidunt ligula sed magna semper.";

        public void ProcessRequest(HttpContext context)
        {
            // prepare the headers
            context.Response.Buffer = false;
            context.Response.AddHeader("Content-Type", "text/plain");

            // parse the parameters
            HttpSandboxParameters parameters = HttpSandboxParameters.Parse(context.Request.QueryString.ToString());

            // build the lines
            string[] parameterLines = parameters.GetLines().ToArray();
            byte[][] lines = Enumerable.Empty<string>()
                .Concat(parameterLines)
                .Concat(Enumerable.Repeat(Line, Math.Max(0, parameters.Lines - parameterLines.Length)))
                .Take(parameters.Lines)
                .Select(l => Encoding.UTF8.GetBytes(l + "\r\n"))
                .ToArray();

            // calculate the content length, if needed
            Stream responseStream;
            if (!parameters.Chunked)
            {
                var outputStream = new MemoryStream();
                Stream encodingStream = AddContentEncodingAndGetStream(parameters, context.Response, outputStream);
                responseStream = context.Response.OutputStream;

                int offset = 0;
                using (encodingStream)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        encodingStream.Write(lines[i], 0, lines[i].Length);
                        encodingStream.Flush();

                        int newLength = (int) outputStream.Length - offset;
                        var newLine = new byte[newLength];
                        Buffer.BlockCopy(outputStream.GetBuffer(), offset, newLine, 0, newLength);
                        offset = (int) outputStream.Length;

                        lines[i] = newLine;
                    }
                }

                context.Response.AddHeader("Content-Length", offset.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                responseStream = AddContentEncodingAndGetStream(parameters, context.Response, context.Response.OutputStream);
            }

            // write the lines
            foreach (var line in lines)
            {
                responseStream.Write(line, 0, line.Length);
                Thread.Sleep(parameters.Sleep);
                responseStream.Flush();
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }

        private static Stream AddContentEncodingAndGetStream(HttpSandboxParameters httpSandboxParameters, HttpResponse headers, Stream sourceStream)
        {
            switch (httpSandboxParameters.Encoding)
            {
                case "gzip":
                    headers.AddHeader("Content-Encoding", "gzip");
                    return new GZipOutputStream(sourceStream);
                case "deflate":
                    headers.AddHeader("Content-Encoding", "deflate");
                    return new DeflaterOutputStream(sourceStream, new Deflater(httpSandboxParameters.DeflateLevel, true));
                case "zlib":
                    headers.AddHeader("Content-Encoding", "deflate");
                    return new DeflaterOutputStream(sourceStream, new Deflater(httpSandboxParameters.DeflateLevel, false));
                default:
                    return sourceStream;
            }
        }
    }
}