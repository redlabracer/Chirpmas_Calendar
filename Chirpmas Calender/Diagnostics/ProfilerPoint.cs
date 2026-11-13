using System;
using System.Diagnostics;
using System.Threading;
using Unity.Profiling;

namespace ChirpmasCalendar.Diagnostics
{
    public sealed class ProfilerPoint
    {
        private readonly ProfilerMarker m_Marker;
        private long m_Count;
        private long m_TotalTicks;
        private long m_MaxTicks;

        internal ProfilerPoint(string name)
        {
            Name = name;
            m_Marker = new ProfilerMarker(name);
        }

        public string Name { get; }

        public long Count => Interlocked.Read(ref m_Count);

        public double TotalMilliseconds => ToMilliseconds(Interlocked.Read(ref m_TotalTicks));

        public double MaxMilliseconds => ToMilliseconds(Interlocked.Read(ref m_MaxTicks));

        public double AverageMilliseconds
        {
            get
            {
                long count = Count;
                return count == 0 ? 0 : TotalMilliseconds / count;
            }
        }

        public Scope Auto() => new Scope(this, true);

        public Scope Measure() => new Scope(this, false);

        /// <summary>Records a duration measured elsewhere (e.g. in the UI). Ignored while profiling is off.</summary>
        public void RecordMilliseconds(double milliseconds)
        {
            if (!ModProfiler.Enabled || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
                return;

            Record((long)(Math.Max(0, milliseconds) * Stopwatch.Frequency / 1000.0));
        }

        internal void Reset()
        {
            Interlocked.Exchange(ref m_Count, 0);
            Interlocked.Exchange(ref m_TotalTicks, 0);
            Interlocked.Exchange(ref m_MaxTicks, 0);
        }

        private void Record(long ticks)
        {
            Interlocked.Increment(ref m_Count);
            Interlocked.Add(ref m_TotalTicks, ticks);

            long max = Interlocked.Read(ref m_MaxTicks);
            while (ticks > max)
            {
                long previous = Interlocked.CompareExchange(ref m_MaxTicks, ticks, max);
                if (previous == max)
                    break;
                max = previous;
            }
        }

        private static double ToMilliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

        public readonly struct Scope : IDisposable
        {
            private readonly ProfilerPoint m_Point;
            private readonly bool m_UsesMarker;
            private readonly long m_Start;

            internal Scope(ProfilerPoint point, bool useMarker)
            {
                m_Point = point;
                m_UsesMarker = useMarker;
                if (useMarker)
                    point.m_Marker.Begin();

                m_Start = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (m_Point == null)
                    return;

                if (ModProfiler.Enabled)
                    m_Point.Record(Stopwatch.GetTimestamp() - m_Start);

                if (m_UsesMarker)
                    m_Point.m_Marker.End();
            }
        }
    }
}
