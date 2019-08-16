using System;
using Newtonsoft.Json;

namespace ElasticSearchTester.Data.Models
{
	[Serializable]
	public class CpasUserEntry
	{
		[JsonProperty(PropertyName = "oid")]
		public readonly int Oid;
		
		[JsonProperty(PropertyName = "login")]
		public readonly string Login;
		
		[JsonProperty(PropertyName = "fullname")]
		public readonly string Fullname;

		public CpasUserEntry(int oid, string login, string fullname)
		{
			Oid = oid;
			Login = login;
			Fullname = fullname;
		}
	}
}