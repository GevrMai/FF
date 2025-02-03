using Domain.Enums;

namespace Domain.Models;

public record WarehouseNode(NodeType Type, (int CenterX, int CenterY) Coordinates);