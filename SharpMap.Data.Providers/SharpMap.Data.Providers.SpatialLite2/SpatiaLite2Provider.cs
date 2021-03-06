#if DEBUG
//#define EXPLAIN
#endif
/*
 *  The attached / following is part of SharpMap.Data.Providers.SpatiaLite2
 *  SharpMap.Data.Providers.SpatiaLite2 is free software � 2008 Ingenieurgruppe IVV GmbH & Co. KG, 
 *  www.ivv-aachen.de; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: Felix Obermaier 2008
 *  
 *  This work is based on SharpMap.Data.Providers.Db by John Diss for 
 *  Newgrove Consultants Ltd, www.newgrove.com
 *  
 *  Other than that, this spatial data provider requires:
 *  - SharpMap by Rory Plaire, Morten Nielsen and others released under LGPL
 *    http://www.codeplex.com/SharpMap
 *    
 *  - GeoAPI.Net by Rory Plaire, Morten Nielsen and others released under LGPL
 *    http://www.codeplex.com/GeoApi
 *    
 *  - SqLite, dedicated to public domain
 *    http://www.sqlite.org
 *  - SpatiaLite-2.2 by Alessandro Furieri released under a disjunctive tri-license:
 *    - 'Mozilla Public License, version 1.1 or later' OR
 *    - 'GNU General Public License, version 2.0 or later' 
 *    - 'GNU Lesser General Public License, version 2.1 or later' <--- this is the one
 *    http://www.gaia-gis.it/spatialite-2.2/index.html
 *    
 *  - SQLite ADO.NET 2.0 Provider by Robert Simpson, dedicated to public domain
 *    http://sourceforge.net/projects/sqlite-dotnet2
 *    
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.DataStructures;
using GeoAPI.Geometries;
using Proj4Utility;
using SharpMap.Data.Providers.Db;
using SharpMap.Data.Providers.Db.Expressions;
using SharpMap.Data.Providers.SpatiaLite2;
using SharpMap.Expressions;
using SharpMap.Utilities.SridUtility;
#if DEBUG
#endif
#if DOTNET35
using Processor = System.Linq.Enumerable;
using Enumerable = System.Linq.Enumerable;
using Caster = System.Linq.Enumerable;
#else

#endif

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// Enumeration of spatial indices valid for SQLite
    /// </summary>
    public enum SpatiaLite2IndexType
    {
        /// <summary>
        /// No valid spatial Index
        /// </summary>
        None = 0,

        /// <summary>
        /// RTree Index
        /// </summary>
        RTree = 1,

        /// <summary>
        /// In-Memory cache of Minimum Bounding Rectangles (MBR)
        /// </summary>
        MBRCache = 2
    }

    /// <summary>
    /// Enumeration of <see cref="SpatiaLite2"/> valid shapes
    /// </summary>
    public enum SpatiaLite2ShapeType
    {
        /// <summary>
        /// Geometry type has not been set.
        /// </summary>
        _Undefined = 0,

        /// <summary>
        /// A point.
        /// </summary>
        /// <remarks>
        /// A point consists of a Double-precision coordinate in 2D space.
        /// SharpMap interprets this as <see cref="IPoint"/>.
        /// </remarks>
        Point = 1,

        /// <summary>
        /// A connected line segment or segments.
        /// </summary>
        /// <remarks>
        /// LineString is an ordered set of vertices that consists of one part. 
        /// A part is a connected sequence of two or more points. 
        /// SharpMap interprets this as <see cref="ILineString"/>. 
        /// </remarks>
        LineString = 3,

        /// <summary>
        /// A connected line segment or segments.
        /// </summary>
        /// <remarks>
        /// MultiLineString is an ordered set of vertices that consists of several parts. 
        /// A part is a connected sequence of two or more points. Parts may or may not 
        /// be connected to one another. Parts may or may not intersect one another.
        /// SharpMap interprets this as <see cref="IMultiLineString"/>.
        /// </remarks>
        MultiLineString = 4,

        /// <summary>
        /// A connected line segment with at least one closure.
        /// </summary>
        /// <remarks>
        /// A polygon consists of one or more rings. A ring is a connected sequence of four or more
        /// points that form a closed, non-self-intersecting loop. A polygon may contain multiple
        /// outer rings. The order of vertices or orientation for a ring indicates which side of the ring
        /// is the interior of the polygon. The neighborhood to the right of an observer walking along
        /// the ring in vertex order is the neighborhood inside the polygon. Vertices of rings defining
        /// holes in polygons are in a counterclockwise direction. Vertices for a single, ringed
        /// polygon are, therefore, always in clockwise order. The rings of a polygon are referred to
        /// as its parts.
        /// SharpMap interprets this as either <see cref="IPolygon"/> 
        /// or <see cref="IMultiPolygon"/>.
        /// </remarks>
        Polygon = 5,

        MultiPolygon = 6,

        /// <summary>
        /// A set of <see cref="ShapeType.Point">points</see>.
        /// </summary>
        /// <remarks>
        /// SharpMap interprets this as <see cref="IMultiPoint"/>.
        /// </remarks>
        MultiPoint = 8,

        GeometryCollection = 31
    }

    public class SpatiaLite2Provider
        : SpatialDbProviderBase<Int64>, ISpatialDbProvider<SpatiaLite2DbUtility>
    {
        #region Static Properties

        static SpatiaLite2Provider()
        {
            //AddDerivedProperties(typeof(SpatialDbProviderBase<Int64>));
            AddDerivedProperties(typeof (SpatiaLite2Provider));
        }

        /// <summary>
        /// Gets a <see cref="PropertyDescriptor"/> for 
        /// <see cref="SpatiaLiteProvider2_2"/>'s <see cref="DefaultSpatiaLiteIndexType"/> property.
        /// </summary>
        public static PropertyDescriptor DefaultSpatiaLiteIndexTypeProperty
        {
            get { return ProviderStaticProperties.Find("DefaultSpatiaLiteIndexType", true); }
        }

        /// <summary>
        /// Gets a <see cref="PropertyDescriptor"/> for 
        /// <see cref="SpatiaLiteProvider2_2"/>'s <see cref="DefaultSRID"/> property.
        /// </summary>
        public static PropertyDescriptor DefaultSRIDProperty
        {
            get { return ProviderStaticProperties.Find("DefaultSRID", false); }
        }

        /// <summary>
        /// Gets a <see cref="PropertyDescriptor"/> for 
        /// <see cref="SpatiaLiteProvider2_2"/>'s <see cref="DefaultGeometryColumnName"/> property.
        /// </summary>
        public static PropertyDescriptor DefaultGeometryColumnNameProperty
        {
            get { return ProviderStaticProperties.Find("DefaultGeometryColumnName", false); }
        }

        /// <summary>
        /// Gets a <see cref="PropertyDescriptor"/> for 
        /// <see cref="SpatiaLiteProvider2_2"/>'s <see cref="DefaultGeometryColumnName"/> property.
        /// </summary>
        public static PropertyDescriptor DefaultOIDColumnNameProperty
        {
            get { return ProviderStaticProperties.Find("DefaultOIDColumnNameProperty", false); }
        }

        #endregion

        public static string DefaultAuthority = "EPSG";

        /// <summary>
        /// Default name of the geometry column. Used in SpatiaLite constructor
        /// if no geometry column is specified.
        /// </summary>
        public static String DefaultGeometryColumnName = "XGeometryX";

        /// <summary>
        /// Default name of PrimaryKey column. Used in SpatialLite constructor
        /// if not primary key column name is specified.
        /// </summary>
        public static String DefaultOidColumnName = "OID";

        /// <summary>
        /// Default SpatiaLiteIndexType. Used in SpatialLite constructor
        /// if not spatial index type is specified.
        /// </summary>
        public static SpatiaLite2IndexType DefaultSpatiaLiteIndexType = SpatiaLite2IndexType.RTree;

        /// <summary>
        /// SpatiaLite does not accept geometries without a valid SRID
        /// Used in SpatiaLite constructor if not SRID is specified.
        /// (e.g. 4326: 'WGS 84', '+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs')
        /// </summary>
        public static int DefaultSridInt = 4326;

        private SpatiaLite2IndexType _spatialLiteIndexType;

        /// <summary>
        /// Spatialite tables only accept geometries specified for the geometry column
        /// Look for entry in geometry_columns table of sqlite-db file
        /// </summary>
        private SpatiaLite2ShapeType _validGeometryType = SpatiaLite2ShapeType._Undefined;

        public SpatiaLite2Provider(IGeometryFactory geometryFactory, string connectionString, string tableName)
            : this(geometryFactory, connectionString, "main", tableName, DefaultOidColumnName, DefaultGeometryColumnName
                )
        {
        }

        public SpatiaLite2Provider(IGeometryFactory geometryFactory, string connectionString,
                                   string tableSchema, string tableName, string oidColumn, string geometryColumn)
            : this(geometryFactory, connectionString, tableSchema, tableName, oidColumn, geometryColumn, null)
        {
        }

        public SpatiaLite2Provider(IGeometryFactory geometryFactory, string connectionString,
                                   string tableSchema, string tableName, string oidColumn, string geometryColumn,
                                   ICoordinateTransformationFactory coordinateTransformationFactory)
            : base(
                new SpatiaLite2DbUtility(), geometryFactory, connectionString, tableSchema, tableName, oidColumn,
                geometryColumn, coordinateTransformationFactory)
        {
            using (SQLiteConnection cn = new SQLiteConnection(connectionString))
            {
                cn.Open();
                try
                {
                    SQLiteCommand cmd = new SQLiteCommand(cn);
                    cmd.CommandText =
                        @"SELECT [type], [spatial_index_enabled] FROM [geometry_columns] WHERE ([f_table_name]=@p1 AND [f_geometry_column]=@p2)";
                    cmd.Parameters.AddWithValue("@p1", tableName);
                    cmd.Parameters.AddWithValue("@p2", geometryColumn);

                    SQLiteDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        dr.Read();

                        //valid geometry type
                        _validGeometryType = parseGeometryType(dr.GetString(0));

                        //SpatiaLite Index
                        switch (dr.GetInt64(1))
                        {
                            case 0:
                                //throw new SpatiaLite2Exception( "Spatial index type must not be 'None'" );
                                _spatialLiteIndexType = SpatiaLite2IndexType.None;
                                break;
                            case 1:
                                _spatialLiteIndexType = SpatiaLite2IndexType.RTree;
                                break;
                            case 2:
                                _spatialLiteIndexType = SpatiaLite2IndexType.MBRCache;
                                break;
                            default:
                                throw new SpatiaLite2Exception("Unknown SpatiaLite index type.");
                        }
                    }
                }
                catch (Exception)
                {
                    _validGeometryType = SpatiaLite2ShapeType._Undefined;
                }
            }
        }

        public static string DefaultSrid
        {
            get { return string.Format("{0}:{1}", DefaultAuthority, DefaultSridInt); }
        }

        public SpatiaLite2ShapeType ValidGeometryType
        {
            get { return _validGeometryType; }
        }

        public override string GeometryColumnConversionFormatString
        {
            get { return "AsBinary({0})"; }
        }

        public override string GeomFromWkbFormatString
        {
            get { return string.Format("GeomFromWKB({0},{1})", "{0}", SridInt.HasValue ? SridInt : DefaultSridInt); }
        }

        public SpatiaLite2IndexType SpatiaLiteIndexType
        {
            get
            {
                return _spatialLiteIndexType;
                //Int64 retVal = 0;
                //using (SQLiteConnection cn = (SQLiteConnection)DbUtility.CreateConnection(ConnectionString))
                //{
                //    //cn.Open();
                //    Object ret = new SQLiteCommand(string.Format("SELECT spatial_index_enabled FROM geometry_columns WHERE (f_table_name='{0}' AND f_geometry_column='{1}');", Table, GeometryColumn), cn).ExecuteScalar();
                //    if (ret != null) retVal = (long)ret;
                //}
                //switch(retVal)
                //{
                //    case 0: return SpatiaLite2IndexType.None;
                //    case 1: return SpatiaLite2IndexType.RTree;
                //    case 2: return SpatiaLite2IndexType.MBRCache;
                //    default:
                //        throw new SpatiaLite2Exception("Unknown spatial index type");
                //}
            }
            set
            {
                //if ( value == SpatiaLite2IndexType.None ) return;

                Object ret = 0;
                long retVal = 0;
                if (_spatialLiteIndexType != value)
                {
                    using (SQLiteConnection cn = (SQLiteConnection) DbUtility.CreateConnection(ConnectionString))
                    {
                        //First disable current spatial index
                        ret =
                            new SQLiteCommand(
                                string.Format("SELECT DisableSpatialIndex( '{0}', '{1}' )", Table, GeometryColumn), cn).
                                ExecuteScalar();

                        if (value == SpatiaLite2IndexType.RTree)
                            ret =
                                new SQLiteCommand(
                                    string.Format("SELECT CreateSpatialIndex( '{0}', '{1}' );", Table, GeometryColumn),
                                    cn).ExecuteScalar();
                        if (value == SpatiaLite2IndexType.MBRCache)
                            ret =
                                new SQLiteCommand(
                                    string.Format("SELECT CreateMBRCache( '{0}', '{1}' );", Table, GeometryColumn), cn).
                                    ExecuteScalar();

                        Debug.Assert(ret != null);
                        retVal = (long) ret;
                        Debug.Assert(retVal == 1);
                    }
                    _spatialLiteIndexType = (retVal == 1) ? value : SpatiaLite2IndexType.None;
                }
            }
        }

        #region ISpatialDbProvider<SpatiaLite2DbUtility> Members

        public new SpatiaLite2DbUtility DbUtility
        {
            get { return (SpatiaLite2DbUtility) base.DbUtility; }
        }

        #endregion

        /// <summary>
        /// Determines whether SqLite database file is spatially enabled
        /// </summary>
        /// <param name="connectionString">Connection String to access SqLite database file</param>
        /// <returns>
        /// <value>true</value> if it is,
        /// <value>false</value> if it isn't.
        /// </returns>
        public static Boolean IsSpatiallyEnabled(String connectionString)
        {
            Boolean result = false;
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                Int64 numTables =
                    (Int64) new SQLiteCommand(String.Format(
                                                  "SELECT COUNT(*) FROM [{0}].[sqlite_master] " +
                                                  "WHERE([tbl_name]='spatial_ref_sys' OR [tbl_name]='geometry_columns');",
                                                  "main")
                                              , conn).ExecuteScalar();

                if (numTables >= 2)
                {
                    Int64 numRefSys =
                        (Int64) new SQLiteCommand("SELECT COUNT(*) FROM spatial_ref_sys;", conn).ExecuteScalar();

                    result = (numRefSys > 0);
                }
            }
            return result;
        }

        public static DataSet GetSpatiallyEnabledTables(String connectionString)
        {
            DataSet ds = null;

            using (SQLiteConnection cn = new SQLiteConnection(connectionString))
            {
                cn.Open();

                createSQLiteInformationSchema(cn);

                SQLiteCommand cm = cn.CreateCommand();
                cm.CommandText =
                    @"
SELECT	'main' AS [Schema],
        x.f_table_name AS [TableName], 
        x.f_geometry_column AS [GeometryColumn], 
        x.coord_dimension AS [Dimension], 
	    x.f_table_name || '.' || x.f_geometry_column || ' (' || x.type || ')' AS [Label],
        y.auth_name || ':' || y.auth_srid AS [SRID],
        y.srtext as [SpatialReference]
FROM [main].[geometry_columns] AS x LEFT JOIN [main].[spatial_ref_sys] as y on x.srid=y.srid;
TYPES [varchar], [varchar], [varchar], [bool];
SELECT  x.table_name as [TableName],
        x.column_name as [ColumnName],
        x.data_type as [DataType],
        -1 AS [Include],
        CASE
            WHEN (x.PK = 1) THEN -1
            ELSE 0
        END AS [PK]
FROM    information_schema_columns AS x 
	        LEFT JOIN (SELECT z.f_table_name FROM geometry_columns AS z) AS cte ON cte.f_table_name=x.table_name
	        LEFT JOIN geometry_columns AS y ON y.f_table_name=x.table_name
WHERE cte.f_table_name=x.table_name AND ( RTRIM(x.column_name) <> RTRIM(y.f_geometry_column))
ORDER BY [TableName], x.ordinal_position;";

                SQLiteDataAdapter da = new SQLiteDataAdapter(cm);
                ds = new DataSet();
                da.Fill(ds);
#if DEBUG
                Debug.Assert(ds.Tables.Count == 2);
#endif
            }
            return ds;
        }

        private static void createSQLiteInformationSchema(SQLiteConnection connection)
        {
            new SQLiteCommand(
                @"CREATE TEMP TABLE information_schema_columns (
    table_name text, 
    ordinal_position integer,
    column_name text,
    data_type text,
    pk integer);",
                connection).ExecuteNonQuery();

            SQLiteCommand insert = new SQLiteCommand(
                "INSERT INTO information_schema_columns (table_name, ordinal_position, column_name, data_type, pk) VALUES(@P1, @P4, @P2, @P3, @P5);",
                connection);

            insert.Parameters.Add(new SQLiteParameter("@P1", DbType.String));
            insert.Parameters.Add(new SQLiteParameter("@P2", DbType.String));
            insert.Parameters.Add(new SQLiteParameter("@P3", DbType.String));
            insert.Parameters.Add(new SQLiteParameter("@P4", DbType.Int64));
            insert.Parameters.Add(new SQLiteParameter("@P5", DbType.Int64));


            //Pragma
            using (SQLiteConnection cn_pragma = (SQLiteConnection) connection.Clone())
            {
                object retval =
                    new SQLiteCommand("SELECT load_extension('libspatialite-2.dll')", cn_pragma).ExecuteScalar();

                SQLiteCommand pragma = new SQLiteCommand("PRAGMA table_info('@P1');", cn_pragma);
                //pragma.CommandType = CommandType.StoredProcedure;
                pragma.Parameters.Add("P1", DbType.String);

                //itereate throuhg sqlite_master
                using (SQLiteConnection cn_master = (SQLiteConnection) connection.Clone())
                {
                    //cn_master.Open();
                    string select =
                        @"
SELECT tbl_name 
FROM sqlite_master 
WHERE type='table' AND NOT( name like 'cache_%' ) AND NOT( name like 'sqlite%' ) AND NOT( name like 'index_%' ) AND NOT (name in ('spatial_ref_sys', 'geometry_columns'));";
                    SQLiteDataReader dr = new SQLiteCommand(
                        select, cn_master
                        ).ExecuteReader(CommandBehavior.CloseConnection);

                    while (dr.Read())
                    {
                        //Get Column Info and ...
                        String tableName = dr.GetString(0).Trim();
                        pragma = new SQLiteCommand(string.Format("PRAGMA table_info('{0}');", tableName), cn_pragma);
                            //.Parameters[0].Value = dr.GetString(0);
                        using (SQLiteDataReader drts = pragma.ExecuteReader())
                        {
                            while (drts.Read())
                            {
                                //... insert it into temp table
                                insert.Parameters[0].Value = tableName;
                                ;
                                insert.Parameters[1].Value = drts.GetString(1);
                                insert.Parameters[2].Value = drts.GetString(2);
                                insert.Parameters[3].Value = drts.GetInt64(0);
                                insert.Parameters[4].Value = drts.GetInt64(5);
                                insert.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        private SpatiaLite2ShapeType parseGeometryType(String geometryString)
        {
            if (String.IsNullOrEmpty(geometryString))
                throw new SpatiaLite2Exception("Geometry type not specified!");

            switch (geometryString.ToUpper())
            {
                case "POINT":
                    return SpatiaLite2ShapeType.Point;
                case "LINESTRING":
                    return SpatiaLite2ShapeType.LineString;
                case "POLYGON":
                    return SpatiaLite2ShapeType.Polygon;
                case "MULTIPOINT":
                    return SpatiaLite2ShapeType.MultiPoint;
                case "MULTILINESTRING":
                    return SpatiaLite2ShapeType.MultiLineString;
                case "MULTIPOLYGON":
                    return SpatiaLite2ShapeType.MultiPolygon;
                case "GEOMETRYCOLLECTION":
                    return SpatiaLite2ShapeType.GeometryCollection;
                default:
                    throw new SpatiaLite2Exception(string.Format("Invalid geometry type '{0}'", geometryString));
            }
        }

        public override IExtents GetExtents()
        {
            using (IDbConnection conn = DbUtility.CreateConnection(ConnectionString))
            using (IDbCommand cmd = DbUtility.CreateCommand())
            {
                cmd.Connection = conn;
                cmd.CommandText = string.Format(
                    "SELECT MIN(MbrMinX({0})) as xmin, MIN(MbrMinY({0})) as ymin, MAX(MbrMaxX({0})) as xmax, MAX(MbrMaxY({0})) as maxy from {1};",
                    GeometryColumn, QualifiedTableName);
                Double xmin, ymin, xmax, ymax;

                using (SQLiteDataReader r = (SQLiteDataReader) cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    if (r.HasRows)
                    {
                        r.Read();

                        if (r.IsDBNull(0) || r.IsDBNull(1) || r.IsDBNull(2) || r.IsDBNull(3))
                            return GeometryFactory.CreateExtents();

                        xmin = r.GetDouble(0); // - 0.000000000001;
                        ymin = r.GetDouble(1); // - 0.000000000001;
                        xmax = r.GetDouble(2); // + 0.000000000001;
                        ymax = r.GetDouble(3); // + 0.000000000001;

                        return GeometryFactory.CreateExtents2D(xmin, ymin, xmax, ymax);
                    }
                }
            }
            return GeometryFactory.CreateExtents();
        }

        protected override DataTable BuildSchemaTable()
        {
            return BuildSchemaTable(false);
        }

        protected override DataTable BuildSchemaTable(Boolean withGeometryColumn)
        {
            DataTable dt = null;
            using (SQLiteConnection conn = (SQLiteConnection) DbUtility.CreateConnection(ConnectionString))
            {
                CollectionExpression<PropertyNameExpression> attributes = null;
                if (DefaultProviderProperties != null)
                {
                    attributes = GetProviderPropertyValue
                        <AttributesCollectionExpression, CollectionExpression<PropertyNameExpression>>(
                        DefaultProviderProperties.ProviderProperties,
                        null);
                }

                string columns = attributes == null
                                     ?
                                         "*"
                                     :
                                         string.Join(",", Enumerable.ToArray(Processor.Select(attributes,
                                                                                              delegate(
                                                                                                  PropertyNameExpression
                                                                                                  o)
                                                                                                  {
                                                                                                      return
                                                                                                          QualifyColumnName
                                                                                                              (
                                                                                                              o.
                                                                                                                  PropertyName);
                                                                                                  })));

                if (columns != "*")
                {
                    if (!columns.Contains(QualifyColumnName(GeometryColumn)))
                        columns = string.Format("{0},{1}", QualifyColumnName(GeometryColumn), columns);
                    if (!columns.Contains(QualifyColumnName(OidColumn)))
                        columns = string.Format("{0},{1}", QualifyColumnName(OidColumn), columns);
                }

                using (
                    SQLiteCommand cmd =
                        new SQLiteCommand(string.Format("SELECT {0} FROM {1} LIMIT 1;", columns, QualifiedTableName),
                                          conn))
                {
                    cmd.Connection = conn;
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.FillSchema(ds, SchemaType.Source);
                    dt = ds.Tables["Table"];
                }

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (dt.Columns[i].DataType == typeof (Object))
                        dt.Columns[i].DataType = typeof (Byte[]);
                    //replaceObjectDataColumn(dt, dt.Columns[i]);
                }

                if (!withGeometryColumn)
                {
                    dt.Columns.Remove(GeometryColumn);
                }
                dt.PrimaryKey = null;
            }
            return dt;
        }

        protected override ExpressionTreeToSqlCompilerBase<Int64> CreateSqlCompiler(Expression expression)
        {
            return new SpatiaLite2ExpressionTreeToSqlCompiler(this,
                                                              expression,
                                                              _spatialLiteIndexType);
        }

        public void Vacuum()
        {
            using (SQLiteConnection cn = new SQLiteConnection(ConnectionString))
            {
                cn.Open();
                try
                {
                    new SQLiteCommand("VACUUM;", cn).ExecuteNonQuery();
                }
                finally
                {
                }
            }
        }


        //public override OgcGeometryType ValidGeometryType
        //{
        //    get { return _validGeometryType; }
        //    protected set
        //    {
        //        if (_validGeometryType == OgcGeometryType.Unknown)
        //            _validGeometryType = value;
        //    }
        //}

        private static SQLiteConnection initSpatialMetaData(String connectionString)
        {
            //Test whether database is spatially enabled
            bool spatiallyEnabled = IsSpatiallyEnabled(connectionString);

            SQLiteConnection conn = new SQLiteConnection(connectionString);
            conn.Open();

            Object retVal = new SQLiteCommand("SELECT load_extension('libspatialite-2.dll');", conn).ExecuteScalar();
            if (spatiallyEnabled) return conn;

            SQLiteTransaction tran = conn.BeginTransaction();

            new SQLiteCommand("SELECT InitSpatialMetaData();", conn).ExecuteNonQuery();

            new SQLiteCommand("ALTER TABLE spatial_ref_sys ADD COLUMN srtext text;", conn).ExecuteNonQuery();

            SQLiteCommand cmd = new SQLiteCommand(
                "INSERT OR REPLACE INTO spatial_ref_sys (srid, auth_name, auth_srid, ref_sys_name, proj4text, srtext) VALUES (@P1, @P2, @P3, @P4, @P5, @P6);",
                conn);
            cmd.Parameters.Add(new SQLiteParameter("@P1", DbType.Int64));
            cmd.Parameters.Add(new SQLiteParameter("@P2", DbType.String));
            cmd.Parameters.Add(new SQLiteParameter("@P3", DbType.Int64));
            cmd.Parameters.Add(new SQLiteParameter("@P4", DbType.String));
            cmd.Parameters.Add(new SQLiteParameter("@P5", DbType.String));
            cmd.Parameters.Add(new SQLiteParameter("@P6", DbType.String));

            SQLiteParameterCollection pars = cmd.Parameters;
            foreach (Proj4Reader.Proj4SpatialRefSys p4srs in Proj4Reader.GetSRIDs())
            {
                pars[0].Value = p4srs.Srid;
                pars[1].Value = p4srs.AuthorityName;
                pars[2].Value = p4srs.AuthoritySrid;
                pars[3].Value = p4srs.RefSysName;
                pars[4].Value = p4srs.Proj4Text;
                pars[5].Value = p4srs.SrText;

                cmd.ExecuteNonQuery();
            }

            tran.Commit();

            return conn;
        }

        public static void CreateDataTable(FeatureDataTable featureDataTable, String connectionString)
        {
            CreateDataTable(featureDataTable, featureDataTable.TableName, connectionString);
        }

        public static void CreateDataTable(FeatureDataTable featureDataTable, String tableName, String connectionString)
        {
            CreateDataTable(featureDataTable, tableName, connectionString,
                            DefaultGeometryColumnName, SpatiaLite2ShapeType._Undefined, DefaultSpatiaLiteIndexType);
        }

        public static void CreateDataTable(
            FeatureDataTable featureDataTable,
            String tableName,
            String connectionString,
            String geometryColumnName,
            SpatiaLite2ShapeType shapeType,
            SpatiaLite2IndexType spatialIndexType)
        {
            //if ( spatialIndexType == SpatiaLite2IndexType.None )
            //    spatialIndexType = DefaultSpatiaLiteIndexType;

            SQLiteConnection conn = initSpatialMetaData(connectionString);

            string srid = featureDataTable.GeometryFactory.SpatialReference != null
                              ?
                                  featureDataTable.GeometryFactory.SpatialReference.AuthorityCode
                              :
                                  DefaultSridInt.ToString();

            if (conn != null)
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                string createTableClause = string.Format("CREATE TABLE IF NOT EXISTS {0} ({1});", tableName,
                                                         ColumnsClause(featureDataTable.Columns,
                                                                       featureDataTable.Constraints));
                new SQLiteCommand(createTableClause, conn).ExecuteNonQuery();

                if (shapeType == SpatiaLite2ShapeType._Undefined)
                    shapeType = ToSpatiaLite2ShapeType(featureDataTable[0].Geometry.GeometryType);

                if (shapeType == SpatiaLite2ShapeType._Undefined)
                    throw new SpatiaLite2Exception(string.Format(
                                                       "Cannot add or recover geometry column with {0}-type.",
                                                       featureDataTable[0].Geometry.GeometryType.ToString()));

                String addGeometryColumnClause = String.Format("('{0}', '{1}', {2}, '{3}', {4})",
                                                               tableName,
                                                               geometryColumnName,
                                                               srid,
                                                               shapeType.ToString(),
                                                               2);

                if (
                    (Int64)
                    new SQLiteCommand(String.Format("SELECT RecoverGeometryColumn {0}", addGeometryColumnClause), conn).
                        ExecuteScalar() == (Int64) 0)
                    if (
                        (Int64)
                        new SQLiteCommand(String.Format("SELECT AddGeometryColumn {0};", addGeometryColumnClause), conn)
                            .ExecuteScalar() == (Int64) 0)
                        throw new SpatiaLite2Exception(string.Format(
                                                           "Cannot create geometry column with type of '{0}'",
                                                           shapeType.ToString()));

                switch (spatialIndexType)
                {
                    case SpatiaLite2IndexType.RTree:
                        if (new SQLiteCommand(String.Format("SELECT CreateSpatialIndex('{0}','{1}');",
                                                            tableName, geometryColumnName), conn).ExecuteScalar() ==
                            (object) 0) throw new SpatiaLite2Exception("Could not create RTree index");
                        break;

                    case SpatiaLite2IndexType.MBRCache:
                        if (new SQLiteCommand(String.Format("SELECT CreateMbrCache('{0}','{1}');",
                                                            tableName, geometryColumnName), conn).ExecuteScalar() ==
                            (object) 0) throw new SpatiaLite2Exception("Could not create MbrCache");
                        break;
                }
            }
            conn.Close();
            conn = null;

            SpatiaLite2Provider prov = new SpatiaLite2Provider(
                featureDataTable.GeometryFactory, connectionString, "main", tableName,
                featureDataTable.Columns[0].ColumnName, geometryColumnName);
            prov.Insert(featureDataTable);

            return;
        }

        private static SpatiaLite2ShapeType ToSpatiaLite2ShapeType(OgcGeometryType ogcGeometryType)
        {
            switch (ogcGeometryType)
            {
                case OgcGeometryType.Point:
                    return SpatiaLite2ShapeType.Point;
                case OgcGeometryType.MultiPoint:
                    return SpatiaLite2ShapeType.MultiPoint;

                case OgcGeometryType.LineString:
                    return SpatiaLite2ShapeType.LineString;
                case OgcGeometryType.MultiLineString:
                    return SpatiaLite2ShapeType.MultiLineString;

                case OgcGeometryType.Polygon:
                    return SpatiaLite2ShapeType.Polygon;
                case OgcGeometryType.MultiPolygon:
                    return SpatiaLite2ShapeType.MultiPolygon;

                case OgcGeometryType.GeometryCollection:
                    return SpatiaLite2ShapeType.GeometryCollection;

                default:
                    return SpatiaLite2ShapeType._Undefined;
            }
        }

        public override void Insert(IEnumerable<FeatureDataRow<Int64>> features)
        {
            //SpatiaLite2ShapeType geometryType = SpatiaLite2ShapeType._Undefined;

            using (IDbConnection conn = DbUtility.CreateConnection(ConnectionString))
            {
                if (conn.State == ConnectionState.Closed) conn.Open();
                {
                    using (IDbTransaction tran = conn.BeginTransaction())
                    {
                        IDbCommand cmd = DbUtility.CreateCommand();
                        cmd.Connection = conn;
                        cmd.Transaction = tran;

                        cmd.CommandText = string.Format(
                            "INSERT INTO {0} {1};", QualifiedTableName, InsertClause(cmd));

                        foreach (FeatureDataRow row in features)
                        {
                            for (int i = 0; i < cmd.Parameters.Count - 1; i++)
                                ((IDataParameter) cmd.Parameters[i]).Value = row[i];

                            ((IDataParameter) cmd.Parameters["@PGeo"]).Value = row.Geometry.AsBinary();
                            if (_validGeometryType == SpatiaLite2ShapeType._Undefined)
                                _validGeometryType = ToSpatiaLite2ShapeType(row.Geometry.GeometryType);

                            if (ToSpatiaLite2ShapeType(row.Geometry.GeometryType) == _validGeometryType)
                                cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                    }
                }
            }
        }

        public override void Update(IEnumerable<FeatureDataRow<Int64>> features)
        {
            //Update(features, "UPDATE OR IGNORE");
        }

        private static String ColumnsClause(DataColumnCollection dcc, ConstraintCollection ccc)
        {
            String[] columns = new String[dcc.Count];

            Int32 index = 0;
            foreach (DataColumn dc in dcc)
            {
                columns[index++] = string.Format(" [{0}] {1}", dc.ColumnName,
                                                 SpatiaLite2DbUtility.GetTypeString(dc.DataType));
            }
            index = 0;

            String[] constraints = new String[ccc.Count];
            foreach (Constraint c in ccc)
            {
                UniqueConstraint uc = c as UniqueConstraint;
                if (uc != null)
                {
                    if (uc.IsPrimaryKey)
                    {
                        constraints[index++] = String.Format(", PRIMARY KEY ({0}) ON CONFLICT IGNORE",
                                                             ColumnNamesToCommaSeparatedString(uc.Columns));
                    }
                    else
                    {
                        constraints[index++] = String.Format(", UNIQUE ({0}) ON CONFLICT IGNORE",
                                                             ColumnNamesToCommaSeparatedString(uc.Columns));
                    }
                }
                //Other Constraints are not supported by SqLite
            }

            String constraintsClause = "";
            if (index > 0)
            {
                Array.Resize<String>(ref constraints, index);
                constraintsClause = String.Join(String.Empty, constraints);
            }
            return String.Join(",", columns) + constraintsClause;
        }

        private static String OrdinalsToCommaSeparatedString(IEnumerable<DataColumn> dcc)
        {
            return OrdinalsToCommaSeparatedString(String.Empty, dcc);
        }

        private static String OrdinalsToCommaSeparatedString(String prefix, IEnumerable dcc)
        {
            String ret = "";
            foreach (DataColumn t in dcc)
                ret += String.Format(", {0}{1}", prefix, t.Ordinal);

            if (ret.Length > 0)
                ret = ret.Substring(2);

            return ret;
        }

        private static String ColumnNamesToCommaSeparatedString(IEnumerable<DataColumn> dcc)
        {
            return ColumnNamesToCommaSeparatedString(String.Empty, dcc);
        }

        private static String ColumnNamesToCommaSeparatedString(String prefix, IEnumerable<DataColumn> dcc)
        {
            String ret = "";
            foreach (DataColumn t in dcc)
                ret += String.Format(", [{0}]", t.ColumnName);

            if (ret.Length > 0)
                ret = ret.Substring(2);

            return ret;
        }

        protected override string GenerateSelectSql(IList<ProviderPropertyExpression> properties,
                                                    ExpressionTreeToSqlCompilerBase<long> compiler)
        {
            int pageNumber = GetProviderPropertyValue<DataPageNumberExpression, int>(properties, -1);
            int pageSize = GetProviderPropertyValue<DataPageSizeExpression, int>(properties, 0);

            string sql = "";
            if (pageSize > 0 && pageNumber > -1)
                sql = GenerateSelectSql(properties, compiler, pageSize, pageNumber);
            else
            {
                string mainQueryColumns = string.Join(",", Enumerable.ToArray(
                                                               FormatColumnNames(true, true,
                                                                                 compiler.ProjectedColumns.Count > 0
                                                                                     ? compiler.ProjectedColumns
                                                                                     : SelectAllColumnNames())));

                //string orderByCols = String.Join(",",
                //                                 Enumerable.ToArray(Processor.Select(
                //                                                        GetProviderPropertyValue
                //                                                            <OrderByCollectionExpression,
                //                                                            CollectionExpression<OrderByExpression>>(
                //                                                            properties,
                //                                                            new CollectionExpression<OrderByExpression>(
                //                                                                new OrderByExpression[] {})),
                //                                                        delegate(OrderByExpression o) { return o.ToString("[{0}]"); })));

                string orderByClause = string.IsNullOrEmpty(compiler.OrderByClause) ? "" : " ORDER BY " + compiler.OrderByClause;

                sql =
                    String.Format("SELECT {0} FROM {1} {2} {3} {4} {5}",
                                  mainQueryColumns,
                                  Table, //QualifiedTableName,
                                  compiler.SqlJoinClauses,
                                  String.IsNullOrEmpty(compiler.SqlWhereClause) ? "" : " WHERE ",
                                  compiler.SqlWhereClause,
                                  orderByClause);
            }
#if DEBUG && EXPLAIN
            using ( SQLiteConnection cn = (SQLiteConnection)DbUtility.CreateConnection( ConnectionString ) )
            {
                if ( cn.State == ConnectionState.Closed ) cn.Open();
                SQLiteCommand cm = new SQLiteCommand( String.Format( "EXPLAIN {0}", sql ), cn );
                foreach ( IDataParameter par in compiler.ParameterCache.Values )
                    cm.Parameters.Add( par );

                Debug.WriteLine( "" );
                SQLiteDataReader dr = cm.ExecuteReader();
                if ( dr.HasRows )
                    while ( dr.Read() )
                    {
                        Debug.WriteLine( String.Format( "{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7}",
                            dr.GetValue( 0 ), dr.GetValue( 1 ), dr.GetValue( 2 ), dr.GetValue( 3 ),
                            dr.GetValue( 4 ), dr.GetValue( 5 ), dr.GetValue( 6 ), dr.GetValue( 7 ) ) );
                    }
                Debug.WriteLine( "" );
            }
#endif
            return sql;
        }

        protected override string GenerateSelectSql(IList<ProviderPropertyExpression> properties,
                                                    ExpressionTreeToSqlCompilerBase<long> compiler, int pageSize,
                                                    int pageNumber)
        {
            //string orderByCols = String.Join(",",
            //                                 Enumerable.ToArray(Processor.Select(
            //                                                        GetProviderPropertyValue
            //                                                            <OrderByCollectionExpression,
            //                                                            CollectionExpression<OrderByExpression>>(
            //                                                            properties,
            //                                                            new CollectionExpression<OrderByExpression>(
            //                                                                new OrderByExpression[] {})),
            //                                                        delegate(OrderByExpression o) { return o.ToString("[{0}]"); })));

            string orderByClause = string.IsNullOrEmpty(compiler.OrderByClause) ? "ROWID" : " ORDER BY " + compiler.OrderByClause;

            int startRecord = (pageNumber*pageSize);
            int endRecord = (pageNumber + 1)*pageSize - 1;

            string mainQueryColumns = string.Join(",", Enumerable.ToArray(
                                                           FormatColumnNames(true, true,
                                                                             compiler.ProjectedColumns.Count > 0
                                                                                 ? compiler.ProjectedColumns
                                                                                 : SelectAllColumnNames()
                                                               )));

            string subQueryColumns = string.Join(",", Enumerable.ToArray(
                                                          FormatColumnNames(false, false,
                                                                            compiler.ProjectedColumns.Count > 0
                                                                                ? compiler.ProjectedColumns
                                                                                : SelectAllColumnNames()
                                                              )));


            String sql = String.Format(
                "CREATE TEMP TABLE tmp AS SELECT {0} FROM {1} {2} {3} {4} {5};" +
                "SELECT {6} FROM tmp WHERE ROWID BETWEEN {7} AND {8};",
                mainQueryColumns,
                QualifiedTableName,
                compiler.SqlJoinClauses,
                String.IsNullOrEmpty(compiler.SqlWhereClause) ? "" : " WHERE ", compiler.SqlWhereClause,
                orderByClause,
                subQueryColumns,
                compiler.CreateParameter(startRecord).ParameterName,
                compiler.CreateParameter(endRecord).ParameterName);

            return sql;
        }

        protected override void ReadSpatialReference(out ICoordinateSystem cs, out string srid)
        {
            using (IDbConnection conn = DbUtility.CreateConnection(ConnectionString))
            {
                using (IDbCommand cmd = DbUtility.CreateCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText =
//@"SELECT y.[srtext] FROM [spatial_ref_sys] as y 
                        @"SELECT y.[auth_name] || "":"" || y.[auth_srid] FROM [spatial_ref_sys] as y 
INNER JOIN [geometry_columns] as x ON x.[srid]=y.[srid]
WHERE (x.[f_table_name]=@p1 AND x.[f_geometry_column]=@p2)
LIMIT 1;";

                    cmd.Parameters.Add(DbUtility.CreateParameter("p1", Table, ParameterDirection.Input));
                    cmd.Parameters.Add(DbUtility.CreateParameter("p2", GeometryColumn, ParameterDirection.Input));

                    object result = cmd.ExecuteScalar();
                    if (result is string)
                    {
                        string ssrid = (string) result;
                        cs = SridMap.DefaultInstance.Process(ssrid, (ICoordinateSystem) null);
                        srid = !Equals(cs, default(ICoordinateSystem)) ? SridMap.DefaultInstance.Process(cs, "") : "";
                        return;
                    }
                }
            }
            cs = default(ICoordinateSystem);
            srid = "";
        }
    }
}