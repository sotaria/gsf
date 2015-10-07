//*******************************************************************************************************
//  ImmediateMeasurements.cs
//  Copyright © 2008 - TVA, all rights reserved - Gbtc
//
//  Build Environment: C#, Visual Studio 2008
//  Primary Developer: James R. Carroll
//      Office: PSO TRAN & REL, CHATTANOOGA - MR 2W-C
//       Phone: 423/751-2827
//       Email: jrcarrol@tva.gov
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  11/12/2004 - James R. Carroll
//       Initial version of source generated for Super Phasor Data Concentrator.
//  02/23/2006 - James R. Carroll
//       Classes abstracted for general use and added to TVA code library.
//  09/17/2008 - James R. Carroll
//       Converted to C#.
//  08/06/2009 - Josh Patterson
//      Edited Comments
//
//*******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

namespace TVA.Measurements
{
    /// <summary>
    /// Represents the absolute latest measurement values received by a <see cref="ConcentratorBase"/> implementation.
    /// </summary>
    [CLSCompliant(false)]
    public class ImmediateMeasurements : IDisposable
    {
        #region [ Members ]

        // Fields
        private ConcentratorBase m_parent;
        private Dictionary<MeasurementKey, TemporalMeasurement> m_measurements;
        private Dictionary<string, List<MeasurementKey>> m_taggedMeasurements;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        internal ImmediateMeasurements(ConcentratorBase parent)
        {
            m_parent = parent;
            m_parent.LagTimeUpdated += m_parent_LagTimeUpdated;
            m_parent.LeadTimeUpdated += m_parent_LeadTimeUpdated;
            m_measurements = new Dictionary<MeasurementKey, TemporalMeasurement>();
            m_taggedMeasurements = new Dictionary<string, List<MeasurementKey>>();
        }

        /// <summary>
        /// Releases the unmanaged resources before the <see cref="ImmediateMeasurements"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~ImmediateMeasurements()
        {
            Dispose(false);
        }

        #endregion

        #region [ Properties ]

        /// <summary>We retrieve adjusted measurement values within time tolerance of concentrator real-time.</summary>
        /// <param name="measurementID">An <see cref="UInt32"/> representing the measurement id.</param>
        /// <param name="source">A <see cref="String"/> indicating the source.</param>
        /// <returns>A <see cref="Double"/> representing the adjusted measurement value.</returns>
        public double this[uint measurementID, string source]
        {
            get
            {
                return this[new MeasurementKey(measurementID, source)];
            }
        }

        /// <summary>We retrieve adjusted measurement values within time tolerance of concentrator real-time.</summary>
        /// <param name="key">An <see cref="MeasurementKey"/> representing the measurement key.</param>
        /// <returns>A <see cref="Double"/> representing the adjusted measurement value.</returns>
        public double this[MeasurementKey key]
        {
            get
            {
                return Measurement(key).GetAdjustedValue(m_parent.RealTime);
            }
        }

        /// <summary>Returns key collection of measurement keys.</summary>
        public Dictionary<MeasurementKey, TemporalMeasurement>.KeyCollection MeasurementKeys
        {
            get
            {
                return m_measurements.Keys;
            }
        }

        /// <summary>Returns key collection for measurement tags.</summary>
        public Dictionary<string, List<MeasurementKey>>.KeyCollection Tags
        {
            get
            {
                return m_taggedMeasurements.Keys;
            }
        }

        /// <summary>Returns the minimum value of all measurements.</summary>
        /// <remarks>This is only useful if all measurements represent the same type of measurement.</remarks>
        public double Minimum
        {
            get
            {
                double minValue = double.MaxValue;
                double measurement;

                lock (m_measurements)
                {
                    foreach (MeasurementKey key in m_measurements.Keys)
                    {
                        measurement = this[key];
                        if (!double.IsNaN(measurement))
                        {
                            if (measurement < minValue)
                            {
                                minValue = measurement;
                            }
                        }
                    }
                }

                return minValue;
            }
        }

