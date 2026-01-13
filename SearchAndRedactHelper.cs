using pdftron.PDF;
using pdftron.SDF;
using System.Collections;

namespace TextSearchAndRedact
{
    public static class SearchAndRedactHelper
    {
        // Overload 1: Source, Target, Phrases to find
        public static void SearchAndRedact(string sourcePath, string targetPath, IList<SearchItem> searchItems)
        {
            SearchAndRedact(sourcePath, targetPath, searchItems, string.Empty);
        }

        // Overload 2: Source, Target, Phrases to find, Overlay text with set appearance
        public static void SearchAndRedact(string sourcePath, string targetPath, IList<SearchItem> searchItems, string overlayText)
        {
            // Create default options for overlay text
            Redactor.Appearance appearance = new()
            {
                RedactionOverlay = true,
                TextColor = new ColorPt(1, 0, 0), // Red in RGB (normalized 0-1)
                HorizTextAlignment = 0,
                VertTextAlignment = 0,
                PositiveOverlayColor = new ColorPt(0, 0, 0), // Black in RGB (normalized 0-1)
                RedactedContentColor = new ColorPt(0, 0, 0) // Black in RGB (normalized 0-1)
            };

            SearchAndRedact(sourcePath, targetPath, searchItems, overlayText, appearance);
        }

        // Overload 3: Source, Target, Phrases to find, Overlay Text, Customize Appearance
        public static void SearchAndRedact(string sourcePath, string targetPath, IList<SearchItem> searchItems, string overlayText, Redactor.Appearance appearance)
        {
            var doc = new PDFDoc(sourcePath);
            doc.InitSecurityHandler();

            int MAX_RESULTS = 1000;
            int iteration = 0;
            int page_num = 0;
            string result_str = "", ambient_string = "";
            Highlights hlts = new();

            var redactions = new ArrayList();

            // Step 1: Locate positions for each phrase using TextSearch
            foreach (SearchItem searchItem in searchItems)
            {
                var txtSearch = new TextSearch();
                var mode = TextSearch.SearchMode.e_whole_word | TextSearch.SearchMode.e_highlight;

                if (searchItem.ItemType == SearchItemType.RegEx)
                {
                    mode |= TextSearch.SearchMode.e_reg_expression;
                    txtSearch.SetPattern(searchItem.Value);
                }

                txtSearch.Begin(doc, searchItem.Value, (int)mode, -1, -1);
                TextSearch.ResultCode code = TextSearch.ResultCode.e_done;

                do
                {
                    code = txtSearch.Run(ref page_num, ref result_str, ref ambient_string, hlts);

                    if (code == TextSearch.ResultCode.e_found)
                    {
                        hlts.Begin(doc);
                        while (hlts.HasNext())
                        {
                            double[] quads = hlts.GetCurrentQuads();
                            int quad_count = quads.Length / 8;
                            for (int i = 0; i < quad_count; ++i)
                            {
                                // Assume each quad is an axis-aligned rectangle
                                int offset = 8 * i;
                                double x1 = Math.Min(Math.Min(Math.Min(quads[offset + 0], quads[offset + 2]), quads[offset + 4]), quads[offset + 6]);
                                double x2 = Math.Max(Math.Max(Math.Max(quads[offset + 0], quads[offset + 2]), quads[offset + 4]), quads[offset + 6]);
                                double y1 = Math.Min(Math.Min(Math.Min(quads[offset + 1], quads[offset + 3]), quads[offset + 5]), quads[offset + 7]);
                                double y2 = Math.Max(Math.Max(Math.Max(quads[offset + 1], quads[offset + 3]), quads[offset + 5]), quads[offset + 7]);

                                redactions.Add(new Redactor.Redaction(page_num, new Rect(x1, y1, x2, y2), false, overlayText));
                            }
                            hlts.Next();
                        }
                    }
                } while ((code != TextSearch.ResultCode.e_done) && (iteration++ < MAX_RESULTS));
            }

            // Step 2: Apply redactions
            if (redactions.Count > 0)
            {
                Redactor.Redact(doc, redactions, appearance);
            }
            else
            {
                Console.WriteLine("No redactions were made.");
            }
            doc.Save(targetPath, SDFDoc.SaveOptions.e_linearized);
        }
    }

    // Class representing a search item (either plain text or RegEx)
    public class SearchItem(SearchItemType itemType, string value)
    {
        public SearchItemType ItemType { get; set; } = itemType;
        public string Value { get; set; } = value;
    }

    // Enum representing the type of search item
    public enum SearchItemType
    {
        Text,
        RegEx
    }
}
