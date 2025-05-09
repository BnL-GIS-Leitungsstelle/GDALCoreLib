namespace GdalToolsLib.Geometry;

public record struct SelfOverlapErrorResult(long FidA, long FidB, double Area);