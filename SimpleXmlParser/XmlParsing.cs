

using System.Diagnostics;
using System.Text;

public static class XmlParsing {

   public static XmlElement Go(string xmlContent) {

      List<XmlRow> rows = Pass1.Go(xmlContent);

      List<XmlElement> elems = Pass2.Go(rows, xmlContent, 0);

      return elems[0];
   }
}

//
// XML DOM model
//
public sealed class XmlException : Exception {
   public XmlException(string msg) : base(msg) { }
}

[DebuggerDisplay("Attr Name={Name}  Value={Value}")]
public sealed class XmlAttribute {
   public string Name { get; set; }
   public string Value { get; set; }
}


[DebuggerDisplay("Name={Name}  #Child={Elements.Count}  #Attributes={Attributes.Count}  HasContent={!string.IsNullOrEmpty(Content)}")]
public sealed class XmlElement {
   public string Name { get; set; }
   public string Content { get; set; }
   public List<XmlAttribute> Attributes { get; set; }
   public List<XmlElement> Elements { get; set; }
}


//
// Pass 1
//   Get all Open Close and OpenClose elements listed
//
[DebuggerDisplay("Name={Name}  Kind={Kind}  Index={Index}  Length={Length}")]
class XmlRow {
   public string Name { get; set; }
   public int Index { get; set; }  // Index of char after <$ or </$
   public int Length { get; set; }  // Length from Index till just after >$ or />$
   public XmlRowKind Kind { get; set; }
}

enum XmlRowKind {
   Open,      // <xyz>
   Close,     // </xyz>
   OpenClose, // <xyz/>
   Comment,   // <!--xyz-->
   CDATA      // <![CDATA[xyz]]>
}

static class Pass1 {
   public static List<XmlRow> Go(string xmlContent) {
      int index = xmlContent.IndexOf("xml");
      if (index == -1) {
         throw new XmlException(@"'xml' expected");
      }

      var list = new List<XmlRow>();
      while (true) {
         int indexOpenChar = xmlContent.IndexOf("<", index);
         if (indexOpenChar == -1) {
            if (list.Count == 0) {
               throw new XmlException(@"'<' expected");
            } // At least one element must have been found!

            return list;
         }

         // parse  <xyz>   </xyz>   <xyz/>   <!--xyz-->   <![CDATA[xyz]]>
         XmlRowKind rowKind;
         int rowIndex = indexOpenChar + 1;
         int indexCloseChar;
         if (!TryGetFirstCharIndexNotInAttribute(xmlContent, indexOpenChar, new[] { '>', '/' }, out int indexFound)) {
            throw new XmlException(@"'>' or '/' expected");
         }

         char nextChar = xmlContent[indexFound];
         if (nextChar == '>') {
            indexCloseChar = indexFound;

            if (xmlContent[indexOpenChar + 1] == '!') {
               

               if (xmlContent[indexOpenChar + 2] == '[') {
                  // case  <![CDATA[xyz]]>
                  rowKind = XmlRowKind.CDATA;

                  const string CDATA_OPEN = "<![CDATA[";
                  const string CDATA_CLOSE = "]]>";

                  string cdataOpenStr = xmlContent.Substring(indexOpenChar, CDATA_OPEN.Length);
                  if (cdataOpenStr != CDATA_OPEN) {
                     throw new XmlException(@"wrongly formatted CDATA open section, "+cdataOpenStr+" instead of "+ CDATA_OPEN);
                  }
                  int cdataCloseIndex = xmlContent.IndexOf(CDATA_CLOSE, indexOpenChar + CDATA_OPEN.Length);
                  if (cdataCloseIndex == -1) {
                     throw new XmlException(@"CDATA open section " + CDATA_OPEN + " without close section " + CDATA_CLOSE);
                  }
                  // Compute the inside of CDATA section only  xyz  in   <![CDATA[xyz]]>
                  rowIndex += CDATA_OPEN.Length - 1;   // -1 coz   rowIndex   is index just after   '<'
                  indexCloseChar = cdataCloseIndex -1; // -1 to compensate the +1 in the rowLength formula below

               } else {

                  // case <!--xyz-->
                  rowKind = XmlRowKind.Comment;

                  foreach (int i in new[] {
                              indexOpenChar + 2, indexOpenChar + 3, indexCloseChar - 1, indexCloseChar - 2
                           }) {
                     if ((xmlContent[i] != '-')) {
                        throw new XmlException(@"wrongly formatted xml comment");
                     }
                  }
               }

            }
            else {
               // case <xyz>
               rowKind = XmlRowKind.Open;
            }
            
         } else {
            Debug.Assert(nextChar == '/');
            if (indexFound == indexOpenChar + 1) {
               rowKind = XmlRowKind.Close; // </xyz>
               rowIndex += 1;
            } else {
               rowKind = XmlRowKind.OpenClose; // <xyz/>
            }

            indexCloseChar = xmlContent.IndexOf(">", indexFound + 1);
            if (indexCloseChar == -1) {
               throw new XmlException(@"'>' expected");
            }
         }

         int rowLength = indexCloseChar + 1 - rowIndex;

         // Parse elemName
         string rowName = xmlContent.Substring(rowIndex, rowLength);
         rowName = rowName.Replace(">", "");
         rowName = rowName.Replace("/", "");
         rowName = rowName.Trim();
         int spaceIndex = rowName.IndexOf(' '); // In case of attribute
         if (spaceIndex > 0) {
            rowName = rowName.Substring(0, spaceIndex);
         }

         // We're done with XmlRow, add it to list except if it is a comment row
         if (rowKind != XmlRowKind.Comment) {
            var elem = new XmlRow() {
               Name = rowName,
               Index = rowIndex,
               Length = rowLength,
               Kind = rowKind
            };
            list.Add(elem);
         }

         // Now try find the next XmlRow after the one just found!
         index = rowIndex + rowLength;
      }
   }

   
   public static bool TryGetFirstCharIndexNotInAttribute(string xmlContent, int index, char[] chars, out int indexFound) {
      indexFound = int.MaxValue;
      foreach (char c in chars) {
         int indexChar = IndexOfNotInAttribute(xmlContent, c, index);
         if (indexChar == -1) { continue; }
         if (indexChar < indexFound) {
            indexFound = indexChar;
         }
      }
      return indexFound < int.MaxValue;
   }

