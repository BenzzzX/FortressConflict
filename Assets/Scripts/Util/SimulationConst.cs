public struct SimulationState
{
    // TODO look at batch sizes, huge batch size == 32
    public const int HumongousBatchSize = 1;
    public const int HugeBatchSize = 2;
    public const int BigBatchSize = 8;
    public const int SmallBatchSize = 16;
    public const int TinyBatchSize = 64;
    public const int MaxPathSize = 30;
    public const float Gravity = -20;
}
