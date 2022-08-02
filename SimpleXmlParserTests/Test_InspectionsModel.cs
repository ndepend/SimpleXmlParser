using System.Diagnostics;
using NUnit.Framework;

using SimpleXmlParser.InspectionsModel;

namespace SimpleXmlParser {
   public class Test_InspectionsModel {
      [SetUp]
      public void Setup() {

         using (Stream? stream = this.GetType().Assembly.GetManifestResourceStream("Inspections.xml")) {
            using (StreamReader sr = new StreamReader(stream)) {
               m_XmlContent = sr.ReadToEnd();
            }
         }
      }

      private string m_XmlContent;

      [Test]
      public void Test_InspectionsXml() {

         XmlElement elem = XmlParsing.Go(m_XmlContent);
         InspectionsExport inspectionsExport = FillInspectionsModel.Go(elem);

         Assert.IsTrue(inspectionsExport.inspectionTypes.Count == 2);
         var it0 = inspectionsExport.inspectionTypes[0];

         Assert.IsTrue(it0.id == "Methods shouldn't be named NonCritical");
         Assert.IsTrue(it0.name == "Methods shouldn't be named NonCritical");
         Assert.IsTrue(it0.category == "Statistic Checks");
         Assert.IsTrue(it0.description == @"// <Name>Methods shouldn't be named NonCritical</Name>
warnif count > 0 from m in JustMyCode.Methods
where m.FullNameLike(""NonCritical"")
select new { m }

// This triggers non critical rule violations, with multiple violated");

         var it1 = inspectionsExport.inspectionTypes[1];
         Assert.IsTrue(it1.id == "Inspection with CDATA1");
         Assert.IsTrue(it1.name == "Inspection with CDATA2");
         Assert.IsTrue(it1.category == "Just for tests");
         Assert.IsTrue(it1.description == @"• <i>Rule Description:</i><br> It's not a hard, fast rule, but multiple asserts in a unit test is a smell. I'm setting this arbitrarily at 10 (RASD updated to 20), but you might want to let that number grow as your test suite grows. Every now and then, this might make sense. But, usually, it's an indicator that people haven't yet grokked unit testing. Each test is essentially a very directed, specific experiment with one setup, one action, one check (arrange, act, assert) If you have multiple asserts, then how do you know what a failure of the test means at a glance? Usually, if you have 2+ asserts in a test, you should have 2+ tests");



         Assert.IsTrue(inspectionsExport.inspections.Count == 2);
         var i0 = inspectionsExport.inspections[0];
         Assert.IsTrue(i0.id == "Methods shouldn't be named NonCritical/");
         Assert.IsTrue(i0.line == 8);
         Assert.IsTrue(i0.severity == "WARN");
         Assert.IsTrue(i0.message == "NonCriticalA()");
         Assert.IsTrue(i0.filePath == @"c:\Code\bitbucket\ndepend-teamcity-plugin\Console\TestSets\TestApplication\TestApplication\StatisticChecks.cs"); 
         
         var i1 = inspectionsExport.inspections[1];
         Assert.IsTrue(i1.id == "Methods shouldn't be named NonCritical");
         Assert.IsTrue(i1.line == 12);
         Assert.IsTrue(i1.severity == "WARN");
         Assert.IsTrue(i1.message == "NonCriticalB()");
         Assert.IsTrue(i1.filePath == @"c:\Code\bitbucket\ndepend-teamcity-plugin\Console\TestSets\TestApplication\TestApplication\StatisticChecks.cs");
      }

      [Test]
      public void Test_ComplexInspectionsXml() {

         string xmlContent;
         using (Stream? stream = this.GetType().Assembly.GetManifestResourceStream("ComplexInspections.xml")) {
            using (StreamReader sr = new StreamReader(stream)) {
               xmlContent = sr.ReadToEnd();
            }
         }

         XmlElement elem = XmlParsing.Go(xmlContent);
         InspectionsExport inspectionsExport = FillInspectionsModel.Go(elem);

         Assert.IsTrue(inspectionsExport.inspectionTypes.Count == 11);
         Assert.IsTrue(inspectionsExport.inspections.Count == 142);
      }

      [Test]
      public void Test_GurdipInspectionsXml() {

         string xmlContent;
         using (Stream? stream = this.GetType().Assembly.GetManifestResourceStream("GurdipInspections.xml")) {
            using (StreamReader sr = new StreamReader(stream)) {
               xmlContent = sr.ReadToEnd();
            }
         }

         XmlElement elem = XmlParsing.Go(xmlContent);
         InspectionsExport inspectionsExport = FillInspectionsModel.Go(elem);

         Assert.IsTrue(inspectionsExport.inspectionTypes.Count == 39);
         Assert.IsTrue(inspectionsExport.inspections.Count == 532);
      }


      [TestCase("InspectionsExport", "Foo", "Expected InspectionsExport tag but xml element name was Foo")]
      [TestCase("InspectionTypes", "Foo", "Expected InspectionTypes or Inspections tag but xml element name was Foo")]
      [TestCase("InspectionTypeInfo", "Foo", "Expected InspectionTypeInfo tag but xml element name was Foo")]
      [TestCase("Description", "Foo", "Expected Description tag but xml element name was Foo")]
      [TestCase("InspectionInstance", "Foo", "Expected InspectionInstance tag but xml element name was Foo")]

      [TestCase("Id=", "Foo=", "Expected Id or Name or Category tag but xml attribute name was Foo")]
      [TestCase("Name=", "Foo=", "Expected Id or Name or Category tag but xml attribute name was Foo")]
      [TestCase("Category=", "Foo=", "Expected Id or Name or Category tag but xml attribute name was Foo")]
      [TestCase("Line=", "Foo=", "Expected Id or Line or Severity tag but xml attribute name was Foo")]
      [TestCase(@"Line=""8""", @"Line=""XYZ""", "Expected an integer value for attribute Line, got instead XYZ")]
      [TestCase("Severity=", "Foo=", "Expected Id or Line or Severity tag but xml attribute name was Foo")]

      [TestCase("Message", "Foo", "Expected Message or FilePath tag but xml element name was Foo")]
      [TestCase("FilePath", "Foo", "Expected Message or FilePath tag but xml element name was Foo")]

      public void Test_Fail(string from, string to, string expectedEx) {

         var xmlContent = m_XmlContent.Replace(from, to);

         bool exThrown = false;
         try {
            XmlElement elem = XmlParsing.Go(xmlContent);
            InspectionsExport inspectionsExport = FillInspectionsModel.Go(elem);
         }
         catch (ModelException ex) {
            exThrown = true;
            //Debug.WriteLine(ex.Message);
            Assert.IsTrue(ex.Message == expectedEx);
         }
         Assert.IsTrue(exThrown);
      }

      
   }
}