using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline.DataRecorder
{
    public class RecordManager : Singleton<RecordManager>
    {
        public bool ShouldRecord { get; set; }

        public List<DataRecorder> dataRecorders { get; private set; } = new List<DataRecorder>();
        public List<string> dataRecorderNames { get; private set; } = new List<string>();

        public DataRecorder currentRecorder;
        public Record currentRecord;

        private Record[] selectedFrameRecords;

        public void AddDataRecorder(DataRecorder dataRecorder)
        {
            dataRecorders.Add(dataRecorder);
            dataRecorderNames.Add(dataRecorder.gameObject.name);
        }

        public void RemoveDataRecorder(DataRecorder dataRecorder)
        {
            var index = dataRecorders.IndexOf(dataRecorder);
            if (index >= 0)
            {
                dataRecorders.RemoveAt(index);
                dataRecorderNames.RemoveAt(index);
                if (currentRecorder == dataRecorder)
                {
                    currentRecorder = null;
                    currentRecord = null;
                }
            }
        }

        public void SetCurrentRecorder(int selectedIndex)
        {
            currentRecorder = dataRecorders[selectedIndex];
        }

        public int SetCurrentRecorder(DataRecorder recorder)
        {
            int index = -1;
            for (int i = 0; i < dataRecorders.Count; i++)
            {
                if (dataRecorders[i] == recorder)
                {
                    index = i;
                    break;
                }
            }
            if (index != -1)
                currentRecorder = dataRecorders[index];
            return index;
        }

        public void SetCurrentRecord(int selectedIndex)
        {
            currentRecord = currentRecorder.records[selectedIndex];
        }

#if UNITY_EDITOR

        //The code below processes data for UI toolkit's tree view and since the tree forces you to construct the data
        //from the liefs and go up (It cannot add children to nodes after construction). 
        //This is recursive and a bit weird.

        public List<UnityEngine.UIElements.TreeViewItemData<string>> GetRoots(Record record)
        {
            List<UnityEngine.UIElements.TreeViewItemData<string>> roots = new List<UnityEngine.UIElements.TreeViewItemData<string>>();
            int id = 0;
            for (int i = 0; i < record.subRecords.Count;)
            {
                SubRecord sub = record.subRecords[i];
                if (sub.indentationLevel == 0)
                {
                    List<TreeViewItemData<string>> children = new List<TreeViewItemData<string>>();
                    int myIndex = i;
                    int myId = id;
                    while (++i < record.subRecords.Count && record.subRecords[i].indentationLevel != 0)
                    {
                        if (record.subRecords[i].indentationLevel == 1)
                        {
                            id++;
                            children.Add(ProcessItem(record.subRecords, i, ref id));
                        }
                    }
                    roots.Add(new TreeViewItemData<string>(myId, sub.text, children));
                }
                else
                {
                    i++;
                }
                id++;
            }
            return roots;
        }

        private TreeViewItemData<string> ProcessItem(List<SubRecord> subRecords, int startingIndex, ref int id)
        {
            //if you do not have children then
            //make yourself and add yourself to your parent
            if (startingIndex == subRecords.Count - 1 || subRecords[startingIndex + 1].indentationLevel <= subRecords[startingIndex].indentationLevel)
                return new TreeViewItemData<string>(id, subRecords[startingIndex].text);
            int myIndex = startingIndex;
            int myId = id;
            List<TreeViewItemData<string>> children = new List<TreeViewItemData<string>>();
            while (++startingIndex < subRecords.Count && subRecords[startingIndex].indentationLevel > subRecords[myIndex].indentationLevel)
            {
                if (subRecords[startingIndex].indentationLevel == subRecords[myIndex].indentationLevel + 1)
                {
                    id++;
                    children.Add(ProcessItem(subRecords, startingIndex, ref id));
                }
            }
            return new TreeViewItemData<string>(myId, subRecords[myIndex].text, children);
        }

        private void OnDrawGizmos()
        {
            if (currentRecord != null)
            {
                int frame = -1;
                if (selectedFrameRecords != null)
                {
                    for (int i = 0; i < selectedFrameRecords.Length; i++)
                    {
                        if (selectedFrameRecords[i] != null)
                        {
                            frame = selectedFrameRecords[i].frameCount;
                            break;
                        }
                    }
                }
                if (frame != currentRecord.frameCount)
                {
                    selectedFrameRecords = dataRecorders.Select(x => x.records.FirstOrDefault(_ => _.frameCount == currentRecord.frameCount)).ToArray();
                }
                
                foreach (var rec in selectedFrameRecords)
                {
                    if (rec == null)
                        continue;
                    foreach (var shape in rec.shapes)
                    {
                        Gizmos.color = shape.color;
                        switch (shape.Type)
                        {
                            case ShapeType.Sphere:
                                Gizmos.DrawSphere(shape.position, shape.size);
                                break;
                            case ShapeType.Cube:
                                Gizmos.DrawCube(shape.position, Vector3.one * shape.size);
                                break;
                            case ShapeType.WiredSphere:
                                Gizmos.DrawWireSphere(shape.position, shape.size);
                                break;
                            case ShapeType.WiredCube:
                                Gizmos.DrawWireCube(shape.position, Vector3.one * shape.size);
                                break;
                            default:
                                break;

                        }
                    }
                }

            }
        }
#endif
    }
}