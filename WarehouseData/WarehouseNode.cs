namespace FF.WarehouseData;

public record WarehouseNode(NodeType Type, (int CenterX, int CenterY) Coordinates);