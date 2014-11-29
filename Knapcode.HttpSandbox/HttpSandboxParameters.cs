using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Knapcode.HttpSandbox
{
    public class HttpSandboxParameters
    {
        private const string EncodingKey = "encoding";
        private const string ChunkedKey = "chunked";
        private const string LinesKey = "lines";
        private const string DeflateLevelKey = "deflateLevel";
        private const string SleepKey = "sleep";

        private static readonly ISet<string> Encodings = new HashSet<string>(new[] { "gzip", "identity", "zlib", "deflate" });

        private HttpSandboxParameters()
        {
            Encoding = "identity";
            Chunked = false;
            Lines = 25;
            DeflateLevel = 6;
            Sleep = 100;
        }

        public string Encoding { get; private set; }

        public bool Chunked { get; private set; }

        public int Lines { get; private set; }

        public int DeflateLevel { get; private set; }

        public int Sleep { get; private set; }

        public IEnumerable<string> GetLines()
        {
            NameValueCollection resolvedQuery = HttpUtility.ParseQueryString(string.Empty);
            resolvedQuery[EncodingKey] = Encoding;
            resolvedQuery[ChunkedKey] = Chunked.ToString().ToLower();
            resolvedQuery[LinesKey] = Lines.ToString(CultureInfo.InvariantCulture);
            resolvedQuery[DeflateLevelKey] = DeflateLevel.ToString(CultureInfo.InvariantCulture);
            resolvedQuery[SleepKey] = Sleep.ToString(CultureInfo.InvariantCulture);

            return new[]
            {
                string.Format(CultureInfo.InvariantCulture, "Optional query string fields:"),
                string.Format(CultureInfo.InvariantCulture, "  '{0}'      The type of compression to use. Must be in ['{1}'].", EncodingKey, string.Join("', '", Encodings)),
                string.Format(CultureInfo.InvariantCulture, "  '{0}'       Whether to use Transfer-Encoding: chunked. must be 'true' or 'false'.", ChunkedKey),
                string.Format(CultureInfo.InvariantCulture, "  '{0}'         The number of lines to return. Must be an integer greater than or equal to 0.", LinesKey),
                string.Format(CultureInfo.InvariantCulture, "  '{0}'  The compression level for DEFLATE. Must be an integer between 0 and 9 (inclusive).", DeflateLevelKey),
                string.Format(CultureInfo.InvariantCulture, "  '{0}'         The milliseconds between returning each line. Must be an integer greater than or equal to 0.", SleepKey),
                string.Format(CultureInfo.InvariantCulture, "Resolved query string:"),
                string.Format(CultureInfo.InvariantCulture, "  {0}", resolvedQuery)
            };
        }

        public static HttpSandboxParameters Parse(string queryString)
        {
            NameValueCollection query = HttpUtility.ParseQueryString(queryString);

            var output = new HttpSandboxParameters();

            // encoding
            string encoding = (query[EncodingKey] ?? string.Empty).Trim().ToLower();
            if (!Encodings.Contains(encoding))
            {
                encoding = output.Encoding;
            }
            output.Encoding = encoding;

            // chunked
            bool chunked;
            if (!bool.TryParse(query[ChunkedKey], out chunked))
            {
                chunked = output.Chunked;
            }
            output.Chunked = chunked;

            // iterations
            int lines;
            if (!int.TryParse(query[LinesKey], out lines))
            {
                lines = output.Lines;
            }
            else
            {
                if (lines < 0)
                {
                    lines = 0;
                }
            }
            output.Lines = lines;

            // deflate compression level
            int deflateLevel;
            if (!int.TryParse(query[DeflateLevelKey], out deflateLevel))
            {
                deflateLevel = output.DeflateLevel;
            }
            else
            {
                if (deflateLevel < 0)
                {
                    deflateLevel = 0;
                }
                else if (deflateLevel > 9)
                {
                    deflateLevel = 9;
                }
            }
            output.DeflateLevel = deflateLevel;

            // sleep milliseconds
            int sleep;
            if (!int.TryParse(query[SleepKey], out sleep))
            {
                sleep = output.Sleep;
            }
            else
            {
                if (sleep < 0)
                {
                    sleep = 0;
                }
            }
            output.Sleep = sleep;

            return output;
        }
    }
}