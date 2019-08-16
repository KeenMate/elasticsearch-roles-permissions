using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ElasticSearchTester.Data.Models;

namespace ElasticSearchTester.Utils
{
	public class CoverageUtils
	{
		private readonly Random random;

		public CoverageUtils(Random random)
		{
			this.random = random;
		}

		public List<T> GetUpTo<T>(int number, List<T> items)
		{
			if (number > items.Count)
				throw new ArgumentOutOfRangeException(nameof(number), "Number was greater than amount of items");

			int returnLength = random.Next(1, number);
			List<T> result = new List<T>();

			for (int i = 0; i < returnLength; i++)
			{
				T item = items[random.Next(items.Count)];
				if (!result.Contains(item))
					result.Add(item);
			}

			return result;
		}

		public List<string> GetUpTo(int number, Dictionary<string, decimal> options)
		{
			int count = random.Next(1, number + 1);
			List<string> result = new List<string>();

			Dictionary<string, decimal> remaining = options
				.ToList()
				.ToDictionary(x => x.Key, x => x.Value);
			for (int i = 0; i < count; i++)
			{
				string chosen = GetRandom(remaining);

				remaining.Remove(chosen);

				result.Add(chosen);
			}

			return result;
		}

		public T GetOne<T>(IList<T> list)
		{
			int index = random.Next(list.Count);
			T result = default;

			try
			{
				result = list[index];
			}
			catch (Exception e)
			{
				Console.WriteLine($"List count: {list.Count} and Index: {index}");
				Console.ReadKey(true);
			}

			return result;
		}

		public T GetProbabilisticRandom<T>(List<CoverageInfo<T>> items)
		{
			double luckyNumber = random.NextDouble();
			IOrderedEnumerable<CoverageInfo<T>> ordered = items.OrderBy(x => x.Probability);
			foreach (var item in ordered)
			{
				if (luckyNumber < item.Probability)
					return item.Item;
			}

			return items[random.Next(items.Count)].Item;
		}

		/// <summary>
		/// </summary>
		/// <param name="options">Options with assigned probability</param>
		/// <returns></returns>
		public string GetRandom(Dictionary<string, decimal> options)
		{
			if (options.Values.Sum() > 1)
				throw new ArgumentOutOfRangeException(nameof(options), "Probability sum cannot exceed 1");

			List<decimal> chances = new List<decimal>(options.Count);
			for (int i = 0; i < options.Count; i++)
				chances.Add((decimal) random.NextDouble());

			List<KeyValuePair<string, decimal>> orderedOptions = options
				.OrderBy(pair => pair.Value)
				.ToList();

			int j = 0;
			foreach (var option in orderedOptions)
			{
				if (chances[j] < option.Value)
					return option.Key;

				j++;
			}

			return orderedOptions
				.Last()
				.Key;
		}

		/// <summary>
		/// Method which for given array returns 
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public double[] SoftmaxFn(double[] input)
		{
			double Op(double x) => Math.Pow(Math.E, (double) x);

			double[] output = new double[input.Length];

			double sum = input.Sum(Op);
			for (int i = 0; i < input.Length; i++)
				output[i] = Op(input[i]) / sum;

			return output;
		}
	}
}