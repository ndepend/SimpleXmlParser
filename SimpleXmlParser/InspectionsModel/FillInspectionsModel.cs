

namespace SimpleXmlParser.InspectionsModel {



   public static class FillInspectionsModel {

      public static InspectionsExport Go(XmlElement xmlElement) {
         var inspectionsExport = GetInspectionsExport(xmlElement);
         if(inspectionsExport == null) {
            throw new ModelException(xmlElement, TAG_InspectionsExport);
         }

         return inspectionsExport;
      }


      private const string TAG_InspectionsExport = "InspectionsExport";
      
      

      private static InspectionsExport? GetInspectionsExport(XmlElement xmlElement) {
         if (xmlElement.Name != TAG_InspectionsExport) {
            return null;
         }

         var inspectionsExport = new InspectionsExport();
         foreach (XmlElement child in xmlElement.Elements) {
            List<InspectionTypeInfo>? inspectionTypeInfos = GetInspectionTypes(child);
            if(inspectionTypeInfos != null) {
               inspectionsExport.inspectionTypes.AddRange(inspectionTypeInfos);
               continue;

            }

            List<InspectionInstance>? inspectionInstances = GetInspections(child);
            if(inspectionInstances != null){
               inspectionsExport.inspections.AddRange(inspectionInstances);
               continue;
            } 

            throw new ModelException(child, TAG_InspectionTypes, TAG_Inspections);
         }
         return inspectionsExport;
      }

      private const string TAG_InspectionTypes = "InspectionTypes";
      private static List<InspectionTypeInfo>? GetInspectionTypes(XmlElement xmlElement) {
         if (xmlElement.Name != TAG_InspectionTypes) {
            return null;
         }
         List<InspectionTypeInfo> inspectionTypeInfos = new List<InspectionTypeInfo>();
         foreach (XmlElement child in xmlElement.Elements) {
            InspectionTypeInfo? inspectionTypeInfo = GetInspectionTypeInfo(child);
            if(inspectionTypeInfo != null) {
               inspectionTypeInfos.Add(inspectionTypeInfo);

            } else {
               throw new ModelException(child, TAG_InspectionTypeInfo);
            }
         }
         return inspectionTypeInfos;
      }



      private const string TAG_InspectionTypeInfo = "InspectionTypeInfo";
      private const string TAG_Description = "Description";
      private const string TAG_Id = "Id";
      private const string TAG_Name = "Name";
      private const string TAG_Category = "Category";
      
      private static InspectionTypeInfo? GetInspectionTypeInfo(XmlElement xmlElement) {
         if (xmlElement.Name != TAG_InspectionTypeInfo) {
            return null;
         }

         var inspectionTypeInfo = new InspectionTypeInfo();

         var child = xmlElement.Elements.FirstOrDefault();
         if (child != null) {
            if (child.Name != TAG_Description) {
               throw new ModelException(child, TAG_Description);
            }
            inspectionTypeInfo.description = child.Content;
         }
         foreach (XmlAttribute attr in xmlElement.Attributes) {
            string val = attr.Value;
            switch (attr.Name) {
               case TAG_Id: inspectionTypeInfo.id = val; break;
               case TAG_Name: inspectionTypeInfo.name = val; break;
               case TAG_Category: inspectionTypeInfo.category = val; break;
               default: throw new ModelException(attr, TAG_Id, TAG_Name, TAG_Category);
            }
         }
         return inspectionTypeInfo;
      }




      private const string TAG_Inspections = "Inspections";
      private static List<InspectionInstance>? GetInspections(XmlElement xmlElement) {
         if (xmlElement.Name != TAG_Inspections) {
            return null;
         }
         List<InspectionInstance>  inspectionInstances = new List<InspectionInstance>();
         foreach (XmlElement child in xmlElement.Elements) {
            InspectionInstance? inspectionInstance = GetInspectionInstance(child);
            if(inspectionInstance != null) {
               inspectionInstances.Add(inspectionInstance);

            } else {
               throw new ModelException(child, TAG_InspectionInstance);
            }
         }
         return inspectionInstances;
      }




      private const string TAG_InspectionInstance = "InspectionInstance";
      private const string TAG_Message = "Message";
      private const string TAG_FilePath = "FilePath";
      private const string TAG_Line = "Line";
      private const string TAG_Severity = "Severity";
      private static InspectionInstance? GetInspectionInstance(XmlElement xmlElement) {
         if (xmlElement.Name != TAG_InspectionInstance) {
            return null;
         }

         var inspectionInstance = new InspectionInstance();

         // Read child elem Message FilePath
         foreach (var child in xmlElement.Elements) {
            if (child.Name == TAG_Message) {
               inspectionInstance.message = child.Content;
               continue;
            }
            if (child.Name == TAG_FilePath) {
               inspectionInstance.filePath = child.Content;
               continue;
            }
            throw new ModelException(child, TAG_Message, TAG_FilePath);
         }

         // Read attributes
         foreach (XmlAttribute attr in xmlElement.Attributes) {
            string val = attr.Value;
            switch (attr.Name) {
               case TAG_Id: inspectionInstance.id = val; break;
               case TAG_Line:
                  if (!int.TryParse(val, out int iVal)) {
                     throw new ModelException($"Expected an integer value for attribute {attr.Name}, got instead {val}");
                  }
                  inspectionInstance.line = iVal;
                  break;
               case TAG_Severity: inspectionInstance.severity = val; break;
               default: throw new ModelException(attr, TAG_Id, TAG_Line, TAG_Severity);
            }
         }
         return inspectionInstance;
      }

   }
}
