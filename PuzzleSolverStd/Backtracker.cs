using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolver
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>	
	/// This is the heart of PuzzleSolver.  It tries using all the rules until none of them apply any
	/// more and then invokes standard backtracking.  Each time it makes a guess, it tries the rules
	/// again to determine the implications.  If the current board turns out to be impossible to
	/// solve, it backtracks. 
	/// </summary>
	///
	/// <remarks>	Darrellp, 2/14/2011. </remarks>
	///
	/// ### <typeparam name="TPs">	The type of partial solution we apply to. </typeparam>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public static class Backtracker<TPs> where TPs : IPartialSolution
	{
		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Return the first solution we can find if any and gather backtracking reasons.
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="ps">	Partial solution so far. </param>
		/// <param name="es">	Expert System to apply. </param>
		///
		/// <returns>	True if a solution was found, else false. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static (TPs, BacktrackInfo) FSolve(TPs ps, ExpertSystem<TPs> es)
		{
            return Solutions(ps, es).FirstOrDefault();
		}


		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	See if there is exactly one unique solution. </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="ps">	Partial solution so far. </param>
		/// <param name="es">	Expert System to apply. </param>
		///
		/// <returns>	true if exactly one solution exists, false otherwise. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static bool FUnique(TPs ps, ExpertSystem<TPs> es)
		{
			return Solutions((TPs)ps.Clone(), es).Take(2).Count() == 1;
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Gets an enumerable of all solutions found. </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="psIn">	The puzzle up to this point. </param>
		/// <param name="es">  	The expert system. </param>
		///
		/// <returns>	Enumerable of tuples with solutions and their reasons. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public static IEnumerable<(TPs, BacktrackInfo)> Solutions( TPs psIn, ExpertSystem<TPs> es)
        {
            var solutionsThusFar = new List<TPs>();
            var extensionsStack = new Stack<(TPs, List<IExtension>)>();
            var fFirstTime = true;

            BacktrackInfo bti = null;

            if (es != null)
            {
                bti = es.IsKeepingReasons ? new BacktrackInfo(BacktrackReason.InitialEntry, null) : null;
            }

            extensionsStack.Push((psIn, null));

            while (true)
            {
                TPs ps;

                if (extensionsStack.Count == 0)
                {
                    yield break;
                }

                var(psParent, lst) = extensionsStack.Peek();

                if (lst != null)
                {
                    if (lst.Count == 0)
                    {
                        // Ran out of extensions for this parent - move one higher in the heirarchy
                        extensionsStack.Pop();

                        // Add that there are no more moves
                        bti?.AddBacktrackReason(BacktrackReason.NoMoreMoves);
                        continue;
                    }

                    ps = (TPs)psParent.PsApply(lst[0], true);
                    // TODO: This is part of the reason for making the extensions into an enumerable of PartialBoards
                    // For the moment, though, we just remove the IExtension from the front of the list.
                    lst.RemoveAt(0);
                }
                else
                {
                    // Should only happen first time through
                    ps = psParent;
                    extensionsStack.Pop();
                }


                // Is an expert system available?
                if (es != null)
                {
                    // Apply rules and gather reasons.  Is it an impossible board?
                    var fImpossible = TryExpertSystem(es, ps, bti, fFirstTime);
                    fFirstTime = false;

					if (fImpossible)
                    {
						// don't generate children
						bti?.AddBacktrackReason(BacktrackReason.RuleDetectedImpossibility);
                        continue;
                    }
                }

                // Did we solve it with the expert system?
                if (ps.FSolved())
                {
                    // Record the solution
                    if (RecordSolutionEnum(ps, bti, solutionsThusFar))
                    {
                        bti?.AddBacktrackReason(BacktrackReason.GoalReachedButMoreNeeded);
                        yield return (ps, bti);
                    }

                    // It's solved - again, don't generate children.
                    continue;
                }

                // Didn't solve it by rules so start backtracking search
                // 
                // Get the list of potential moves from this partial board
                // TODO: GetIExtensions should return an enumerable (or enumerator?) of boards
                // List of IExtensions could potentially have a lot of extensions so we wanted
                // to keep each extension small which meant an extension to a "parent" board.
                // Problem is, now that we're flattening things this means we have to keep
                // both the extensions and the parent board around.  Better to get an IEnumerable
                // of actual partial solutions.  At the worst, this could be implemented by keeping
                // the parent board around, generating the current list of IExtensions and then
                // enumerating them off one at a time.  At best it will be much more efficient.
                // I think maybe enumerator is what we want here since an enumerator keeps track
                // of how much it's got left to go. IEnumerables are best for foreach loops but
                // our system of popping stuff off a stack is more complex than that.
                var lstIExtensions = ps.GetIExtensions();

                // Are there no moves from the current position?
                if (lstIExtensions.Count == 0)
                {
                    // Add that we've reached a leaf node
                    bti?.AddBacktrackReason(BacktrackReason.LeafNode);

                    // Return true to keep looking for solutions since none found on this branch
                    continue;
                }

                // We're either forced into this (if there's only one choice) or we're just going to make a series of guesses
                bti?.AddBacktrackReason(lstIExtensions.Count == 1 ? BacktrackReason.ForcedChoice : BacktrackReason.Guess);
                lstIExtensions.Sort();
                extensionsStack.Push((ps, lstIExtensions));
            }
        }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>
		/// Searches for a fixed count of solutions.  Returns as many up to that count as it can find.
		/// </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="ps">   	The puzzle/game up to this point. </param>
		/// <param name="cPlys">	Count of plys deep we've searched thus far. </param>
		/// <param name="bti">  	Backtrack info. </param>
		///
		/// <returns>	An evaluation of how valuable the current board position is. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		// ReSharper disable once UnusedMember.Local
		private static int EvaluateBoard(TPs ps, int cPlys, BacktrackInfo bti)
		{
			// Get the list of potential moves from this partial board
			var lstIExtensions = ps.GetIExtensions();

			// Are there no moves from the current position?
			if (lstIExtensions.Count == 0)
			{
                // Add that we've reached a leaf node
                bti?.AddBacktrackReason(BacktrackReason.LeafNode);
                // Return the value of this leaf board
				return ps.Evaluate();
			}

			// Assume that we're going to have a choice of moves
			var btr = BacktrackReason.Guess;

			// Is there really only one move available?
			if (lstIExtensions.Count == 1)
			{
				// Indicate that our choice is forced
				btr = BacktrackReason.ForcedChoice;
			}
			else
			{
				// Put the extensions in the most likely order of succeeding heuristically
				lstIExtensions.Sort();
			}

			var iVal = int.MinValue;

			// Try each extension in turn
			foreach (var ext in lstIExtensions)
			{
				BacktrackInfo btiCur = null;

				// Are we keeping track of backtrack reasons?
				if (bti != null)
				{
					btiCur = new BacktrackInfo(btr, ext);

					// Add that we're investigating this new move
					bti.AddBacktrackReason(btiCur);
				}

				// Apply the current extension
				var psCur = (TPs)ps.PsApply(ext, true);

				if (psCur.FContinueEvaluation(cPlys))
				{
					iVal = Math.Max(iVal, EvaluateBoard(ps, cPlys + 1, btiCur));
				}
				else
				{
					iVal = Math.Max(iVal, ps.Evaluate());
				}
			}

			return iVal;
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>	Check that solution isn't a dup of one already created </summary>
        ///
        /// <remarks>	Darrell Plank, 11/24/2019. </remarks>
        ///
        /// <param name="ps">			 	Solved solution </param>
        /// <param name="bti">			 	Reasons for inferences. </param>
        /// <param name="lstpsSolutions">	The list to place the found solutions in. </param>
        ///
        /// <returns>	True if it a solution was added, false if it was a duplicate. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private static bool RecordSolutionEnum(
            TPs ps,
            BacktrackInfo bti,
            ICollection<TPs> lstpsSolutions)
        {
            // Are we identical to solutions already identified?
            if (lstpsSolutions.Any(psCur => ps.IdenticalTo(psCur)))
            {
                // Add Duplicate Solution as the reason for backtracking
                bti?.AddBacktrackReason(BacktrackReason.DuplicateSolution);
                return false;
            }

            // New solution - add to the list of solutions
            lstpsSolutions.Add((TPs)ps.Clone());

            // Add that we found a soln but we need more
            bti?.AddBacktrackReason(BacktrackReason.GoalReachedButMoreNeeded);

            // Return true to keep looking for more solutions
            return true;
        }

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Attempts to expert system. </summary>
		///
		/// <remarks>	Darrell Plank, 11/24/2019. </remarks>
		///
		/// <param name="es"> 	Expert System to apply. </param>
		/// <param name="ps"> 	Partial solution so far. </param>
		/// <param name="bti">	[out] Reasons for inferences. </param>
		///
		/// <returns>
		/// True if we've reaced an impossible situation from which there is no advancement, false if the
		/// partial solution is still viable.
		/// </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		private static bool TryExpertSystem(ExpertSystem<TPs> es, TPs ps, BacktrackInfo bti, bool fFirstTime)
		{
            var fImpossible = !es.FApply(ps, fFirstTime, out var lstrrp);

            // Add that we applied the expert system instead of backtracking
            bti?.AddExpertSystemReasons(lstrrp);

            // Did we reach an impossible situation?
			if (fImpossible)
			{
                // Add that we detected an impossible board
                bti?.AddBacktrackReason(BacktrackReason.RuleDetectedImpossibility);
                // No possible solutions on this branch - Search other branches
				return true;
			}
			return false;
		}

	}
}
