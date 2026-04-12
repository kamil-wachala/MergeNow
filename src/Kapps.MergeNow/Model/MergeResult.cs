namespace MergeNow.Model
{
    public sealed class MergeResult
    {
        public MergeResultType ResultType { get; set; } = MergeResultType.Success;

        public string Summary { get; set; }

        public string Details { get; set; }
    }
}
