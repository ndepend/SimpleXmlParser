package com.ndepend.inspections;

import java.util.ArrayList;



public class FillInspectionsModel {


    public static InspectionsExport Go(XmlElement xmlElement) throws ModelException {
        InspectionsExport inspectionsExport = GetInspectionsExport(xmlElement);
        if(inspectionsExport == null) {
            throw new ModelException(xmlElement, new String[] { TAG_InspectionsExport});
        }

        return inspectionsExport;
    }


    final static String TAG_InspectionsExport = "InspectionsExport";



    private static InspectionsExport GetInspectionsExport(XmlElement xmlElement) throws ModelException {
        if (!xmlElement.Name.equals(TAG_InspectionsExport)) {
            return null;
        }

        InspectionsExport inspectionsExport = new InspectionsExport();
        for (XmlElement child : xmlElement.Elements) {
            ArrayList<InspectionTypeInfo> inspectionTypeInfos = GetInspectionTypes(child);
            if(inspectionTypeInfos != null) {
                inspectionsExport.inspectionTypes.addAll(inspectionTypeInfos);
                continue;

            }

            ArrayList<InspectionInstance> inspectionInstances = GetInspections(child);
            if(inspectionInstances != null){
                inspectionsExport.inspections.addAll(inspectionInstances);
                continue;
            }

            throw new ModelException(child, new String[] { TAG_InspectionTypes, TAG_Inspections });
        }
        return inspectionsExport;
    }

    final static String TAG_InspectionTypes = "InspectionTypes";
    private static ArrayList<InspectionTypeInfo> GetInspectionTypes(XmlElement xmlElement) throws ModelException {
        if (!xmlElement.Name.equals(TAG_InspectionTypes)) {
            return null;
        }
        ArrayList<InspectionTypeInfo> inspectionTypeInfos = new ArrayList<>();
        for (XmlElement child : xmlElement.Elements) {
            InspectionTypeInfo inspectionTypeInfo = GetInspectionTypeInfo(child);
            if(inspectionTypeInfo != null) {
                inspectionTypeInfos.add(inspectionTypeInfo);

            } else {
                throw new ModelException(child, new String[] {TAG_InspectionTypeInfo});
            }
        }
        return inspectionTypeInfos;
    }



    final static String TAG_InspectionTypeInfo = "InspectionTypeInfo";
    final static String TAG_Description = "Description";
    final static String TAG_Id = "Id";
    final static String TAG_Name = "Name";
    final static String TAG_Category = "Category";

    private static InspectionTypeInfo GetInspectionTypeInfo(XmlElement xmlElement) throws ModelException {
        if (!xmlElement.Name.equals(TAG_InspectionTypeInfo)) {
            return null;
        }

        InspectionTypeInfo inspectionTypeInfo = new InspectionTypeInfo();

        if (xmlElement.Elements.size() > 0) {
            XmlElement child = xmlElement.Elements.get(0);
            if (!child.Name.equals(TAG_Description)) {
                throw new ModelException(child, new String[] {TAG_Description});
            }
            inspectionTypeInfo.description = child.Content;
        }
        for (XmlAttribute attr : xmlElement.Attributes) {
            String val = attr.Value;
            switch (attr.Name) {
                case TAG_Id: inspectionTypeInfo.id = val; break;
                case TAG_Name: inspectionTypeInfo.name = val; break;
                case TAG_Category: inspectionTypeInfo.category = val; break;
                default: throw new ModelException(attr, new String[] {TAG_Id, TAG_Name, TAG_Category});
            }
        }
        return inspectionTypeInfo;
    }




    final static String TAG_Inspections = "Inspections";
    private static ArrayList<InspectionInstance> GetInspections(XmlElement xmlElement) throws ModelException {
        if (!xmlElement.Name.equals(TAG_Inspections)) {
            return null;
        }
        ArrayList<InspectionInstance>  inspectionInstances = new ArrayList<>();
        for (XmlElement child : xmlElement.Elements) {
            InspectionInstance inspectionInstance = GetInspectionInstance(child);
            if(inspectionInstance != null) {
                inspectionInstances.add(inspectionInstance);

            } else {
                throw new ModelException(child, new String[] {TAG_InspectionInstance});
            }
        }
        return inspectionInstances;
    }




    final static String TAG_InspectionInstance = "InspectionInstance";
    final static String TAG_Message = "Message";
    final static String TAG_FilePath = "FilePath";
    final static String TAG_Line = "Line";
    final static String TAG_Severity = "Severity";
    private static InspectionInstance GetInspectionInstance(XmlElement xmlElement) throws ModelException {
        if (!xmlElement.Name.equals(TAG_InspectionInstance)) {
            return null;
        }

        InspectionInstance inspectionInstance = new InspectionInstance();

        // Read child elem Message FilePath
        for (XmlElement child : xmlElement.Elements) {
            if (child.Name.equals(TAG_Message)) {
                inspectionInstance.message = child.Content;
                continue;
            }
            if (child.Name.equals(TAG_FilePath)) {
                inspectionInstance.filePath = child.Content;
                continue;
            }
            throw new ModelException(child, new String[] {TAG_Message, TAG_FilePath});
        }

        // Read attributes
        for (XmlAttribute attr : xmlElement.Attributes) {
            String val = attr.Value;
            switch (attr.Name) {
                case TAG_Id: inspectionInstance.id = val; break;
                case TAG_Line:
                    int iVal = tryParseInt(val);
                    if (iVal == Integer.MIN_VALUE) {
                        throw new ModelException("Expected an integer value for attribute "+attr.Name+", got instead "+val);
                    }
                    inspectionInstance.line = iVal;
                    break;
                case TAG_Severity: inspectionInstance.severity = val; break;
                default: throw new ModelException(attr, new String[] {TAG_Id, TAG_Line, TAG_Severity});
            }
        }
        return inspectionInstance;
    }

    static int tryParseInt(String value) {
        try {
            return Integer.parseInt(value);
        } catch (NumberFormatException e) {
            return Integer.MIN_VALUE;
        }
    }
}
