using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Alea;
using Alea.Parallel;


namespace ConcurrencyModels
{
	class Program
	{
		const int ARRAY_SIZE = 10000000;

		static void Main(string[] args)
		{
			
			var array = new int[ARRAY_SIZE];
			ResetArray(array);

			for (int i = 1; i <= 3; i++)
			{
				Console.WriteLine($"\n\n*********Run {i}***********\n\n");
				Console.WriteLine($"Double Single Threaded: {Time(() => DoubleSingleThreaded(array)).TotalMilliseconds}ms Elapsed");
				ResetArray(array);
				Console.WriteLine($"Double Multi Threaded: {Time(() => DoubleMultiThreaded(array)).TotalMilliseconds}ms Elapsed");
				ResetArray(array);
				
				Console.WriteLine(
					$"Double SIMD, Single Threaded: {Time(() => DoubleSingleThreadedSIMD(array)).TotalMilliseconds}ms Elapsed");
				ResetArray(array);
				Console.WriteLine(
					$"Double SIMD, Multi Threaded: {Time(() => DoubleMultiThreadedSIMD(array)).TotalMilliseconds}ms Elapsed");
				ResetArray(array);
				Console.WriteLine(
					$"Double CUDA, Single Threaded: {Time(() => DoubleSingleThreadedCUDA(array)).TotalMilliseconds}ms Elapsed");
				ResetArray(array);
				Console.WriteLine(
					$"Double CUDA, Multi Threaded: {Time(() => DoubleMultiThreadedCUDA(array)).TotalMilliseconds}ms Elapsed");
			}
		}


		private static void DoubleMultiThreadedCUDA(int[] array)
		{
			var partitioner = Partitioner.Create(0, ARRAY_SIZE);

			Parallel.ForEach(partitioner.GetPartitions(Environment.ProcessorCount), p =>
			{
				while (p.MoveNext())
				{
					
					Gpu.Default.For(p.Current.Item1, p.Current.Item2, i => array[i] = i * 2);
				}
			});
		}

		private static void DoubleSingleThreadedCUDA(int[] array)
		{
			Gpu.Default.For(0, ARRAY_SIZE, i => array[i] = i*2);

		}

		private static void ResetArray(int[] array)
		{
			for (int i = 0; i < ARRAY_SIZE; i++)
			{
				array[i] = i;
			}
		}

		private static void DoubleSingleThreaded(int[] array)
		{
			for (int i = 0; i < ARRAY_SIZE; i++)
			{
				array[i] = array[i] * 2;
			}
		}

		private static void DoubleMultiThreaded(int[] array)
		{
			var partitioner = Partitioner.Create(0, ARRAY_SIZE);
		
			Parallel.ForEach(partitioner.GetPartitions(Environment.ProcessorCount), p =>
			{
				while (p.MoveNext())
				{
					for (int i = p.Current.Item1; i < p.Current.Item2; i++)
					{
						array[i] = array[i]* 2;
					}
				}
			});

		}

		private static void DoubleSingleThreadedSIMD(int[] array)
		{
			
			int vCount = Vector<int>.Count;
		
			for (int i = 0; i < ARRAY_SIZE; i+=vCount)
			{

				var lhs = new Vector<int>(array, i);

				

				Vector.Multiply(lhs, 2).CopyTo(array, i);
			}
		}


		private static void DoubleMultiThreadedSIMD(int[] array)
		{

			int vCount = Vector<int>.Count;


			var partitioner = Partitioner.Create(0, ARRAY_SIZE);

			Parallel.ForEach(partitioner.GetPartitions(Environment.ProcessorCount), p =>
			{

				while (p.MoveNext())
				{
					for (int i = p.Current.Item1; i < p.Current.Item2; i += vCount)
					{


						var lhs = new Vector<int>(array, i);



						Vector.Multiply(lhs, 2).CopyTo(array, i);


					}
				}
			});

		}

		private static TimeSpan Time(Action a)
		{
			var sw = Stopwatch.StartNew();
			a();
			sw.Stop();
			return sw.Elapsed;
		}
	}
}
