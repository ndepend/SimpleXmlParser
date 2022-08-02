
using System.Text;

namespace SimpleXmlParser.InspectionsModel {
   public class ModelException : Exception {
      internal ModelException(XmlElement xmlElement, params string[] tagsExpected) : base(
         $"Expected {Aggregate(tagsExpected, " or ")} tag but xml element name was {xmlElement.Name}") { }

      internal ModelException(XmlAttribute xmlAttribute, params string[] tagsExpected) : base(
         $"Expected {Aggregate(tagsExpected, " or ")} tag but xml attribute name was {xmlAttribute.Name}") { }

      internal ModelException(string msg) : base(msg) { }


      private static string Aggregate(string[] arr, string sep) {
         StringBuilder sb = new StringBuilder(arr[0]);
         for (int i = 1; i < arr.Length; i++) {
            sb.Append(sep);
            sb.Append(arr[i]);
         }
         return sb.ToString();
      }
   }
}
