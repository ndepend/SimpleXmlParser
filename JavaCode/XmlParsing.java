package com.ndepend.inspections;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.StringReader;
import java.util.ArrayList;
import java.util.Arrays;

public class XmlParsing {

    public static XmlElement Go(String xmlContent) throws XmlException {

        ArrayList<XmlRow> rows = Pass1.Go(xmlContent);

        ArrayList<XmlElement> elems = Pass2.Go(rows, xmlContent, 0);

        return elems.get(0);
    }

}


//
// XML DOM model
//

class XmlAttribute {
    public String Name;
    public String Value;
}

class XmlElement {
    public String Name;
    public String Content ;
    public ArrayList<XmlAttribute> Attributes;
    public ArrayList<XmlElement> Elements;
}

//
// Pass 1
//   Get all Open Close and OpenClose elements listed
//
class XmlRow {
    public String Name;
    public int Index;  // Index of char after <$ or </$
    public int Length;  // Length from Index till just after >$ or />$
    public XmlRowKind Kind;
}

enum XmlRowKind {
    Open,      // <xyz>
    Close,     // </xyz>
    OpenClose, // <xyz/>
    Comment,   // <!--xyz-->
    CDATA      // <![CDATA[xyz]]>
}

class Pass1 {

    final static String CDATA_OPEN = "<![CDATA[";
    final static String CDATA_CLOSE = "]]>";

    public static ArrayList<XmlRow> Go(String xmlContent) throws XmlException {
        int index = xmlContent.indexOf("xml");
        if (index == -1) {
            throw new XmlException("'xml' expected");
        }

        ArrayList<XmlRow> list = new ArrayList<>();
        while (true) {
            int indexOpenChar = xmlContent.indexOf("<", index);
            if (indexOpenChar == -1) {
                if (list.size() == 0) {
                    throw new XmlException("'<' expected");
                } // At least one element must have been found!

                return list;
            }

            // parse  <xyz>   </xyz>   <xyz/>   <!--xyz-->   <![CDATA[xyz]]>
            XmlRowKind rowKind;
            int rowIndex = indexOpenChar + 1;
            int indexCloseChar;
            int indexFound = GetFirstCharIndexNotInAttribute(xmlContent, indexOpenChar, new char[] { '>', '/' });
            if (indexFound == -1) {
                throw new XmlException("'>' or '/' expected");
            }

            char nextChar = xmlContent.charAt(indexFound);
            if (nextChar == '>') {
                indexCloseChar = indexFound;

                if (xmlContent.charAt(indexOpenChar + 1) == '!') {

                    if (xmlContent.charAt(indexOpenChar + 2) == '[') {
                        // case  <![CDATA[xyz]]>
                        rowKind = XmlRowKind.CDATA;

                        String cdataOpenStr = xmlContent.substring(indexOpenChar, indexOpenChar + CDATA_OPEN.length());
                        if (!cdataOpenStr.equals(CDATA_OPEN)) {
                            throw new XmlException("wrongly formatted CDATA open section, "+cdataOpenStr+" instead of "+ CDATA_OPEN);
                        }
                        int cdataCloseIndex = xmlContent.indexOf(CDATA_CLOSE, indexOpenChar + CDATA_OPEN.length());
                        if (cdataCloseIndex == -1) {
                            throw new XmlException("CDATA open section " + CDATA_OPEN + " without close section " + CDATA_CLOSE);
                        }
                        // Compute the inside of CDATA section only  xyz  in   <![CDATA[xyz]]>
                        rowIndex += CDATA_OPEN.length() - 1;   // -1 coz   rowIndex   is index just after   '<'
                        indexCloseChar = cdataCloseIndex -1; // -1 to compensate the +1 in the rowLength formula below

                    } else {

                        // case <!--xyz-->
                        rowKind = XmlRowKind.Comment;

                        for (int i : new Integer[]{indexOpenChar + 2, indexOpenChar + 3, indexCloseChar - 1, indexCloseChar - 2}) {
                            if ((xmlContent.charAt(i) != '-')) {
                                throw new XmlException("wrongly formatted xml comment");
                            }
                        }
                    }

                }
                else {
                    // case <xyz>
                    rowKind = XmlRowKind.Open;
                }

            } else {
                //Debug.Assert(nextChar == '/');
                if (indexFound == indexOpenChar + 1) {
                    rowKind = XmlRowKind.Close; // </xyz>
                    rowIndex += 1;
                } else {
                    rowKind = XmlRowKind.OpenClose; // <xyz/>
                }

                indexCloseChar = xmlContent.indexOf(">", indexFound + 1);
                if (indexCloseChar == -1) {
                    throw new XmlException("'>' expected");
                }
            }

            int rowLength = indexCloseChar + 1 - rowIndex;

            // Parse elemName
            String rowName = xmlContent.substring(rowIndex, rowIndex+rowLength);
            rowName = rowName.replace(">", "");
            rowName = rowName.replace("/", "");
            rowName = rowName.trim();
            int spaceIndex = rowName.indexOf(' '); // In case of attribute
            if (spaceIndex > 0) {
                rowName = rowName.substring(0, spaceIndex);
            }

            // We're done with XmlRow, add it to list except if it is a comment row
            if (rowKind != XmlRowKind.Comment) {
                XmlRow elem = new XmlRow();
                elem.Name = rowName;
                elem.Index = rowIndex;
                elem.Length = rowLength;
                elem.Kind = rowKind;

                list.add(elem);
            }

            // Now try find the next XmlRow after the one just found!
            index = rowIndex + rowLength;
        }
    }