   private static int IndexOfNotInAttribute(string xmlContent, char charToFind, int index) {
      bool inAttribute = false;
      for (int i = index; i < xmlContent.Length; i++) {
         char c = xmlContent[i];
         if (c == '"') {
            inAttribute = !inAttribute;
            continue;
         }
         if (inAttribute) {
            continue;
         }
         if (c == charToFind) {
            return i;
         }
      }
      return -1;
   }
}


//
// Pass 2
//   Build a hierarchy from Open Close and openClose element
//   + parse attributes
//   + parse content
//
static class Pass2 {
   public static List<XmlElement> Go(List<XmlRow> rows, string xmlContent, int indexRow) {
      var list = new List<XmlElement>();

      while (true) {
         if (indexRow == rows.Count) {
            // We reached the end of rows
            return list;
         }

         XmlRow row = rows[indexRow];
         XmlElement elem;
         int nextRowIndex;

         if (row.Kind == XmlRowKind.Close) {
            // Ok we finished parsing these siblings row
            return list;
         }

         if (row.Kind == XmlRowKind.OpenClose) {
            // Case  <xyz/>
            elem = new XmlElement() {
               Name = row.Name,
               Attributes = ParseAttributes(row, xmlContent)
            };
            nextRowIndex = indexRow + 1;

         } else {
            // Case  <xyz>...</xyz>
            Debug.Assert(row.Kind == XmlRowKind.Open);

            // Search for matching close row 
            XmlRow rowClose = null;
            int indexRowClose = 0;
            for (int i = indexRow + 1; i < rows.Count; i++) {
               XmlRow rowTmp = rows[i];
               if (rowTmp.Kind != XmlRowKind.Close ||
                   rowTmp.Name != row.Name) {
                  continue;
               }
               rowClose = rowTmp;
               indexRowClose = i;
               break;
            }

            if (rowClose == null) {
               throw new XmlException(@"No close elem </" + row.Name + ">");
            }
            nextRowIndex = indexRowClose + 1;

            string content;
            List<XmlElement> childElems;
            if (indexRowClose == indexRow + 1) {
               // case  <Row>content</Row>
               content = ParseContent(row, rowClose, xmlContent);
               childElems = new List<XmlElement>(); // remain empty

            }  else if (indexRowClose == indexRow + 2 && 
                        rows[indexRow+1].Kind == XmlRowKind.CDATA) {
               // CDATA row is not kept, we only grab its content
               content = ParseCDATAContent(rows[indexRow + 1], xmlContent);
               childElems = new List<XmlElement>(); // remain empty

            } else {
               // case  <Row> <Child /><Child /> </Row>
               content = "";
               childElems = Go(rows, xmlContent, indexRow + 1);
            }

            elem = new XmlElement() {
               Name = row.Name,
               Content = content,
               Elements = childElems,
               Attributes = ParseAttributes(row, xmlContent)
            };
         }

         list.Add(elem);
         indexRow = nextRowIndex;
      }
   }


