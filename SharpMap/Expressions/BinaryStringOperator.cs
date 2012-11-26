﻿/*
 *  The attached / following is part of SharpMap
 *  this file © 2008 Newgrove Consultants Limited, 
 *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
 *  of the current GNU Lesser General Public License (LGPL) as published by and 
 *  available from the Free Software Foundation, Inc., 
 *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
 *  This program is distributed without any warranty; 
 *  without even the implied warranty of merchantability or fitness for purpose.  
 *  See the GNU Lesser General Public License for the full details. 
 *  
 *  Author: John Diss 2008
 * 
 */
namespace SharpMap.Expressions
{
    /// <summary>
    /// Binary operators for binary comparisons of <see cref="string"/>s
    /// </summary>
    public enum BinaryStringOperator
    {
        /// <summary>
        /// Binary starts with operator
        /// </summary>
        StartsWith,
        /// <summary>
        /// Binary contains operator
        /// </summary>
        Contains,
        /// <summary>
        /// Binary contains operator
        /// </summary>
        EndsWith,
        /// <summary>
        /// Binary equals operator
        /// </summary>
        Equals,
        /// <summary>
        /// Binary inequality operator
        /// </summary>
        NotEquals
    }
}