using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoomNodeSO : ScriptableObject
{
     [HideInInspector] public string id;
     [HideInInspector] public List<string> parentRoomNodeIDList = new List<string>();
     [HideInInspector] public List<string> childRoomNodeIDList = new List<string>();
     [HideInInspector] public RoomNodeGraphSO roomNodeGraph;
     public RoomNodeTypeSO roomNodeType;
     [HideInInspector] public RoomNodeTypeListSO roomNodeTypeList;

     #region Editor Code

#if UNITY_EDITOR

     [HideInInspector] public Rect rect;
     [HideInInspector] public bool isLeftClickDragging = false;
     [HideInInspector] public bool isSelected = true;

     public void Initialise(Rect rect, RoomNodeGraphSO nodeGraph, RoomNodeTypeSO roomNodeType)
     {
          this.rect = rect;
          this.id = Guid.NewGuid().ToString();
          this.name = "RoomNode";
          this.roomNodeGraph = nodeGraph;
          this.roomNodeType = roomNodeType;
          
          // Load room node type list
          roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
     }

     /// <summary>
     /// Draw node with the nodeStyle
     /// </summary>
     /// <param name="nodeStyle"></param>
     public void Draw(GUIStyle nodeStyle)
     {
          // Draw node box using begin area
          GUILayout.BeginArea(rect, nodeStyle);
          
          // Start region to detect popup selection changes
          EditorGUI.BeginChangeCheck();
          
          // if the room node has a parent or is of type entrance then display a label else display a popup
          if (parentRoomNodeIDList.Count > 0 || roomNodeType.isEntrance)
          {
               EditorGUILayout.LabelField(roomNodeType.roomNodeTypeName);
          }
          else
          {
               // Display a popup using the RoomNodeType name values that can be selected from (Default to the currently set roomNodeType)
               int selected = roomNodeTypeList.list.FindIndex(x => x == roomNodeType);
               int selection = EditorGUILayout.Popup("", selected, GetRoomNodeTypesToDisplay());

               roomNodeType = roomNodeTypeList.list[selection];
               
               // If the room type selection has changed making child connections potentially invalid
               if (roomNodeTypeList.list[selected].isCorridor && !roomNodeTypeList.list[selection].isCorridor || !roomNodeTypeList.list[selected].isCorridor && roomNodeTypeList.list[selection].isCorridor ||
                   !roomNodeTypeList.list[selected].isBossRoom && roomNodeTypeList.list[selection].isBossRoom)
               {
                    for (int i = childRoomNodeIDList.Count - 1; i >= 0; i--)
                    {
                         RoomNodeSO childRoomNode = roomNodeGraph.GetRoomNode(childRoomNodeIDList[i]);

                         if (childRoomNode != null)
                         {
                              RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);
                              childRoomNode.RemoveParentRoomNodeIDFromRoomNode(id);
                         }
                    }
               }
               
          }
          
          if (EditorGUI.EndChangeCheck())
               EditorUtility.SetDirty(this);
          GUILayout.EndArea();
     }

     /// <summary>
     /// Populate a string array with the room node types to display that can be selected
     /// </summary>
     /// <returns></returns>
     public string[] GetRoomNodeTypesToDisplay()
     {
          string[] roomArray = new string[roomNodeTypeList.list.Count];

          for (int i = 0; i < roomNodeTypeList.list.Count; i++)
          {
               if (roomNodeTypeList.list[i].displayInNodeGraphEditor)
               {
                    roomArray[i] = roomNodeTypeList.list[i].roomNodeTypeName;
               }
          }
          
          return roomArray;
     }

     /// <summary>
     /// Process events for the node
     /// </summary>
     /// <param name="currentEvent"></param>
     public void ProcessEvents(Event currentEvent)
     {
          switch (currentEvent.type)
          {
               case EventType.MouseDown:
                    ProcessMouseDownEvent(currentEvent);
                    break;
               case EventType.MouseUp:
                    ProcessMouseUpEvent(currentEvent);
                    break;
               case EventType.MouseDrag:
                    ProcessMouseDragEvent(currentEvent);
                    break;
               
               default:
                    break;
          }
     }
     
     private void ProcessMouseDownEvent(Event currentEvent)
     {
          // left  click down
          if (currentEvent.button == 0)
          {
               ProcessLeftClickDownEvent();
          }
          else if (currentEvent.button == 1)
          {
               ProcessRightClickDownEvent(currentEvent);
          }
     }

     /// <summary>
     /// Process right click down
     /// </summary>
     /// <param name="currentEvent"></param>
     private void ProcessRightClickDownEvent(Event currentEvent)
     {
          roomNodeGraph.SetNodeToDrawConnectionLineFrom(this, currentEvent.mousePosition);
     }

     /// <summary>
     /// Process left click down event
     /// </summary>
     private void ProcessLeftClickDownEvent()
     {
          Selection.activeObject = this;
          
          // Toggle mode selection
          if (isSelected == true)
          {
               isSelected = false;
          }
          else
          {
               isSelected = true;
          }
     }

     private void ProcessMouseUpEvent(Event currentEvent)
     {
          // If left click up
          if (currentEvent.button == 0)
          {
               ProcessLeftClickUpEvent();
          }
     }

     private void ProcessLeftClickUpEvent()
     {
          if (isLeftClickDragging)
          {
               isLeftClickDragging = false;
          }
     }
     
     private void ProcessMouseDragEvent(Event currentEvent)
     {
          // process left click drag event
          if (currentEvent.button == 0)
          {
               ProcessLeftMouseDragEvent(currentEvent);
          }
     }

     private void ProcessLeftMouseDragEvent(Event currentEvent)
     {
          isLeftClickDragging = true;
          DragNode(currentEvent.delta);
          GUI.changed = true;
     }

     public void DragNode(Vector2 delta)
     {
          rect.position += delta;
          EditorUtility.SetDirty(this);
     }

     /// <summary>
     /// Add childID to the node (returns true if the node has been added, false otherwise)
     /// </summary>
     /// <param name="childID"></param>
     /// <returns></returns>
     public bool AddChildRoomNodeIDToRoomNode(string childID)
     {
          // Check child node can be added validly to parent
          if (IsChildRoomValid(childID))
          {
               childRoomNodeIDList.Add(childID);
               return true;
          }

          return false;
     }

     /// <summary>
     /// Check the child node can be validly added to the parent node - return true if it can otherwise return false
     /// </summary>
     /// <param name="childID"></param>
     /// <returns></returns>
     private bool IsChildRoomValid(string childID)
     {
          bool isConnectedBossNodeAlready = false;
          // Check if there is already a connected boos room node in the graph
          foreach (RoomNodeSO roomNode in roomNodeGraph.roomNodeList)
          {
               if (roomNode.roomNodeType.isBossRoom && roomNode.parentRoomNodeIDList.Count > 0)
                    isConnectedBossNodeAlready = true;
          }
          
          // if the child node has a type of boss room and there is already a connected boss room node return false
          if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isBossRoom && isConnectedBossNodeAlready)
               return false;
          
          // If the child node has a type of none then return false
          if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isNone)
               return false;
          
          // if the node already has a child with this child ID return false
          if (childRoomNodeIDList.Contains(childID))
               return false;
          
          // If this node ID and the child ID are the same return false
          if (id == childID)
               return false;
          
          // If child ID is already in the parentID list return false
          if (parentRoomNodeIDList.Contains(childID))
               return false;
          
          // If the child node already has a parent
          if (roomNodeGraph.GetRoomNode(childID).parentRoomNodeIDList.Count > 0)
               return false;
          
          // If the child is a corridor and this node is a corridor return false
          if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && roomNodeType.isCorridor)
               return false;
          
          // If child is not a corridor and this node is not a corridor return false
          if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && !roomNodeType.isCorridor)
               return false;
          
          // If adding a corridor check that this node has < the maximum permitted child corridors
          if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count >= Settings.maxChildCorridors)
               return false;
          
          // If the child room is an entrance return false - the entrance must always be the top level parent node
          if (roomNodeGraph.GetRoomNode(childID).roomNodeType.isEntrance)
               return false;
          
          // If adding a room to a corridor check that this corridor node doesnt already have a room added
          if (!roomNodeGraph.GetRoomNode(childID).roomNodeType.isCorridor && childRoomNodeIDList.Count > 0)
               return false;

          return true;
     }

     /// <summary>
     /// Add parentID to the node (returns true if the node has been added, false otherwise)
     /// </summary>
     /// <param name="parentID"></param>
     /// <returns></returns>
     public bool AddParentRoomNodeIDToRoomNode(string parentID)
     {
          parentRoomNodeIDList.Add(parentID);
          return true;
     }

     /// <summary>
     /// Removes childID from the node (returns true if the node has been removed, false otherwise)
     /// </summary>
     /// <param name="childID"></param>
     /// <returns></returns>
     public bool RemoveChildRoomNodeIDFromRoomNode(string childID)
     {
          // if the node contains the child then remove it
          if (childRoomNodeIDList.Contains(childID))
          {
               childRoomNodeIDList.Remove(childID);
               return true;
          }

          return false;
     }

     /// <summary>
     /// Removes parentID from the node (returns true if the node has been removed, false otherwise)
     /// </summary>
     /// <param name="parentID"></param>
     /// <returns></returns>
     public bool RemoveParentRoomNodeIDFromRoomNode(string parentID)
     {
          if (parentRoomNodeIDList.Contains(parentID))
          {
               parentRoomNodeIDList.Remove(parentID);
               return true;
          }

          return false;
     }
     

#endif

     #endregion

}