   static string ParseContent(XmlRow elem1, XmlRow elem1Close, string xmlContent) {
      int indexContentStart = elem1.Index + elem1.Length;
      int indexContentEnd = xmlContent.LastIndexOf('<', elem1Close.Index);
      Debug.Assert(indexContentEnd >= indexContentStart);
      string untrimmedContent = xmlContent.Substring(indexContentStart, indexContentEnd - indexContentStart);

      // Trim each line of  untrimmedContent and remove first empty line(s)
      var list = new List<string>();
      bool bANonEmptyLineHasBeenFound = false;
      using (StringReader sr = new StringReader(untrimmedContent)) {
         string line;
         while ((line = sr.ReadLine()) != null) {
            line = line.Trim();
            // Don't add first empty line(s)
            if (line.Length > 0) {
               bANonEmptyLineHasBeenFound = true;
            }
            if (bANonEmptyLineHasBeenFound) {
               list.Add(line);
            }
         }
      }

      // Remove last empty line(s)
      for (int i = list.Count - 1; i >= 0; i--) {
         if (list[i].Length > 0) { break; }
         list.RemoveAt(i);
      }
      
      // Concatenate lines
      var sb = new StringBuilder();
      for (int i = 0; i < list.Count; i++) {
         sb.Append(list[i]);
         if (i < list.Count - 1) {
            sb.Append(@"
");
         }
      }
      string content = sb.ToString();

      // Special XML char
      content = content.Replace("&gt;", ">");
      content = content.Replace("&lt;", "<");
      content = content.Replace("&quot;", @"""");
      content = content.Replace("&#13;", "\r");
      content = content.Replace("&#10;", "\n");
      content = content.Replace("&amp;", "&");

      return content;
   }
   
   static string ParseCDATAContent(XmlRow rowCDATA, string xmlContent) {
      Debug.Assert(rowCDATA.Kind == XmlRowKind.CDATA);
      return xmlContent.Substring(rowCDATA.Index, rowCDATA.Length);
   }

   static List<XmlAttribute> ParseAttributes(XmlRow elem, string xmlContent) {
      var list = new List<XmlAttribute>();

      string elemContent = xmlContent.Substring(elem.Index, elem.Length - 1);
      elemContent = elemContent.Replace(elem.Name, "");

      int indexLastQuoteEnd = 0;
      while (true) {
         int indexEqual = elemContent.IndexOf('=', indexLastQuoteEnd);
         if (indexEqual == -1) {
            break;
         }

         int indexQuoteOpen = elemContent.IndexOf('"', indexEqual);
         if (indexQuoteOpen == -1) {
            throw new XmlException(@"'""' open expected");
         }

         indexQuoteOpen++;

         int indexQuoteEnd = elemContent.IndexOf('"', indexQuoteOpen);
         if (indexQuoteEnd == -1) {
            throw new XmlException(@"'""' close expected");
         }

         string attrValue = elemContent.Substring(indexQuoteOpen, indexQuoteEnd - indexQuoteOpen);
         string attrName = elemContent.Substring(indexLastQuoteEnd, indexEqual - indexLastQuoteEnd);
         attrName = attrName.Trim();

         list.Add(new XmlAttribute() {
            Name = attrName,
            Value = attrValue
         });

         indexLastQuoteEnd = indexQuoteEnd + 1; // Prepare next attribute
      }

      return list;
   }
}
