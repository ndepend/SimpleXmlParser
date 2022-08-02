package com.ndepend.inspections;


class InspectionTypeInfo {
    public String id;
    public String name;
    public String category;
    public String description;

    public jetbrains.buildServer.agent.inspections.InspectionTypeInfo ToTeamCityInspectionType() {
        jetbrains.buildServer.agent.inspections.InspectionTypeInfo type = new jetbrains.buildServer.agent.inspections.InspectionTypeInfo();
        type.setId(id);
        type.setName(name);
        type.setCategory(category);
        type.setDescription(description);
        return type;
    }
}
