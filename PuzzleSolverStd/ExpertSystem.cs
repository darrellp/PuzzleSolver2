using System.Collections.Generic;
using System.Linq;

namespace PuzzleSolver
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// The ExpertSystem is composed of a set of rules which act on IPartialSolution objects.  It
	/// applies those rules until none apply or until some rule flags the partial solution as
	/// unsolvable.
	/// </summary>
	///
	/// <remarks>	Darrellp, 2/14/2011. </remarks>
	///
	/// <typeparam name="TPs">	Type of the ps. </typeparam>
	////////////////////////////////////////////////////////////////////////////////////////////////////

	public class ExpertSystem<TPs> where TPs : IPartialSolution
	{
		// True if we're gathering reasons during the backtracking process.
		internal bool IsKeepingReasons {get; }
        private readonly List<IRule> _lstIRule;
		private readonly List<IRule> _lstOneTimeRules; 

		////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Constructor. </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="lstRules">		  	The list of rules. </param>
		/// <param name="lstOneTimeRules">	(Optional) Rules run only once at the start. </param>
		/// <param name="fKeepReasons">   	(Optional) true if we want to gather reasons. </param>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public ExpertSystem(List<IRule> lstRules, List<IRule> lstOneTimeRules = null, bool fKeepReasons = false)
		{
			_lstIRule = lstRules;
			IsKeepingReasons = fKeepReasons;
			_lstOneTimeRules = lstOneTimeRules;
		}

        ////////////////////////////////////////////////////////////////////////////////////////////////////
		/// <summary>	Apply the rules of this expert system. </summary>
		///
		/// <remarks>	Darrellp, 2/14/2011. </remarks>
		///
		/// <param name="ps">			The partial solution we're being applied to. </param>
		/// <param name="firstTime">	True if this is the first time the expert system is being called. </param>
		/// <param name="lstrrp">   	[out] The list of reasons for rule applications. </param>
		///
		/// <returns>	Returns false if an impossible state is detected, else true. </returns>
		////////////////////////////////////////////////////////////////////////////////////////////////////

		public bool FApply(TPs ps, bool firstTime, out List<ReasonRulePair> lstrrp)
		{
			// Set up
			var fApplied = true;
			lstrrp = IsKeepingReasons ? new List<ReasonRulePair>() : null;

			// As long as some rule is applied, cycle through and try to apply them all
			while (fApplied)
			{
				fApplied = false;
				var ruleList = firstTime && _lstOneTimeRules != null ? _lstOneTimeRules : _lstIRule;

				// For every applicable rule
				foreach (var rl in ruleList.Where(rl => rl.FTrigger(ps)))
				{
                    // Apply the rule
					fApplied = rl.FApply(ps, out var lstReason, out var fImpossible);


					// Are we gathering reasons?
					if (lstrrp != null && lstReason != null)
					{
						// Assume all rules applied
						foreach (var reason in lstReason)
						{
							reason.Applied = true;
						}
						// Add the new reasons
						lstrrp.AddRange(lstReason.Select(reason => new ReasonRulePair(reason, rl)));
					}

					// Are we at an impossible position?
					if (fImpossible)
					{
						if (lstrrp != null && lstReason != null)
						{
							// We assume that any impossible situation was immediately returned from
							// and so will be the last reason in the list...
							lstrrp[lstrrp.Count - 1].Reason.Applied = false;
							lstrrp[lstrrp.Count - 1].Reason.Impossible = true;
						}
						// The rule detected an unsolveable partial solution - return false
						return false;
					}
					if (fApplied)
					{
						// If we find a rule that applied, then restart at the top
						// since ones at the top (the most "important/useful" ones)
						// may now apply
						if (!firstTime)
						{
							break;
						}
					}
				}
				fApplied = fApplied || firstTime;
				firstTime = false;
			}

			// We've applied as many rules as we can - start a full backtracking search.
			return true;
		}
	}
}
