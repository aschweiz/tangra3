﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tangra.Model.Astro;
using Tangra.Model.Config;
using Tangra.Model.Image;

namespace Tangra.VideoOperations.LightCurves.Tracking
{
	public abstract class BaseTracker : ITracker
	{
		private List<TrackedObjectConfig> m_TrackedObjectsConfig;
		protected List<ITrackedObject> m_TrackedObjects = new List<ITrackedObject>();

		protected Dictionary<int, List<List<double>>> m_PastMeasuredRelativeDistances = new Dictionary<int, List<List<double>>>();
		protected Dictionary<int, List<double>> m_PastAverageRelativeDistances = new Dictionary<int, List<double>>();

		protected ITrackedObject m_OccultedStar;
		protected bool m_IsFullDisappearance;
		protected bool m_IsFieldRotation;

		internal BaseTracker(List<TrackedObjectConfig> measuringStars)
		{
			if (measuringStars.Count > 4)
				throw new NotSupportedException("Only up to 4 tracked objects are supported by this Tracker.");

			m_TrackedObjectsConfig = measuringStars;

			for (int i = 0; i < m_TrackedObjectsConfig.Count; i++)
			{
				var trackedObject = new TrackedObjectLight((byte) i, m_TrackedObjectsConfig[i]);
				m_TrackedObjects.Add(trackedObject);

				if (m_TrackedObjectsConfig[i].TrackingType == TrackingType.OccultedStar)
					m_OccultedStar = trackedObject;
			}

			m_IsFullDisappearance = LightCurveReductionContext.Instance.FullDisappearance;
			m_IsFieldRotation = LightCurveReductionContext.Instance.FieldRotation;
		}

		public bool IsTrackedSuccessfully { get; protected set; }

		public float[] RefinedFWHM
		{
			get
			{
				return m_TrackedObjects
					.Cast<TrackedObjectLight>()
					.Select(x => x.RefinedFWHM)
					.ToArray();
			}
		}

		public void InitializeNewTracking()
		{
			RefinedAverageFWHM = float.NaN;
			MedianValue = uint.MinValue;
			m_PastMeasuredRelativeDistances.Clear();
			m_PastAverageRelativeDistances.Clear();

			for (int i = 0; i < m_TrackedObjects.Count; i++)
			{
				((TrackedObjectLight)m_TrackedObjects[i]).InitializeNewTracking();

				var averlist = new List<double>();
				var meaList = new List<List<double>>();
				for (int j = 0; j < m_TrackedObjects.Count; j++)
				{
					meaList.Add(new List<double>());
					averlist.Add(double.NaN);
				}
				m_PastMeasuredRelativeDistances.Add(i, meaList);
				m_PastAverageRelativeDistances.Add(i, averlist);
			}

			// No preliminary refining used by the AdHockTracker
			RefiningPercentageWorkLeft = 0;
		}

		public List<ITrackedObject> TrackedObjects
		{
			get { return m_TrackedObjects; }
		}

		public float RefinedAverageFWHM { get; protected set; }

		public float PositionTolerance
		{
			get
			{
				return !float.IsNaN(RefinedAverageFWHM) 
					? 2 * RefinedAverageFWHM
					: m_OccultedStar.OriginalObject.PositionTolerance;
			}
		}

		public uint MedianValue { get; protected set; }

		public float RefiningPercentageWorkLeft { get; protected set; }

		public void DoManualFrameCorrection(int manualTrackingDeltaX, int manualTrackingDeltaY)
		{
			throw new NotImplementedException();
		}

		public void BeginMeasurements(IAstroImage astroImage)
		{
			// Nothing special to do as there is no premilinary refining used by the AdHocTracker
		}

		public virtual void NextFrame(int frameNo, IAstroImage astroImage)
		{
			IsTrackedSuccessfully = false;
		}
	}
}