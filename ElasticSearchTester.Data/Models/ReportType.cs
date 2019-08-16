using System;
using Newtonsoft.Json;

namespace ElasticSearchTester.Data.Models
{
	[Serializable]
	public class ReportType
	{
		[JsonProperty(PropertyName = "id")]
		public readonly string Id;

		[JsonProperty(PropertyName = "title")]
		public readonly string Title;

		public ReportType(string id, string title)
		{
			Id = id;
			Title = title;
		}
	}
}