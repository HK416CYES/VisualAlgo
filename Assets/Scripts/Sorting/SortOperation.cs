namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 排序算法发出的可视化操作类型。
    /// </summary>
    public enum SortOperationType
    {
        Compare, // 比较操作
        Swap,    // 交换操作
        Assign, // 将一个高度值写入指定位置，主要用于归并排序
        Insert, // 将一个元素插入到目标位置，并平移中间元素
        Pivot, // 标记快速排序当前分区的轴
        MarkSorted // 标记某个索引已确认有序
    }

    /// <summary>
    /// 由柱状图控制器消耗的不可变命令，用于可视化展示比较或交换步骤。
    /// </summary>
    public readonly struct SortOperation
    {
        /// <summary>
        /// 构造一个新的排序操作。
        /// </summary>
        /// <param name="type">操作类型（比较或交换）</param>
        /// <param name="firstIndex">第一个元素的索引</param>
        /// <param name="secondIndex">第二个元素的索引</param>
        public SortOperation(SortOperationType type, int firstIndex, int secondIndex, float value = 0f)
        {
            Type = type;
            FirstIndex = firstIndex;
            SecondIndex = secondIndex;
            Value = value;
        }

        /// <summary>
        /// 当前发出的操作类型。
        /// </summary>
        public SortOperationType Type { get; }

        /// <summary>
        /// 涉及的第一个元素的对应索引。
        /// </summary>
        public int FirstIndex { get; }

        /// <summary>
        /// 涉及的第二个元素的对应索引。
        /// </summary>
        public int SecondIndex { get; }

        /// <summary>
        /// 写入类操作携带的目标高度值；非写入操作忽略该值。
        /// </summary>
        public float Value { get; }

        /// <summary>
        /// 快速创建一个用于比较两个元素的操作。
        /// </summary>
        /// <param name="firstIndex">第一个比较元素的索引</param>
        /// <param name="secondIndex">第二个比较元素的索引</param>
        /// <returns>返回比较类型的排序操作实例</returns>
        public static SortOperation Compare(int firstIndex, int secondIndex)
        {
            return new SortOperation(SortOperationType.Compare, firstIndex, secondIndex);
        }

        /// <summary>
        /// 快速创建一个用于交换两个元素的操作。
        /// </summary>
        /// <param name="firstIndex">第一个交换元素的索引</param>
        /// <param name="secondIndex">第二个交换元素的索引</param>
        /// <returns>返回交换类型的排序操作实例</returns>
        public static SortOperation Swap(int firstIndex, int secondIndex)
        {
            return new SortOperation(SortOperationType.Swap, firstIndex, secondIndex);
        }

        /// <summary>
        /// 快速创建一个插入操作。firstIndex 是被移动元素，secondIndex 是插入目标位置。
        /// </summary>
        public static SortOperation Insert(int fromIndex, int toIndex)
        {
            return new SortOperation(SortOperationType.Insert, fromIndex, toIndex);
        }

        /// <summary>
        /// 快速创建一个用于标记快速排序轴元素的操作。
        /// </summary>
        /// <param name="index">当前分区轴元素所在的索引。</param>
        /// <returns>返回轴标记类型的排序操作实例</returns>
        public static SortOperation Pivot(int index)
        {
            return new SortOperation(SortOperationType.Pivot, index, index);
        }

        /// <summary>
        /// 快速创建一个用于标记某个索引已确认有序的操作。
        /// </summary>
        /// <param name="index">已经确认不会再发生交换的位置索引。</param>
        /// <returns>返回标记已排序类型的排序操作实例</returns>
        public static SortOperation MarkSorted(int index)
        {
            return new SortOperation(SortOperationType.MarkSorted, index, index);
        }
    }
}
