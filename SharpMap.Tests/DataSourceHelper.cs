using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using SharpMap.Geometries;
using SharpMap.Data.Providers;

namespace SharpMap.Tests
{

	public static class DataSourceHelper
	{
		public static IProvider CreateGeometryDatasource()
		{
			Collection<Geometry> geoms = new Collection<Geometry>();
			geoms.Add(Geometry.GeomFromText("POINT EMPTY"));
			geoms.Add(Geometry.GeomFromText("GEOMETRYCOLLECTION (POINT (10 10), POINT (30 30), LINESTRING (15 15, 20 20))"));
			geoms.Add(Geometry.GeomFromText("MULTIPOLYGON (((0 0, 10 0, 10 10, 0 10, 0 0)), ((5 5, 7 5, 7 7, 5 7, 5 5)))"));
			geoms.Add(Geometry.GeomFromText("LINESTRING (20 20, 20 30, 30 30, 30 20, 40 20)"));
			geoms.Add(Geometry.GeomFromText("MULTILINESTRING ((10 10, 40 50), (20 20, 30 20), (20 20, 50 20, 50 60, 20 20))"));
			geoms.Add(Geometry.GeomFromText("POLYGON ((20 20, 20 30, 30 30, 30 20, 20 20), (21 21, 29 21, 29 29, 21 29, 21 21), (23 23, 23 27, 27 27, 27 23, 23 23))"));
			geoms.Add(Geometry.GeomFromText("POINT (20.564 346.3493254)"));
			geoms.Add(Geometry.GeomFromText("MULTIPOINT (20.564 346.3493254, 45 32, 23 54)"));
			geoms.Add(Geometry.GeomFromText("MULTIPOLYGON EMPTY"));
			geoms.Add(Geometry.GeomFromText("MULTILINESTRING EMPTY"));
			geoms.Add(Geometry.GeomFromText("MULTIPOINT EMPTY"));
			geoms.Add(Geometry.GeomFromText("LINESTRING EMPTY"));
			return new GeometryProvider(geoms);
		}
	}
}
