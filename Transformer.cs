using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using StatusTracking_v1_0;
using Documentation_v1_0;

namespace StatusTrackingToDocumentationTRANS
{
    public class Transformer
    {
        T LoadXml<T>(string xmlFileName)
        {
            using (FileStream fStream = File.OpenRead(xmlFileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T result = (T)serializer.Deserialize(fStream);
                fStream.Close();
                return result;
            }
        }



	    public Tuple<string, string>[] GetGeneratorContent(params string[] xmlFileNames)
	    {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            foreach(string xmlFileName in xmlFileNames)
            {
                StatusTrackingAbstractionType fromAbs = LoadXml<StatusTrackingAbstractionType>(xmlFileName);
                DocumentationAbstractionType toAbs = TransformAbstraction(fromAbs);
                string xmlContent = WriteToXmlString(toAbs);
                FileInfo fInfo = new FileInfo(xmlFileName);
                string contentFileName = "Documentation_From" + fInfo.Name;
                result.Add(Tuple.Create(contentFileName, xmlContent));
            }
	        return result.ToArray();
	    }

        private string WriteToXmlString(object toAbs)
        {
            XmlSerializer serializer = new XmlSerializer(toAbs.GetType());
            MemoryStream memoryStream = new MemoryStream();
            serializer.Serialize(memoryStream, toAbs);
            byte[] data = memoryStream.ToArray();
            string result = System.Text.Encoding.UTF8.GetString(data);
            return result;
        }

        public static DocumentationAbstractionType TransformAbstraction(StatusTrackingAbstractionType fromAbs)
        {
            return new DocumentationAbstractionType();
        }

    }
}