    public static int GetFirstCharIndexNotInAttribute(String xmlContent, int index, char[] chars) {
        int indexFound = Integer.MAX_VALUE;
        for (char c : chars) {
            int indexChar = IndexOfNotInAttribute(xmlContent, c, index);
            if (indexChar == -1) { continue; }
            if (indexChar < indexFound) {
                indexFound = indexChar;
            }
        }
        return indexFound < Integer.MAX_VALUE ? indexFound : -1;
    }

    private static int IndexOfNotInAttribute(String xmlContent, char charToFind, int index) {
        Boolean inAttribute = false;
        for (int i = index; i < xmlContent.length(); i++) {
            char c = xmlContent.charAt(i);
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
class Pass2 {
    public static ArrayList<XmlElement> Go(ArrayList<XmlRow> rows, String xmlContent, int indexRow) throws XmlException {
        ArrayList<XmlElement> list = new ArrayList<>();

        while (true) {
            if (indexRow == rows.size()) {
                // We reached the end of rows
                return list;
            }

            XmlRow row = rows.get(indexRow);
            XmlElement elem;
            int nextRowIndex;

            if (row.Kind == XmlRowKind.Close) {
                // Ok we finished parsing these siblings row
                return list;
            }

            if (row.Kind == XmlRowKind.OpenClose) {
                // Case  <xyz/>
                elem = new XmlElement();
                elem.Name = row.Name;
                elem.Attributes = ParseAttributes(row, xmlContent);
                nextRowIndex = indexRow + 1;

            } else {
                // Case  <xyz>...</xyz>
                //Debug.Assert(row.Kind == XmlRowKind.Open);

                // Search for matching close row
                XmlRow rowClose = null;
                int indexRowClose = 0;
                for (int i = indexRow + 1; i < rows.size(); i++) {
                    XmlRow rowTmp = rows.get(i);
                    if (rowTmp.Kind != XmlRowKind.Close ||
                        !rowTmp.Name.equals(row.Name)) {
                        continue;
                    }
                    rowClose = rowTmp;
                    indexRowClose = i;
                    break;
                }

                if (rowClose == null) {
                    throw new XmlException("No close elem </" + row.Name + ">");
                }
                nextRowIndex = indexRowClose + 1;

                String content;
                ArrayList<XmlElement> childElems;
                if (indexRowClose == indexRow + 1) {
                    // case  <Row>content</Row>
                    content = ParseContent(row, rowClose, xmlContent);
                    childElems = new ArrayList<>(); // remain empty
                } else if (indexRowClose == indexRow + 2 &&
                        rows.get(indexRow+1).Kind == XmlRowKind.CDATA) {
                    // CDATA row is not kept, we only grab its content
                    content = ParseCDATAContent(rows.get(indexRow + 1), xmlContent);
                    childElems = new ArrayList<XmlElement>(); // remain empty

                } else {
                    // case  <Row> <Child /><Child /> </Row>
                    content = "";
                    childElems = Go(rows, xmlContent, indexRow + 1);
                }

                elem = new XmlElement();
                elem.Name = row.Name;
                elem.Content = content;
                elem.Elements = childElems;
                elem.Attributes = ParseAttributes(row, xmlContent);

            }

            list.add(elem);
            indexRow = nextRowIndex;
        }
    }


    static String ParseContent(XmlRow elem1, XmlRow elem1Close, String xmlContent) {
        int indexContentStart = elem1.Index + elem1.Length;
        int indexContentEnd = xmlContent.lastIndexOf('<', elem1Close.Index);
        //Debug.Assert(indexContentEnd >= indexContentStart);
        String untrimmedContent = xmlContent.substring(indexContentStart, indexContentEnd);

        // Trim each line of  untrimmedContent and remove first empty line(s)

        ArrayList<String> list = new ArrayList<>();
        Boolean bANonEmptyLineHasBeenFound = false;
        try (BufferedReader reader = new BufferedReader(new StringReader(untrimmedContent))) {
            String line = reader.readLine();
            while (line != null) {
                line = line.trim();
                // Don't add first empty line(s)
                if (line.length() > 0) {
                    bANonEmptyLineHasBeenFound = true;
                }
                if (bANonEmptyLineHasBeenFound) {
                    list.add(line);
                }
                line = reader.readLine();
            }
        } catch (IOException exc) {
        }

        // Remove last empty line(s)
        for (int i = list.size() - 1; i >= 0; i--) {
            if (list.get(i).length() > 0) { break; }
            list.remove(i);
        }

        // Concatenate lines
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < list.size(); i++) {
            sb.append(list.get(i));
            if (i < list.size() - 1) {
                sb.append("\n");
            }
        }
        String content = sb.toString();

        // Special XML char
        content = content.replace("&gt;", ">");
        content = content.replace("&lt;", "<");
        content = content.replace("&quot;", "\"");
        content = content.replace("&#13;", "\r");
        content = content.replace("&#10;", "\n");
        content = content.replace("&amp;", "&");
        return content;
    }

    static String ParseCDATAContent(XmlRow rowCDATA, String xmlContent) {
        //Debug.Assert(rowCDATA.Kind == XmlRowKind.CDATA);
        return xmlContent.substring(rowCDATA.Index, rowCDATA.Index + rowCDATA.Length);
    }

    static ArrayList<XmlAttribute> ParseAttributes(XmlRow elem, String xmlContent) throws XmlException {
        ArrayList<XmlAttribute> list = new ArrayList<>();

        String elemContent = xmlContent.substring(elem.Index, elem.Index + elem.Length - 1);
        elemContent = elemContent.replace(elem.Name, "");

        int indexLastQuoteEnd = 0;
        while (true) {
            int indexEqual = elemContent.indexOf('=', indexLastQuoteEnd);
            if (indexEqual == -1) {
                break;
            }

            int indexQuoteOpen = elemContent.indexOf('"', indexEqual);
            if (indexQuoteOpen == -1) {
                throw new XmlException("'\"' open expected");
            }

            indexQuoteOpen++;

            int indexQuoteEnd = elemContent.indexOf('"', indexQuoteOpen);
            if (indexQuoteEnd == -1) {
                throw new XmlException("'\"' close expected");
            }

            String attrValue = elemContent.substring(indexQuoteOpen, indexQuoteEnd);
            String attrName = elemContent.substring(indexLastQuoteEnd, indexEqual);
            attrName = attrName.trim();

            XmlAttribute attr = new XmlAttribute();
            attr.Name = attrName;
            attr.Value = attrValue;

            list.add(attr);

            indexLastQuoteEnd = indexQuoteEnd + 1; // Prepare next attribute
        }

        return list;
    }
}
