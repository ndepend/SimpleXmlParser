using System.Diagnostics;
using NUnit.Framework;


namespace SimpleXmlParserTests {
   public class Test_XmlParsing {

      [SetUp]
      public void Setup() {

         using (Stream? stream = this.GetType().Assembly.GetManifestResourceStream("Inspections.xml")) {
            using (StreamReader sr = new StreamReader(stream)) {
               m_XmlContent = sr.ReadToEnd();
            }
         }
      }

      private string m_XmlContent;


      [TestCase("<?xml", "<?hello", @"'xml' expected")]
      [TestCase("<", "$", @"'<' expected")]
      [TestCase("<", "$", @"'<' expected")]
      [TestCase(">", "$", @"'>' expected")]
      [TestCase("</Description>", "<!--/Description-->", @"No close elem </Description>")]
      [TestCase(@"Severity=""WARN""", @"Severity=WARN", @"'""' open expected")]
      [TestCase(@"Severity=""WARN""", @"Severity=""WARN", @"'""' close expected")]
      [TestCase(@"<InspectionsExport>", @"<!InspectionsExport>", @"wrongly formatted xml comment")]
      [TestCase(@"<InspectionsExport>", @"<!-InspectionsExport>", @"wrongly formatted xml comment")]
      [TestCase(@"<InspectionsExport>", @"<!--InspectionsExport>", @"wrongly formatted xml comment")]
      [TestCase(@"<InspectionsExport>", @"<!--InspectionsExport->", @"wrongly formatted xml comment")]
      [TestCase(@"<![CDATA[", @"<![CDATX[", @"wrongly formatted CDATA open section, <![CDATX[ instead of <![CDATA[")]
      [TestCase(@"<![CDATA[", @"<![", @"wrongly formatted CDATA open section, <![• <i>R instead of <![CDATA[")]
      [TestCase(@"]]>", @"]!>", @"CDATA open section <![CDATA[ without close section ]]>")]
      [TestCase(@"]]>", @"", @"CDATA open section <![CDATA[ without close section ]]>")]
      public void Test_Fail1(string from, string to, string expectedEx) {

         var xmlContent = m_XmlContent.Replace(from, to);

         bool exThrown = false;
         try {
            XmlElement elem = XmlParsing.Go(xmlContent);
         } catch (XmlException ex) {
            exThrown = true;
            Debug.WriteLine(ex.Message);
            //Assert.IsTrue(ex.Message == expectedEx);
         }
         Assert.IsTrue(exThrown);
      }


      [TestCase(">", "$", "/", "$", @"'>' or '/' expected")]
      public void Test_Fail2(string from0, string to0, string from1, string to1,string expectedEx) {

         var xmlContent = m_XmlContent.Replace(from0, to0);
         xmlContent = xmlContent.Replace(from1, to1);

         bool exThrown = false;
         try {
            XmlElement elem = XmlParsing.Go(xmlContent);
         } catch (XmlException ex) {
            exThrown = true;
            Debug.WriteLine(ex.Message);
            //Assert.IsTrue(ex.Message == expectedEx);
         }
         Assert.IsTrue(exThrown);
      }


      [Test]
      public void Test_OKRowOpenClose() {
         XmlElement elemRoot = XmlParsing.Go(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <Elem Id=""1"" />
</Root>");
         Assert.IsTrue(elemRoot.Elements.Count == 1);
         var elem = elemRoot.Elements[0];
         Assert.IsTrue(elem.Name == "Elem");
         Assert.IsTrue(elem.Attributes.Count == 1);
         var attr = elem.Attributes[0];
         Assert.IsTrue(attr.Name == "Id");
         Assert.IsTrue(attr.Value == "1");
      }

      [Test]
      public void Test_OKCommentRow() {
         XmlElement elemRoot = XmlParsing.Go(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <!--Elem Id=""1""-->
</Root>");
         Assert.IsTrue(elemRoot.Elements.Count == 0);
      }
      [Test]
      public void Test_KOCommentRow() {
         XmlElement elemRoot = XmlParsing.Go(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <!--Elem Id=""1""-->
</Root>");
         Assert.IsTrue(elemRoot.Elements.Count == 0);
      }


      [TestCase(@"")]
      [TestCase(@"
")]
      [TestCase(@"<>[!@$ &amp; &gt; &lt; &quot;")]
      public void Test_OK_CDATA(string str) {
         XmlElement elemRoot = XmlParsing.Go($@"<?xml version=""1.0"" encoding=""utf-8""?>
<Root>
  <![CDATA[{str}]]>
</Root>");
         Assert.IsTrue(elemRoot.Content == str);
         Assert.IsTrue(elemRoot.Elements.Count == 0);
      }
   }
}
