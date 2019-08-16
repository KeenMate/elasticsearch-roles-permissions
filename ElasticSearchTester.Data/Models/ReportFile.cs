using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ElasticSearchTester.Data.Models
{
	[Serializable]
	public class ReportFile
	{
		[JsonProperty(PropertyName = "filename")]
		public readonly string FileName;

		[JsonProperty(PropertyName = "id")]
		public readonly Guid Identifier;

		public ReportFile(Guid identifier, string fileName)
		{
			Identifier = identifier;
			FileName = fileName;
		}
	}
}