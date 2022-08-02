package com.ndepend.inspections;

public class ModelException extends Exception {
    public ModelException(XmlElement xmlElement, String[] tagsExpected) {
        super("Expected "+Aggregate(tagsExpected)+" tag but xml element name was "+xmlElement.Name);
    }

    public ModelException(XmlAttribute xmlAttribute, String[] tagsExpected) {
        super("Expected "+Aggregate(tagsExpected)+" tag but xml attribute name was "+xmlAttribute.Name);
    }

    public ModelException(String message) {
        super(message);
    }

    private static String Aggregate(String[] arr) {
        StringBuilder sb = new StringBuilder(arr[0]);
        for (int i = 1; i < arr.length; i++) {
            sb.append(" or ");
            sb.append(arr[i]);
        }
        return sb.toString();
    }
}
