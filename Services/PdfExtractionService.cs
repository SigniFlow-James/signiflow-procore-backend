using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

public class PdfExtractionService
{
    public List<Hyperlink> GetLinksFromPdfBytes(byte[] pdfBytes)
    {
        var extractedUrls = new List<Hyperlink>();

        // Open the document directly from the byte array
        using (var document = PdfDocument.Open(pdfBytes))
        {
            foreach (var page in document.GetPages())
            {
                // Specifically extract hyperlink annotations
                var hyperlinks = page.GetHyperlinks();

                foreach (var link in hyperlinks)
                {
                    if (!string.IsNullOrEmpty(link.Uri))
                    {
                        extractedUrls.Add(link);
                    }
                }
            }
        }
        return extractedUrls;
    }
}

