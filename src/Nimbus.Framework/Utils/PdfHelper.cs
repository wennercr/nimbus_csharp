using UglyToad.PdfPig;

namespace Nimbus.Framework.Utils
{
    /// <summary>
    /// PdfHelper is a utility for extracting text from PDF files.
    /// It supports both local files and in-memory byte[] content.
    /// Requires the PdfPig NuGet package.
    /// </summary>
    public class PdfHelper
    {
        /// <summary>
        /// Extracts all text from a local PDF file.
        /// </summary>
        /// <param name="file">The PDF file on disk</param>
        /// <returns>The extracted text as a string</returns>
        public string Read(FileInfo file)
        {
            using (PdfDocument document = PdfDocument.Open(file.FullName))
            {
                return string.Join(Environment.NewLine,
                    document.GetPages().Select(p => p.Text));
            }
        }

        /// <summary>
        /// Reads an in-memory PDF (byte array) and returns the extracted text.
        /// </summary>
        /// <param name="data">The byte[] PDF content</param>
        public string Read(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("PDF byte array is empty.");

            using (var stream = new MemoryStream(data))
            using (PdfDocument document = PdfDocument.Open(stream))
            {
                return string.Join(Environment.NewLine,
                    document.GetPages().Select(p => p.Text)).Trim();
            }
        }

        /// <summary>
        /// Checks whether a local PDF file contains the expected text.
        /// </summary>
        public bool DoesPdfContainText(FileInfo file, string expectedText)
        {
            return Read(file).Contains(expectedText);
        }

        /// <summary>
        /// Checks whether the in-memory PDF content contains the expected text.
        /// </summary>
        public bool DoesPdfContainText(byte[] content, string expectedText)
        {
            return Read(content).Contains(expectedText);
        }
    }
}
