using pdftron;
using pdftron.Common;
using pdftron.PDF;

using static TextSearchAndRedact.SearchAndRedactHelper;

namespace TextSearchAndRedact
{
    class Program
    {
        public static void Main()
        {
            // Initialize the Apryse PDFNet SDK
            PDFNet.Initialize("YOUR_LICENSE_KEY");

            // Get the base directory of the application
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // Relative path to the folder containing test files
            string input_path = Path.Combine(baseDirectory, "../../../TestFiles/Input/");

            // Check that input directory exists
            if (!Directory.Exists(input_path))
            {
                Console.WriteLine("Input directory does not exist. Exiting.");
                return;
            }

            // Sample code showing how to use high-level text extraction APIs
            try
            {
                // Search items and patterns to be redacted
                List<SearchItem> searchItems =
                [
                    // Plain text
                    new SearchItem(SearchItemType.Text, "Harry Styles"),
                    // Regular expressions (RegEx)
                    new SearchItem(SearchItemType.RegEx, @"\b(?:\+?1[-.\s]?)*\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b"), // Phone numbers
                    new SearchItem(SearchItemType.RegEx, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b"), // Email addresses
                    new SearchItem(SearchItemType.RegEx, @"\b(?:\d[ -]*?){13,16}\b"), // Credit card numbers
                ];

                // Path to the folder where output files will be saved
                string output_path = Path.Combine(baseDirectory, "../../../TestFiles/Output/");
                // Ensure output directory exists
                Directory.CreateDirectory(output_path);

                // Default appearance - Overload 1
                SearchAndRedact(input_path + "sales-invoice-with-credit-cards.pdf", output_path + "redacted.pdf", searchItems);

                // Overlay text - Overload 2
                string overlayText = "REDACTED";
                SearchAndRedact(input_path + "sales-invoice-with-credit-cards.pdf", output_path + "redacted_overlay_text.pdf", searchItems, overlayText);

                // Customize appearance - Overload 3
                Redactor.Appearance appearance = new()
                {
                    RedactionOverlay = true,
                    TextColor = new ColorPt(0, 1, 1), // Cyan in RGB (normalized 0-1)
                    HorizTextAlignment = 0,
                    VertTextAlignment = 0,
                    PositiveOverlayColor = new ColorPt(0, 0, 0), // Black in RGB (normalized 0-1)
                    RedactedContentColor = new ColorPt(0, 0, 0) // Black in RGB (normalized 0-1)
                };
                SearchAndRedact(input_path + "sales-invoice-with-credit-cards.pdf", output_path + "redacted_overlay_text_custom_color.pdf", searchItems, "REDACTED", appearance);
            }
            catch (PDFNetException e)
            {
                Console.WriteLine(e.Message);
            }
            PDFNet.Terminate();
        }
    }
}
