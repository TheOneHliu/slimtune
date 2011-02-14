﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using UICore;

namespace SlimTuneUI.CoreVis
{
	[DisplayName("Hotspots")]
	public partial class HotSpots : UserControl, IVisualizer
	{
		ProfilerWindowBase m_mainWindow;
		Connection m_connection;

		ListBox m_rightMost;

		public HotSpots()
		{
			InitializeComponent();
			m_rightMost = HotspotsList;
		}

		public string DisplayName
		{
			get { return "Hotspots"; }
		}

		public bool Initialize(ProfilerWindowBase mainWindow, Connection connection)
		{
			if(mainWindow == null)
				throw new ArgumentNullException("mainWindow");
			if(connection == null)
				throw new ArgumentNullException("connection");

			m_mainWindow = mainWindow;
			m_connection = connection;

			UpdateHotspots();
			return true;
		}

		public void Show(Control.ControlCollection parent)
		{
			this.Dock = DockStyle.Fill;
			parent.Add(this);
		}

		public void OnClose()
		{
		}

		private void UpdateHotspots()
		{
			HotspotsList.Items.Clear();
			using(var session = m_mainWindow.OpenActiveSnapshot())
			{
				//find the functions that consumed the most time-exclusive. These are hotspots.
				var query = session.CreateQuery("from Call as call where call.ChildId = 0 inner join fetch call.Parent order by Time desc");
				query.SetMaxResults(20);
				var hotspots = query.List<Call>();
				foreach(var call in hotspots)
				{
					var func = call.Parent;
					var parentName = func.Name;
					HotspotsList.Items.Add(call);
				}
			}
		}

		private bool UpdateParents(Call child, ListBox box)
		{
			using(var session = m_mainWindow.OpenActiveSnapshot())
			{
				session.Lock(child.Parent, NHibernate.LockMode.None);
				var parents = child.Parent.CallsAsChild;
				foreach(var call in parents)
				{
					if(call.ParentId == 0)
						return false;

					var func = call.Parent;
					var parentName = func.Name;
					box.Items.Add(call);
				}
			}

			return true;
		}

		private void RefreshTimer_Tick(object sender, EventArgs e)
		{
			//UpdateHotspots();
		}

		private void RemoveList(ListBox list)
		{
			if(list.Tag != null)
				RemoveList(list.Tag as ListBox);

			ScrollPanel.Controls.Remove(list);
		}

		private void CallList_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListBox list = sender as ListBox;
			if(list.Tag != null)
				RemoveList(list.Tag as ListBox);
			m_rightMost = list;

			//create a new listbox to the right
			ListBox lb = new ListBox();
			lb.Size = m_rightMost.Size;
			lb.Location = new Point(m_rightMost.Right + 4, 4);
			lb.IntegralHeight = false;
			lb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
			lb.FormattingEnabled = true;
			lb.Format += new ListControlConvertEventHandler(CallList_Format);
			lb.SelectedIndexChanged += new EventHandler(CallList_SelectedIndexChanged);

			if(UpdateParents(m_rightMost.SelectedItem as Call, lb))
			{
				ScrollPanel.Controls.Add(lb);
				ScrollPanel.ScrollControlIntoView(lb);
				m_rightMost.Tag = lb;
				m_rightMost = lb;
			}
		}

		private void CallList_Format(object sender, ListControlConvertEventArgs e)
		{
			Call call = e.ListItem as Call;
			e.Value = call.Parent.Name;
		}
	}
}