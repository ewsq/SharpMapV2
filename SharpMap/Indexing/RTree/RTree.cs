// Copyright 2006 - 2008: Rory Plaire (codekaizen@gmail.com)
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
using GeoAPI.Geometries;
using GeoAPI.Indexing;
using SharpMap.Expressions;

namespace SharpMap.Indexing.RTree
{
    /// <summary>
    /// Implements an R-Tree spatial index.
    /// </summary>
    /// <typeparam name="TItem">The type of the value used in the entries.</typeparam>
    public abstract class RTree<TItem> : ISpatialIndex<IExtents, TItem>
        where TItem : IBoundable<IExtents>
    {
        private ISpatialIndexNode<IExtents, TItem> _root;
        private readonly IGeometryFactory _geoFactory;
        private Boolean _disposed;
        //private Int64 _nextNodeId = 0;
        //private readonly Func<TItem, IExtents> _bounder;

        #region Object Construction/Destruction

        /// <summary>
        /// Creates a new instance of an RTree index.
        /// </summary>
        //protected RTree(Func<TItem, IExtents> bounder)
        //{
        //    _bounder = bounder;
        //    initIndex();
        //}

        protected RTree(IGeometryFactory geoFactory)
        {
            _geoFactory = geoFactory;
            initIndex();
        }

        ~RTree()
        {
            Dispose(false);
        }

        #region Dispose Pattern

        /// <summary>
        /// Disposes the index.
        /// </summary>
        public void Dispose()
        {
            if (!IsDisposed)
            {
                Dispose(true);
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(Boolean disposing) { }

        protected Boolean IsDisposed
        {
            get { return _disposed; }
            private set { _disposed = value; }
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Root node in the R-Tree
        /// </summary>
        public ISpatialIndexNode<IExtents, TItem> Root
        {
            get { return _root; }
            protected set { _root = value; }
        }

        public IExtents Bounds
        {
            get
            {
                return _root == null
                    ? _geoFactory.CreateExtents()
                    : _root.Bounds;
            }
        }

        //protected Func<TItem, IExtents> Bounder
        //{
        //    get { return _bounder; }
        //}

        #endregion

        /// <summary>
        /// Clears the index of all entries.
        /// </summary>
        public void Clear()
        {
            initIndex();
        }

        #region ISpatialIndex<IExtents,TItem> Members

        //public abstract void Insert(IExtents bounds, TItem item);
        public abstract void Insert(TItem item);

        public virtual IEnumerable<TItem> Query(SpatialExpression spatialExpression)
        {
            GeometryExpression geometryExpression = spatialExpression as GeometryExpression;
            ExtentsExpression extentsExpression = spatialExpression as ExtentsExpression;

            IGeometry geometry = geometryExpression != null
                                     ? geometryExpression.Geometry
                                     : null;

            IExtents extents = extentsExpression != null
                                   ? extentsExpression.Extents
                                   : geometry.Extents;

            return geometry == null
                       ? Query(extents)
                       : Query(geometry);
        }

        /// <summary>
        /// Searches the tree and looks for intersections with the given
        /// <see cref="IExtents"/>.
        /// </summary>
        /// <param name="bounds">
        /// An <see cref="IExtents"/> to intersect the index with.
        /// </param>
        public virtual IEnumerable<TItem> Query(IExtents bounds)
        {
            return IntersectTreeRecursive(bounds, Root as RTreeNode<TItem>);
        }

        public IEnumerable<TItem> Query(IExtents bounds, Predicate<TItem> predicate)
        {
            foreach (TItem item in IntersectTreeRecursive(bounds, Root as RTreeNode<TItem>))
            {
                if (predicate(item)) yield return item;
            }
        }

        public IEnumerable<TResult> Query<TResult>(IExtents bounds, Func<TItem, TResult> selector)
        {
            foreach (TItem item in Query(bounds))
            {
                yield return selector(item);
            }
        }

        /// <summary>
        /// Searches the tree and looks for intersections with the
        /// given <paramref name="geometry"/>.
        /// </summary>
        /// <param name="geometry">Geometry to intersect the index with.</param>
        public virtual IEnumerable<TItem> Query(IGeometry geometry)
        {
            if (geometry == null || geometry.IsEmpty)
            {
                yield break;
            }

            foreach (TItem item in Query(geometry.Extents))
            {
                // Hm, just what is the performance of this delegate call and intersect...
                //IExtents bounds = _bounder(item);
                IExtents bounds = item.Bounds;

                if (geometry.Intersects(bounds.ToGeometry()))
                {
                    yield return item;
                }
            }
        }

        #endregion

        #region Protected / Internal methods

        /// <summary>
        /// Creates a new index node with an empty extents.
        /// </summary>
        /// <returns>A new node for the R-Tree.</returns>
        protected internal virtual RTreeNode<TItem> CreateNode(int level)
        {
            //RTreeNode<TItem> node = new RTreeNode<TItem>(this, _bounder);
            RTreeNode<TItem> node = new RTreeNode<TItem>(this, level, _geoFactory.CreateExtents());
            return node;
        }

        /// <summary>
        /// Creates a new index branch node with the given extents.
        /// </summary>
        /// <returns>A new branch node for the R-Tree.</returns>
        protected internal virtual RTreeNode<TItem> CreateNode(int level, IExtents extents)
        {
            RTreeNode<TItem> node = CreateNode(level);
            node.Bounds = extents;
            return node;
        }

        /// <summary>
        /// Recursive function that traverses the tree and looks for intersections 
        /// with a given <see cref="IExtents">region</see>.
        /// </summary>
        /// <remarks>
        /// Nothing is added to the list if <paramref name="bounds"/> is 
        /// <see langword="null" />
        /// </remarks>
        /// <param name="bounds">An IExtents to intersect with.</param>
        /// <param name="node">Node to search from.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="node"/>  is null.
        /// </exception>
        protected IEnumerable<TItem> IntersectTreeRecursive(IExtents bounds,
            RTreeNode<TItem> node)
        {
            if (node == null) throw new ArgumentNullException("node");

            if (bounds == null)
            {
                yield break;
            }

            if (node.IsLeaf)
            {
                foreach (TItem item in node.Items)
                {
                    if (item.Intersects(bounds))
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                if (bounds.Intersects(node.Bounds))
                {
                    foreach (RTreeNode<TItem> child in node.SubNodes)
                    {
                        foreach (TItem item in IntersectTreeRecursive(bounds, child))
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private helper methods

        private void initIndex()
        {
            Root = CreateNode(0);
        }

        //private UInt32 getNewNodeId()
        //{
        //    return (UInt32) Interlocked.Increment(ref _nextNodeId);
        //}

        #endregion




        #region ISpatialIndex<IExtents,TItem> Members


        public abstract bool Remove(TItem item);

        #endregion


        public void InsertRange(IEnumerable<TItem> items)
        {
            foreach (TItem item in items)
            {
                Insert(item);
            }
        }
        #region ISpatialIndexNodeFactory<IExtents,TItem> Members

        ISpatialIndexNode<IExtents, TItem> ISpatialIndexNodeFactory<IExtents, TItem>.CreateNode(int level)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}