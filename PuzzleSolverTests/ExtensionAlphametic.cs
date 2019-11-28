using PuzzleSolver;

namespace PuzzleSolverTests
{
	internal class ExtensionAlphametic : IExtension
	{
		public char Char { get; }
		public byte Value { get; }

		public ExtensionAlphametic(char character, byte value)
		{
			Char = character;
			Value = value;
		}

		public int CompareTo(object obj)
		{
			return 0;
		}
	}
}
