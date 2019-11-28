using Microsoft.VisualStudio.TestTools.UnitTesting;
using PuzzleSolver;
using bt = PuzzleSolver.Backtracker<PuzzleSolverTests.PartialSolutionAlphametic>;
using es = PuzzleSolver.ExpertSystem<PuzzleSolverTests.PartialSolutionAlphametic>;
namespace PuzzleSolverTests
{
	[TestClass]
	public class TestAlphametic
	{
		[TestMethod]
		public void TestSendMoreMoney()
		{
			var es = new es(
				RulesAlphametic.Rules, 
				RulesAlphametic.OneTimeRules);
			var psa = new PartialSolutionAlphametic("SEND", "MORE", "MONEY");
            var (psaSolved, _) = bt.FSolve(psa, es);
			Assert.IsTrue(psaSolved != null);
			Assert.AreEqual(9, psaSolved['S']);
			Assert.AreEqual(5, psaSolved['E']);
			Assert.AreEqual(6, psaSolved['N']);
			Assert.AreEqual(7, psaSolved['D']);
			Assert.AreEqual(1, psaSolved['M']);
			Assert.AreEqual(0, psaSolved['O']);
			Assert.AreEqual(8, psaSolved['R']);
			Assert.AreEqual(2, psaSolved['Y']);

            psa = new PartialSolutionAlphametic("EVERY", "THREE", "MONTHS");
            (psaSolved, _) = bt.FSolve(psa, es);
            Assert.IsTrue(psaSolved != null);
            Assert.AreEqual(6, psaSolved['E']);
            Assert.AreEqual(5, psaSolved['V']);
            Assert.AreEqual(7, psaSolved['R']);
            Assert.AreEqual(2, psaSolved['Y']);
            Assert.AreEqual(4, psaSolved['T']);
            Assert.AreEqual(3, psaSolved['H']);
            Assert.AreEqual(1, psaSolved['M']);
            Assert.AreEqual(0, psaSolved['O']);
            Assert.AreEqual(9, psaSolved['N']);
            Assert.AreEqual(8, psaSolved['S']);


			// No solution for SEND + MORE = MMONEY because MMONEY is too long
			psa = new PartialSolutionAlphametic("SEND", "MORE", "MMONEY");
			(psaSolved, _) = bt.FSolve(psa, es);
			Assert.IsTrue(psaSolved == null);

            // No solution for A + A = BA because A + A ends in A only if A is zero and
            // that would mean the two summing terms would start with zero - not possible.
			psa = new PartialSolutionAlphametic("A", "A", "BA");
            (psaSolved, _) = bt.FSolve(psa, es);
			Assert.IsTrue(psaSolved == null);

            // There is trivially a solution for A + A = BC though for any choice of
            //  A >= 5
            psa = new PartialSolutionAlphametic("A", "A", "BC");
            (psaSolved, _) = bt.FSolve(psa, es);
            Assert.IsTrue(psaSolved != null);
		}

		[TestMethod]
		public void TestReasons()
		{
			var psa = new PartialSolutionAlphametic("SEND", "MORE", "MONEY");
			var es = new es(
				RulesAlphametic.Rules,
				RulesAlphametic.OneTimeRules,
				true);
            var (solution, bti) = bt.FSolve(psa, es);
			Assert.IsTrue(solution != null);
		}
	}
}
