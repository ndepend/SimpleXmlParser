package com.ndepend.inspections;

import jetbrains.buildServer.agent.inspections.InspectionAttributesId;

import java.nio.file.FileSystems;
import java.nio.file.Path;
import java.util.Arrays;

public class InspectionInstance {
    public String id;
    public String message;
    public String filePath;
    public int line;
    public String severity;

    public jetbrains.buildServer.agent.inspections.InspectionInstance ToTeamCityInspection(String checkoutDirectory) {
        jetbrains.buildServer.agent.inspections.InspectionInstance inspection = new jetbrains.buildServer.agent.inspections.InspectionInstance();
        inspection.setInspectionId(id);
        inspection.setMessage(message);
        inspection.setFilePath(getInspectionPath(checkoutDirectory, filePath));
        inspection.setLine(line);
        inspection.addAttribute(InspectionAttributesId.SEVERITY.toString(), Arrays.asList(severity));
        return inspection;
    }

    public static String getInspectionPath(String checkoutDirectory, String filePath) {
        if (filePath == null || checkoutDirectory == null) {
            return null;
        }
        String relativePath = getRelativePath(checkoutDirectory, filePath);
        return relativePath.replace('\\', '/');
    }

    private static String getRelativePath(String absoluteCheckoutDirectory, String absoluteFilePath) {
        Path checkoutPath = FileSystems.getDefault().getPath(absoluteCheckoutDirectory);
        Path filePath = FileSystems.getDefault().getPath(absoluteFilePath);
        if (!filePath.startsWith(checkoutPath)) {
            return absoluteFilePath;
        }
        return checkoutPath.relativize(filePath).toString();
    }
}
