// Copyright 2006, 2007 - Rory Plaire (codekaizen@gmail.com)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using SharpMap.Styles;
using SharpMap.Geometries;

namespace SharpMap.Rendering.Rendering2D
{
    /// <summary>
    /// A basic renderer which renders the geometric paths from feature geometry, 
    /// taking into account <see cref="Style">styles</see> and 
    /// <see cref="SharpMap.Rendering.Thematics.ITheme">themes</see>.
    /// </summary>
    /// <typeparam name="TRenderObject">Type of render object to generate.</typeparam>
	public class BasicGeometryRenderer2D<TRenderObject> : FeatureRenderer2D<VectorStyle, TRenderObject>, 
        IGeometryRenderer<Symbol2D,  PositionedRenderObject2D<TRenderObject>>
    {
        
        #region Type Members
        private static object _defaultSymbol;

        /// <summary>
        /// The default basic symbol for rendering point data.
        /// </summary>
        public static Symbol2D DefaultSymbol
        {
            get
            {
                if (Thread.VolatileRead(ref _defaultSymbol) == null)
                {
                    lock (_defaultSymbol)
                    {
						if (Thread.VolatileRead(ref _defaultSymbol) == null)
                        {
                            Stream data = Assembly.GetExecutingAssembly()
								.GetManifestResourceStream("SharpMap.Styles.DefaultSymbol.png");
                            Thread.VolatileWrite(ref _defaultSymbol, new Symbol2D(data, new Size2D(16, 16)));
                        }
                    }
                }

				return _defaultSymbol as Symbol2D;
            }
        }

		// DON'T remove - this eliminates the .beforefieldinit IL metadata
        static BasicGeometryRenderer2D() { }
        #endregion

		#region Object construction and disposal
		public BasicGeometryRenderer2D(VectorRenderer2D<TRenderObject> vectorRenderer)
            : base(vectorRenderer)
		{
		}

        ~BasicGeometryRenderer2D()
        {
            Dispose(false);
		}
		#endregion

		/// <summary>
        /// Renders the geometry of the <paramref name="feature"/>.
        /// </summary>
        /// <param name="feature">The feature to render.</param>
        /// <param name="style">The style to use to render the feature.</param>
        /// <returns>An enumeration of positioned render objects suitable for display.</returns>
		protected override IEnumerable<PositionedRenderObject2D<TRenderObject>> DoRenderFeature(
            FeatureDataRow feature, VectorStyle style)
        {
            if (feature == null)
            {
                throw new ArgumentNullException("feature");
            }

            if (style == null)
            {
                throw new ArgumentNullException("style");
            }

            if (feature.Geometry == null)
            {
                throw new InvalidOperationException("Feature must have a geometry to be rendered.");
            }

            return renderGeometry(feature.Geometry, style);
		}

		/// <summary>
		/// Renders a <see cref="MultiLineString"/>.
		/// </summary>
        /// <param name="lines">MultiLineString to be rendered.</param>
        /// <param name="fill">Pen used for filling (null or transparent for no filling).</param>
        /// <param name="highlightFill">Pen used for filling when highlighted.</param>
        /// <param name="selectFill">Pen used for filling when selected.</param>
        /// <param name="outline">Outline pen style (null if no outline).</param>
        /// <param name="highlightOutline">Outline pen style used when highlighted.</param>
        /// <param name="selectOutline">Outline pen style used when selected.</param>
		public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawMultiLineString(
            MultiLineString lines, StylePen fill, StylePen highlightFill, StylePen selectFill,
			StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			return drawLineStrings(lines.LineStrings, fill, highlightFill, selectFill, outline, highlightOutline, selectOutline);
		}

		/// <summary>
		/// Renders a <see cref="LineString"/>.
		/// </summary>
		/// <param name="line">LineString to render.</param>
		/// <param name="fill">Pen used for filling (null or transparent for no filling).</param>
		/// <param name="highlightFill">Pen used for filling when highlighted.</param>
		/// <param name="selectFill">Pen used for filling when selected.</param>
		/// <param name="outline">Outline pen style (null if no outline).</param>
		/// <param name="highlightOutline">Outline pen style used when highlighted.</param>
		/// <param name="selectOutline">Outline pen style used when selected.</param>
        public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawLineString(
            LineString line, StylePen fill, StylePen highlightFill, StylePen selectFill,
			StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			return drawLineStrings(new LineString[] { line }, fill, highlightFill, selectFill, 
                outline, highlightOutline, selectOutline);
		}

		/// <summary>
		/// Renders a <see cref="MultiPolygon"/>.
		/// </summary>
		/// <param name="multipolygon">MultiPolygon to render.</param>
		/// <param name="fill">Brush used for filling (null or transparent for no filling).</param>
		/// <param name="highlightFill">Brush used for filling when highlighted.</param>
		/// <param name="selectFill">Brush used for filling when selected.</param>
		/// <param name="outline">Outline pen style (null if no outline).</param>
		/// <param name="highlightOutline">Outline pen style used when highlighted.</param>
		/// <param name="selectOutline">Outline pen style used when selected.</param>
		public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawMultiPolygon(
            MultiPolygon multipolygon, StyleBrush fill, StyleBrush highlightFill, StyleBrush selectFill, 
            StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			return drawPolygons(multipolygon.Polygons, fill, highlightFill, selectFill, 
                outline, highlightOutline, selectOutline);
		}

		/// <summary>
		/// Renders a <see cref="Polygon"/>.
		/// </summary>
		/// <param name="polygon">Polygon to render</param>
		/// <param name="fill">Brush used for filling (null or transparent for no filling).</param>
		/// <param name="highlightFill">Brush used for filling when highlighted.</param>
		/// <param name="selectFill">Brush used for filling when selected.</param>
		/// <param name="outline">Outline pen style (null if no outline).</param>
		/// <param name="highlightOutline">Outline pen style used when highlighted.</param>
		/// <param name="selectOutline">Outline pen style used when selected.</param>
		public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawPolygon(
            Polygon polygon, StyleBrush fill, StyleBrush highlightFill, StyleBrush selectFill, 
            StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			return drawPolygons(new Polygon[] { polygon }, fill, highlightFill, selectFill, 
                outline, highlightOutline, selectOutline);
		}

		/// <summary>
		/// Renders a <see cref="Point"/>.
		/// </summary>
		/// <param name="point">Point to render.</param>
		/// <param name="symbol">Symbol to place over point.</param>
		/// <param name="highlightSymbol">Symbol to use for point when point is highlighted.</param>
		/// <param name="selectSymbol">Symbol to use for point when point is selected.</param>
		public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawPoint(
            Point point, Symbol2D symbol, Symbol2D highlightSymbol, Symbol2D selectSymbol)
		{
			return drawPoints(new Point[] { point }, symbol, highlightSymbol, selectSymbol);
		}

		/// <summary>
		/// Renders a <see cref="MultiPoint"/>.
		/// </summary>
		/// <param name="points">MultiPoint to render.</param>
		/// <param name="symbol">Symbol to place over point.</param>
		/// <param name="highlightSymbol">Symbol to use for point when point is highlighted.</param>
		/// <param name="selectSymbol">Symbol to use for point when point is selected.</param>
		public virtual IEnumerable<PositionedRenderObject2D<TRenderObject>> DrawMultiPoint(
            MultiPoint points, Symbol2D symbol, Symbol2D highlightSymbol, Symbol2D selectSymbol)
		{
			return drawPoints(points.Points, symbol, highlightSymbol, selectSymbol);
		}

		#region Private helper methods
		private IEnumerable<PositionedRenderObject2D<TRenderObject>> renderGeometry(
            IGeometry geometry, VectorStyle style)
        {
            if (geometry == null)
            {
                throw new ArgumentNullException("geometry");
            }

            if (geometry is Polygon)
            {
                return DrawPolygon(geometry as Polygon, style.Fill, style.HighlightFill, style.SelectFill, style.Outline, 
                    style.HighlightOutline, style.SelectOutline);
            }
            else if (geometry is MultiPolygon)
            {
                return DrawMultiPolygon(geometry as MultiPolygon, style.Fill, style.HighlightFill, style.SelectFill, 
                    style.Outline, style.HighlightOutline, style.SelectOutline);
            }
            else if (geometry is LineString)
            {
                return DrawLineString(geometry as LineString, style.Line, style.HighlightLine, style.SelectLine, 
                    style.Outline, style.HighlightOutline, style.SelectOutline);
            }
            else if (geometry is MultiLineString)
            {
                return DrawMultiLineString(geometry as MultiLineString, style.Line, style.HighlightLine, style.SelectLine, 
                    style.Outline, style.HighlightOutline, style.SelectOutline);
            }
            else if (geometry is Point)
            {
                return DrawPoint(geometry as Point, style.Symbol, style.HighlightSymbol, style.SelectSymbol);
            }
            else if (geometry is MultiPoint)
            {
                return DrawMultiPoint(geometry as MultiPoint, style.Symbol, style.HighlightSymbol, style.SelectSymbol);
            }
            else if (geometry is GeometryCollection)
            {
                List<TRenderObject> renderObjects = new List<TRenderObject>();

                foreach (Geometry g in (geometry as GeometryCollection))
                {
                    return renderGeometry(g, style);
                }
            }

            throw new NotSupportedException(String.Format("Geometry type is not supported: {0}", geometry.GetType()));
        }

		private IEnumerable<PositionedRenderObject2D<TRenderObject>> drawPoints(
            IEnumerable<Point> points, Symbol2D symbol, Symbol2D highlightSymbol, Symbol2D selectSymbol)
		{
			foreach (Point point in points)
			{
				if (point == null)
				{
					continue;
				}

				if (symbol == null) //We have no point symbol - Use a default symbol
				{
					symbol = DefaultSymbol;
				}

				Point2D pointLocation = ViewTransform.TransformVector(point.X, point.Y);

				TRenderObject renderedObject = VectorRenderer.RenderSymbol(
                    pointLocation, symbol, highlightSymbol, selectSymbol);

				yield return new PositionedRenderObject2D<TRenderObject>(pointLocation.X, pointLocation.Y, renderedObject);
			}
		}

		private IEnumerable<PositionedRenderObject2D<TRenderObject>> drawLineStrings(
            IEnumerable<LineString> lines, StylePen fill, StylePen highlightFill, StylePen selectFill,
			StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			GraphicsPath2D gp = new GraphicsPath2D();

			foreach (LineString line in lines)
			{
				gp.NewFigure(convertPointsAndTransform(line.Vertices), false);
			}

            TRenderObject renderedObject;

            Point2D location = gp.Bounds.Location;

			if (outline != null && highlightOutline != null && selectOutline != null)
			{
                renderedObject = VectorRenderer.RenderPath(gp, outline, highlightOutline, selectOutline);
				yield return new PositionedRenderObject2D<TRenderObject>(location.X, location.Y, renderedObject);
			}

            renderedObject = VectorRenderer.RenderPath(gp, fill, highlightFill, selectFill);
            yield return new PositionedRenderObject2D<TRenderObject>(location.X, location.Y, renderedObject);
		}

		private IEnumerable<PositionedRenderObject2D<TRenderObject>> drawPolygons(
            IEnumerable<Polygon> polygons, StyleBrush fill, StyleBrush highlightFill, StyleBrush selectFill, 
            StylePen outline, StylePen highlightOutline, StylePen selectOutline)
		{
			foreach (Polygon polygon in polygons)
			{
				if (polygon.ExteriorRing == null)
				{
					continue;
				}

				if (polygon.ExteriorRing.Vertices.Count <= 1)
				{
					continue;
				}

				GraphicsPath2D gp = new GraphicsPath2D();

				// Add the exterior polygon
				gp.NewFigure(convertPointsAndTransform(polygon.ExteriorRing.Vertices), true);

				// Add the interior polygons (holes)
				foreach (LinearRing ring in polygon.InteriorRings)
				{
					gp.NewFigure(convertPointsAndTransform(ring.Vertices), true);
				}

                TRenderObject renderedObject;

                Point2D location = gp.Bounds.Location;

                renderedObject = VectorRenderer.RenderPath(gp, fill, highlightFill, selectFill, 
                    outline, highlightOutline, selectOutline);

                yield return new PositionedRenderObject2D<TRenderObject>(location.X, location.Y, renderedObject);
			}
        }

        private IEnumerable<Point2D> convertPointsAndTransform(IEnumerable<Point> points)
        {
            foreach (Point geoPoint in points)
            {
                yield return ViewTransform.TransformVector(geoPoint.X, geoPoint.Y);
            }
		}
		#endregion
	}
}