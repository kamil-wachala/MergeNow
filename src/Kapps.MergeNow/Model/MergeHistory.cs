using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MergeNow.Model
{
    public class MergeHistory : IEnumerable<KeyValuePair<string, List<string>>>
    {
        private Dictionary<string, List<string>> Items { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public void Add(IEnumerable<string> sourceBranches, string targetBranch)
        {
            if (sourceBranches == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetBranch))
            {
                return;
            }

            foreach (var sourceBranch in sourceBranches)
            {
                if (sourceBranch == null)
                {
                    continue;
                }

                if (!Items.TryGetValue(sourceBranch, out var targetBranches))
                {
                    targetBranches = new List<string>();
                    Items[sourceBranch] = targetBranches;
                }

                if (!targetBranches.Any(existingTargetBranch => string.Equals(existingTargetBranch, targetBranch, StringComparison.OrdinalIgnoreCase)))
                {
                    targetBranches.Add(targetBranch);
                }
            }
        }

        public void Clear()
        {
            Items.Clear();
        }

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
