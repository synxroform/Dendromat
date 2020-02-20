using System.Collections.Generic;
using System.Linq;

namespace Zeometry.Common
{
	public class MyMatrix<T>
	{
		private T[,] matrix;
		private int A;
		private int B;

		public MyMatrix(List<List<T>> list)
		{
			A = list.Count;
			B = list[0].Count;

			matrix = new T[A, B];

			for (int n = 0; n < A; n++)
				for (int m = 0; m < B; m++) matrix[n, m] = list[n][m];
		}

		public MyMatrix(int A, int B)
		{
			matrix = new T[A, B];
			this.A = A;
			this.B = B;
		}

		public void SetRow(List<T> row, int index)
		{
			for (int n = 0; n < B; n++)
			matrix[index, n] = row[n];
		}

		public List<T> GetRow(int index)
		{
			List<T> output = new List<T>();
			for (int n = 0; n < B; n++)
				output.Add(matrix[index, n]);
			return output;
		}

		public void SetColumn(List<T> column, int index)
		{
			for (int n = 0; n < A; n++)
				matrix[n, index] = column[n];
		}

		public List<T> GetColumn(int index)
		{
			List<T> output = new List<T>();
			for (int n = 0; n < A; n++)
				output.Add(matrix[n, index]);
			return output;
		}

		public MyMatrix<T> Flipped()
		{
			MyMatrix<T> output = new MyMatrix<T>(A, B);

			for (int n = 0; n < A; n++)
				for (int m = 0; m < B; m++)
					output.GetMatrix[m, n] = matrix[n, m];

			return output;
		}

		public T[,] GetMatrix
		{
			get { return matrix; }
		}

		public int Rows
		{
			get { return A; }
		}

		public int Columns
		{
			get {return B; }
		}
  }
}