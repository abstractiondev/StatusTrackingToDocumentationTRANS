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
            foreach (string xmlFileName in xmlFileNames)
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

        public DocumentationAbstractionType TransformAbstraction(StatusTrackingAbstractionType fromAbs)
        {
            DocumentationAbstractionType toAbs = new DocumentationAbstractionType()
            {
                Documentations = new DocumentationsType { Documents = new[] { GetDocument(fromAbs) } }
            };
            return toAbs;
        }

        private DocumentType GetDocument(StatusTrackingAbstractionType fromAbs)
        {
            DocumentType document = new DocumentType()
                                        {
                                            name = "Status Tracking Document",
                                            title = "Status Tracking Document"
                                        };

            document.AddHeader(GetSummary(fromAbs));
            Array.ForEach(fromAbs.Groups.OrderBy(grp => grp.name).ToArray(), 
                groupItem => document.AddHeader(GetGroupStatus(groupItem, fromAbs)));
            return document;
        }

        private HeaderType GetGroupStatus(GroupType grp, StatusTrackingAbstractionType fromAbs)
        {
            StatusItemType[] groupStatusItems = grp.GetStatusItems(fromAbs);
            StatusSummaryItem groupSummary = grp.GetGroupSummary(fromAbs);
            if (groupSummary.IsComplete)
                return null;
            string headerText = grp.name + String.Format(" ({0:P0})", groupSummary.GreenRatio);
            HeaderType result = new HeaderType
                                    {
                                        text = headerText,
                                        level = 1,
                                    };
            //result.AddSubHeaderTableContent("Items", GetStatusItemTable(groupStatusItems));
            result.AddHeaderTableContent(GetStatusItemTable(groupStatusItems));
            return result;
        }

        private static HeaderType GetSummary(StatusTrackingAbstractionType fromAbs)
        {
            HeaderType summaryHeader = new HeaderType {text = "Summary", level = 1};
            var summaries =
                fromAbs.Groups.Select(grp => new
                                                 {
                                                     Name = grp.name,
                                                     IsRoot = grp.groupRole == GroupTypeGroupRole.Root,
                                                     RootOrder = grp.groupRole == GroupTypeGroupRole.Root ? 0 : 1,
                                                     Summary = grp.GetGroupSummary(true, fromAbs)
                                                 });
            summaries =
                summaries.Where(summary => summary.Summary.IsIncomplete)
                    .OrderBy(summary => summary.RootOrder)
                    .ThenByDescending(summary => summary.Summary.RedRatio)
                    .ThenByDescending(summary => summary.Summary.YellowRatio);
            foreach(var summary in summaries)
            {
                string[] summaryStrings = new string[]
                                              {
                                                  summary.Summary.RedSummaryString,
                                                  summary.Summary.YellowSummaryString,
                                                  summary.Summary.GreenSummaryString
                                              }.Where(item => String.IsNullOrWhiteSpace(item) == false).ToArray();
                string summaryString = String.Join(", ", summaryStrings);
                string rootPrefix = summary.IsRoot ? "(R) " : "";
                string headerText = rootPrefix + summary.Name + String.Format(" ({0:P0})", summary.Summary.GreenRatio);
                HeaderType subHeader = summaryHeader.AddSubHeaderTextContent(headerText, null, summaryString);
            }
            return summaryHeader;
        }

        private static TableType GetStatusItemTable(StatusItemType[] statusItems)
        {
            TableType table = new TableType
            {
                Columns = new[]
                                                    {
                                                        new ColumnType {name = "Item"},
                                                        new ColumnType {name = "Status"},
                                                        new ColumnType {name = "Description"}
                                                    }
            };
            List<TextType[]> rows = new List<TextType[]>();
            rows.AddRange(statusItems.Select(item => new TextType[]
                                                       {
                                                           new TextType {TextContent = item.displayName, 
                                                               styleRef = GetStyleName(item.StatusValue.trafficLightIndicator)},
                                                           new TextType {TextContent = item.StatusValue.indicatorDisplayText,
                                                               styleRef = GetStyleName(item.StatusValue.trafficLightIndicator)},
                                                           new TextType {TextContent = item.description,
                                                               styleRef = GetStyleName(item.StatusValue.trafficLightIndicator)}
                                                       }));
            table.Rows = rows.ToArray();
            return table;
        }

        private static string GetStyleName(StatusValueTypeTrafficLightIndicator trafficLightIndicator)
        {
            switch(trafficLightIndicator)
            {
                case StatusValueTypeTrafficLightIndicator.green:
                    return null;
                case StatusValueTypeTrafficLightIndicator.yellow:
                    return "color:blue;font-weight:bold;font-style:italic";
                case StatusValueTypeTrafficLightIndicator.red:
                    return "color:red;font-weight:bold;text-decoration:underline";
                default:
                    throw new NotSupportedException("Traffic light value: " + trafficLightIndicator);
            }
        }
    }

}
