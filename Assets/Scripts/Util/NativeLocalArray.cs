using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

[StructLayout(LayoutKind.Sequential)]
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
unsafe public struct NativeLocalArray<T> : IDisposable
    where T : struct
{
    [NativeDisableUnsafePtrRestriction]
    void *m_Buffer;

    int m_Length;

    int m_AlignedBytes;

    [NativeSetThreadIndex]
    int m_ThreadIndex;

    int sizeofT;
    
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle m_Safety;
    // The dispose sentinel tracks memory leaks. It is a managed type so it is cleared to null when scheduling a job
    // The job cannot dispose the container, and no one else can dispose it until the job has run, so it is ok to not pass it along
    // This attribute is required, without it this NativeContainer cannot be passed to a job; since that would give the job access to a managed object
    [NativeSetClassTypeToNullOnSchedule]
    DisposeSentinel m_DisposeSentinel;
#endif

    // Keep track of where the memory for this was allocated
    Allocator m_AllocatorLabel;

    public int Length { get { return m_Length; } }

    public bool IsCreated { get { return m_Buffer != null; } }

    public NativeLocalArray(int length, Allocator lable, NativeArrayOptions option)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!UnsafeUtility.IsBlittable<T>())
            throw new ArgumentException(string.Format("{0} used in NativeQueue<{0}> must be blittable", typeof(int)));
#endif
        m_ThreadIndex = 0;
        m_AllocatorLabel = lable;
        sizeofT = UnsafeUtility.SizeOf<T>();
        m_Length = length;
        int bytesPerData = length * sizeofT;
        m_AlignedBytes = (bytesPerData / JobsUtility.CacheLineSize + 1) * JobsUtility.CacheLineSize;
        m_Buffer = UnsafeUtility.Malloc(m_AlignedBytes * JobsUtility.MaxJobThreadCount, 4, lable);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0);
#endif
        if (option == NativeArrayOptions.ClearMemory)
            UnsafeUtility.MemClear(m_Buffer, length * sizeofT);
    }

    public T this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            void* data = ((byte*)m_Buffer + m_AlignedBytes * m_ThreadIndex + index * sizeofT);
            T value;
            UnsafeUtility.CopyPtrToStructure(data, out value);
            return value;
        }
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            void* data = ((byte*)m_Buffer + m_AlignedBytes * m_ThreadIndex + index * sizeofT);
            UnsafeUtility.CopyStructureToPtr<T>(ref value, data);
        }
    }

    public void Dispose()
    {
        // Let the dispose sentinel know that the data has been freed so it does not report any memory leaks
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif

        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        m_Buffer = null;
    }
}