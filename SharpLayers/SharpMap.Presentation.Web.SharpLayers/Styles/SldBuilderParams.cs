using System.IO;
using System.Web;
using System.Web.UI;
using AjaxControlToolkit;

namespace SharpMap.Presentation.Web.SharpLayers.Styles
{
    public class SldBuilderParams : BuilderParamsBase
    {
        private string _sldDocumentPath;

        [ExtenderControlProperty(true)]
        [ClientPropertyName("sldDocumentXml"),
         PersistenceMode(PersistenceMode.InnerProperty)]
        public string SldDocumentXml { get; set; }


        public string SldDocumentPath
        {
            get { return _sldDocumentPath; }
            set
            {
                _sldDocumentPath = value;
                SldDocumentXml = GetDocumentContent(value);
            }
        }

        private string GetDocumentContent(string uri)
        {
            return File.ReadAllText(HttpContext.Current.Server.MapPath(uri));
        }
    }
}