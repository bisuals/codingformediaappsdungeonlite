using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline.DataRecorder
{
    public class DataRecorder : MonoBehaviour
    {
        public List<Record> records { get; private set; } = new List<Record>();

        private bool isRegistered;

        private void Start()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
            {
                Destroy(this);
                return;
            }
            else
            {
                RecordManager.Instance.AddDataRecorder(this);
                isRegistered = true;
            }
        }

        private void OnDestroy()
        {
            records?.Clear();
            if (isRegistered)
            {
                RecordManager.Instance?.RemoveDataRecorder(this);
                isRegistered = false;
            }
        }

        public void AddSubRecord(SubRecord subRecord, int index)
        {
            RecordManager manager = RecordManager.Instance;
            if (manager == null || !manager.ShouldRecord)
                return;
                if (index >= 0 && index < records.Count)
                {
                    var rec = records[index];
                    if (rec != null)
                    {
                        subRecord.record = rec;
                        rec.subRecords.Add(subRecord);
                    }
                }
        }

        /// <summary>
        /// Returns the index of the record of the current frame, and creates it if this is the first call in this frame.
        /// Returns -1 while recording is off, so nothing is stored. AddSubRecord and AddShape ignore an index of -1.
        /// </summary>
        /// <returns></returns>
        public int GetCurrentFrameRecord()
        {
            RecordManager manager = RecordManager.Instance;
            if (manager == null || !manager.ShouldRecord)
                return -1;
            if (records.Count == 0 || records[records.Count - 1].frameCount != Time.frameCount)
            {
                Record record = new Record();
                record.go = this.gameObject;
#if UNITY_6000_4_OR_NEWER
                record.id = unchecked((long)EntityId.ToULong(record.go.GetEntityId()));
#else
                record.id = record.go.GetInstanceID();
#endif
                record.frameTime = Time.time;
                record.frameCount = Time.frameCount;
                records.Add(record);
            }
            return records.Count - 1;
        }

        public void RemoveRecord(Record record)
        {
            records.Remove(record);
        }

        public void ClearRecords()
        {
            records.Clear();
        }

        public void AddShape(Shape shape, int index)
        {
            RecordManager manager = RecordManager.Instance;
            if (manager == null || !manager.ShouldRecord)
                return;
            if (index >= 0 && index < records.Count)
            {
                var rec = records[index];
                if (rec != null)
                {
                    rec.shapes.Add(shape);
                }
            }
        }
    }

    /// <summary>
    /// Each record holds all textual and shape information for a frame of a specific data recorder component
    /// </summary>
    public class Record
    {
        public List<SubRecord> subRecords;
        public GameObject go;
        public long id;
        public int frameCount;
        public float frameTime;
        public List<Shape> shapes;

        public Record()
        {
            subRecords = new List<SubRecord>();
            shapes = new List<Shape>();
        }

        public Record(string name, GameObject go, long id, int frameCount, float frameTime)
        {
            this.go = go;
            this.id = id;
            this.frameCount = frameCount;
            this.frameTime = frameTime;
            this.subRecords = new List<SubRecord>();
            this.shapes = new List<Shape>();
        }
    }

    public class SubRecord
    {
        public int indentationLevel;
        public string text;
        public Record record;
    }

    public struct Shape
    {
        public float size;
        public Color color;
        public ShapeType Type;
        public Vector3 position;
    }

    public enum ShapeType : byte
    {
        Sphere,
        Cube,
        WiredSphere,
        WiredCube
    }
}
