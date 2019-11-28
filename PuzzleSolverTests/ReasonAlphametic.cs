using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PuzzleSolver;

namespace PuzzleSolverTests
{
	class ReasonSum : ReasonBase
	{
		public ReasonSum(int column, byte carry, byte add1, byte add2, byte sum, byte newValue)
		{
			Column = column;
			Carry = carry;
			Add1 = add1;
			Add2 = add2;
			Sum = sum;
			NewValue = newValue;
		}

		public int Column { get; }
		public byte Carry { get; }
		public byte Add1 { get; }
		public byte Add2 { get; }
		public byte Sum { get; }
		public byte NewValue { get; }
	}
	class ImpossibleSum : ReasonBase
	{
		public ImpossibleSum(int column, byte carry, byte add1, byte add2, byte sum)
		{
			Column = column;
			Carry = carry;
			Add1 = add1;
			Add2 = add2;
			Sum = sum;
		}

		public int Column { get; }
		public byte Carry { get; }
		public byte Add1 { get; }
		public byte Add2 { get; }
		public byte Sum { get; }
	}

	class GenerateCarry : ReasonBase
	{
		public GenerateCarry(int column, byte value)
		{
			Column = column;
			this.value = value;
		}

		public int Column { get; }
		public byte value { get; }
	}

	class ImpossibleCarry : ReasonBase
	{
		public ImpossibleCarry(int column, byte incompatibleValue)
		{
			Column = column;
			IncompatibleValue = incompatibleValue;
		}

		public int Column { get; }
		public byte IncompatibleValue { get; }
	}

	class DisableLeadingZero : ReasonBase
	{
		public DisableLeadingZero(char c)
		{
			Char = c;
		}

		public char Char { get; }
	}

	class ImpossibleSizes : ReasonBase
	{
	}
}