        /// <summary>Returns the maximum value of all measurements.</summary>
        /// <remarks>This is only useful if all measurements represent the same type of measurement.</remarks>
        public double Maximum
        {
            get
            {
                double maxValue = double.MinValue;
                double measurement;

                lock (m_measurements)
                {
                    foreach (MeasurementKey key in m_measurements.Keys)
                    {
                        measurement = this[key];
                        if (!double.IsNaN(measurement))
                        {
                            if (measurement > maxValue)
                            {
                                maxValue = measurement;
                            }
                        }
                    }
                }

                return maxValue;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all the resources used by the <see cref="ImmediateMeasurements"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ImmediateMeasurements"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (m_parent != null)
                        {
                            m_parent.LagTimeUpdated -= m_parent_LagTimeUpdated;
                            m_parent.LeadTimeUpdated -= m_parent_LeadTimeUpdated;
                        }
                        m_parent = null;

                        if (m_measurements != null)
                        {
                            m_measurements.Clear();
                        }
                        m_measurements = null;

                        if (m_taggedMeasurements != null)
                        {
                            m_taggedMeasurements.Clear();
                        }
                        m_taggedMeasurements = null;
                    }
                }
                finally
                {
                    m_disposed = true;  // Prevent duplicate dispose.
                }
            }
        }

        /// <summary>Returns measurement key list of specified tag, if it exists.</summary>
        /// <param name="tag">A <see cref="String"/> that indicates the tag to use.</param>
        /// <returns>A collection of measurement keys.</returns>
        public ReadOnlyCollection<MeasurementKey> TaggedMeasurementKeys(string tag)
        {
            return new ReadOnlyCollection<MeasurementKey>(m_taggedMeasurements[tag]);
        }

        /// <summary>We only store a new measurement value that is newer than the cached value.</summary>
        internal void UpdateMeasurementValue(IMeasurement newMeasurement)
        {
            Measurement(newMeasurement.Key).SetValue(newMeasurement.Timestamp, newMeasurement.Value);
        }

        /// <summary>Retrieves the specified immediate temporal measurement, creating it if needed.</summary>
        /// <param name="measurementID">An <see cref="UInt32"/> representing the measurement id.</param>
        /// <param name="source">A <see cref="String"/> indicating the source.</param>
        /// <returns>A <see cref="TemporalMeasurement"/> object.</returns>
        public TemporalMeasurement Measurement(uint measurementID, string source)
        {
            return Measurement(new MeasurementKey(measurementID, source));
        }

        /// <summary>Retrieves the specified immediate temporal measurement, creating it if needed.</summary>
        /// <param name="key">A <see cref="MeasurementKey"/> object indicating the key to use.</param>
        /// <returns>A <see cref="TemporalMeasurement"/> object.</returns>
        public TemporalMeasurement Measurement(MeasurementKey key)
        {
            lock (m_measurements)
            {
                TemporalMeasurement value;

                if (!m_measurements.TryGetValue(key, out value))
                {
                    // Create new temporal measurement if it doesn't exist
                    value = new TemporalMeasurement(key.ID, key.Source, double.NaN, m_parent.RealTime, m_parent.LagTime, m_parent.LeadTime);
                    m_measurements.Add(key, value);
                }

                return value;
            }
        }

        /// <summary>Defines tagged measurements from a data table.</summary>
        /// <remarks>Expects tag field to be aliased as "Tag", measurement ID field to be aliased as "ID" and source field to be aliased as "Source".</remarks>
        /// <param name="taggedMeasurements">A <see cref="DataTable"/> to use for defining the tagged measurements.</param>
        public void DefineTaggedMeasurements(DataTable taggedMeasurements)
        {
            foreach (DataRow row in taggedMeasurements.Rows)
            {
                AddTaggedMeasurement(row["Tag"].ToNonNullString("_tag_"), new MeasurementKey(uint.Parse(row["ID"].ToNonNullString(uint.MaxValue.ToString())), row["Source"].ToNonNullString("__")));
            }
        }

        /// <summary>Associates a new measurement ID with a tag, creating the new tag if needed.</summary>
        /// <remarks>Allows you to define "grouped" points so you can aggregate certain measurements.</remarks>
        /// <param name="key">A <see cref="MeasurementKey"/> to associate with the tag.</param>
        /// <param name="tag">A <see cref="String"/> to represent the key.</param>
        public void AddTaggedMeasurement(string tag, MeasurementKey key)
        {
            // Check for new tag
            if (!m_taggedMeasurements.ContainsKey(tag))
                m_taggedMeasurements.Add(tag, new List<MeasurementKey>());

            // Add measurement to tag's measurement list
            List<MeasurementKey> measurements = m_taggedMeasurements[tag];

            if (measurements.BinarySearch(key) < 0)
            {
                measurements.Add(key);
                measurements.Sort();
            }
        }

        /// <summary>Calculates an average of all measurements.</summary>
        /// <remarks>This is only useful if all measurements represent the same type of measurement.</remarks>
        /// <param name="count">An <see cref="Int32"/> value to get the count of values averaged.</param>
        /// <returns>A <see cref="Double"/> value representing the average of the measurements.</returns>
        public double CalculateAverage(ref int count)
        {
            double measurement;
            double total = 0.0D;

            lock (m_measurements)
            {
                foreach (MeasurementKey key in m_measurements.Keys)
                {
                    measurement = this[key];
                    if (!double.IsNaN(measurement))
                    {
                        total += measurement;
                        count++;
                    }
                }
            }

            return total / count;
        }

        /// <summary>Calculates an average of all measurements associated with the specified tag.</summary>
        /// <param name="count">An <see cref="Int32"/> value to get the count of values averaged.</param>
        /// <param name="tag">The type of measurements to average.</param>
        /// <returns>A <see cref="Double"/> value representing the average of the tags.</returns>
        public double CalculateTagAverage(string tag, ref int count)
        {
            double measurement;
            double total = 0.0D;

            foreach (MeasurementKey key in m_taggedMeasurements[tag])
            {
                measurement = this[key];
                if (!double.IsNaN(measurement))
                {
                    total += measurement;
                    count++;
                }
            }

            return total / count;
        }

        /// <summary>Returns the minimum value of all measurements associated with the specified tag.</summary>
        /// <returns>A <see cref="Double"/> value representing the tag minimum.</returns>
        /// <param name="tag">The tag group to evaluate.</param>
        public double TagMinimum(string tag)
        {
            double minValue = double.MaxValue;
            double measurement;

            foreach (MeasurementKey key in m_taggedMeasurements[tag])
            {
                measurement = this[key];
                if (!double.IsNaN(measurement))
                {
                    if (measurement < minValue)
                    {
                        minValue = measurement;
                    }
                }
            }

            return minValue;
        }

        /// <summary>Returns the maximum value of all measurements associated with the specified tag.</summary>
        /// <returns>A <see cref="Double"/> value representing the tag maximum.</returns>
        /// <param name="tag">The tag group to evaluate.</param>
        public double TagMaximum(string tag)
        {
            double maxValue = double.MinValue;
            double measurement;

            foreach (MeasurementKey key in m_taggedMeasurements[tag])
            {
                measurement = this[key];
                if (!double.IsNaN(measurement))
                {
                    if (measurement > maxValue)
                    {
                        maxValue = measurement;
                    }
                }
            }

            return maxValue;
        }

        // We dyanmically respond to real-time changes in lead or lag time...
        private void m_parent_LagTimeUpdated(double lagTime)
        {
            lock (m_measurements)
            {
                foreach (MeasurementKey key in m_measurements.Keys)
                {
                    Measurement(key).LagTime = lagTime;
                }
            }
        }

        private void m_parent_LeadTimeUpdated(double leadTime)
        {
            lock (m_measurements)
            {
                foreach (MeasurementKey key in m_measurements.Keys)
                {
                    Measurement(key).LeadTime = leadTime;
                }
            }
        }

        #endregion
    }
}