using System;
using FluentAssertions;
using GdalToolsLib.Models;
using OSGeo.OGR;
using Xunit;

namespace GdalToolsTest.Models;

public class PolygonComparisonAccessTests
{
    [Theory]
    [InlineData("POLYGON ((0 0,10 0,10 10,0 10,0 0))", 0)]
    [InlineData("POLYGON ((5 0,15 0,15 10,5 10,5 0))", 100)]
    public void SymmetricDifferenceAreaMeasuresChangedSurface(string candidateWkt, double expectedArea)
    {
        using var initialization = new OgctDataSourceAccessor().CreateAndOpenInMemoryDatasource();
        using var master = new OgctGeometry(Geometry.CreateFromWkt("POLYGON ((0 0,10 0,10 10,0 10,0 0))"));
        using var candidate = new OgctGeometry(Geometry.CreateFromWkt(candidateWkt));
        master.SymmetricDifferenceArea(candidate).Should().Be(expectedArea);
    }

    [Theory]
    [InlineData("POLYGON EMPTY")]
    [InlineData("POLYGON ((0 0,10 10,10 0,0 10,0 0))")]
    [InlineData("POINT (0 0)")]
    public void ValidatePolygonRejectsUnusableGeometries(string wkt)
    {
        using var initialization = new OgctDataSourceAccessor().CreateAndOpenInMemoryDatasource();
        using var geometry = new OgctGeometry(Geometry.CreateFromWkt(wkt));
        Action validate = () => geometry.ValidatePolygon();
        validate.Should().Throw<InvalidOperationException>();
    }
}
