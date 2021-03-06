﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Dissertation.Util;

namespace Dissertation.NodeGraph
{
	public class Connection
	{
		public ConnectionPoint InPoint { get; private set; }
		public ConnectionPoint OutPoint { get; private set; }
		private Action<Connection> _onClick;

		public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint, Action<Connection> onClick)
		{
			InPoint = inPoint;
			OutPoint = outPoint;
			_onClick = onClick;
		}

		public Connection(BinaryReader reader, List<Node> allNodes, Action<Connection> onClickRemoveConnection)
		{
			Deserialise(reader, allNodes);

			_onClick = onClickRemoveConnection;
		}

		public Connection(BinaryReader reader, List<Node> allNodes)
		{
			Deserialise(reader, allNodes);
		}

#if UNITY_EDITOR
		public void Draw()
		{
			Handles.DrawBezier(InPoint.Rect.center, OutPoint.Rect.center,
				InPoint.Rect.center + (Vector2.left * 50.0f),
				OutPoint.Rect.center - (Vector2.left * 50.0f),
				Color.white, null, 2.0f);
#pragma warning disable 0618 //This is obsolete
			if(Handles.Button((InPoint.Rect.center + OutPoint.Rect.center) * 0.5f, Quaternion.identity, 4.0f, 8.0f, Handles.RectangleCap))
			{
				_onClick.InvokeSafe(this);
			}
#pragma warning restore
		}
#endif //UNITY_EDITOR

		public void Serialise(BinaryWriter writer)
		{
			writer.Write(InPoint.Node.UID);
			writer.Write(OutPoint.Node.UID);
		}

		protected virtual void Deserialise(BinaryReader reader, List<Node> allNodes)
		{
			int guid = reader.ReadInt32();
			Node inNode = allNodes.Find(node => node.UID == guid);
			InPoint = inNode.InPoint;

			guid = reader.ReadInt32();

			Node outNode = allNodes.Find(node => node.UID == guid);
			OutPoint = outNode.OutPoint;
		}
	}
}