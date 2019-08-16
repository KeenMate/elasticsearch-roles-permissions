namespace ElasticSearchTester.Data.Models
{
	public class CoverageInfo<T>
	{
		public T Item { get; }

		public double Probability { get; set; }

		public CoverageInfo(T item, double probability)
		{
			Item = item;
			Probability = probability;
		}
	}
}