// /*
//  *  The attached / following is part of SharpMap.Data.Providers.Kml
//  *  SharpMap.Data.Providers.Kml is free software � 2008 Newgrove Consultants Limited, 
//  *  www.newgrove.com; you can redistribute it and/or modify it under the terms 
//  *  of the current GNU Lesser General Public License (LGPL) as published by and 
//  *  available from the Free Software Foundation, Inc., 
//  *  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA: http://fsf.org/    
//  *  This program is distributed without any warranty; 
//  *  without even the implied warranty of merchantability or fitness for purpose.  
//  *  See the GNU Lesser General Public License for the full details. 
//  *  
//  *  Author: John Diss 2009
//  * 
//  */
using System;
using System.Xml.Serialization;

namespace SharpMap.Entities.Ogc.Kml
{
    [XmlType(TypeName = "SnippetType", Namespace = Declarations.SchemaVersion), Serializable]
    public class SnippetType
    {
        [XmlIgnore] private int __maxLines;

        [XmlIgnore] public bool __maxLinesSpecified;

        [XmlIgnore] private string __Value;

        public SnippetType()
        {
            maxLines = 2;
        }

        [XmlAttribute(AttributeName = "maxLines", DataType = "int")]
        public int maxLines
        {
            get { return __maxLines; }
            set
            {
                __maxLines = value;
                __maxLinesSpecified = true;
            }
        }

        [XmlText(DataType = "string")]
        public string Value
        {
            get { return __Value; }
            set { __Value = value; }
        }

        public void MakeSchemaCompliant()
        {
        }
    }
}