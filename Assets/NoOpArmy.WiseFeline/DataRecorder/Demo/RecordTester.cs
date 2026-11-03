using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline.DataRecorder
{
    public class RecordTester : MonoBehaviour
    {
        private DataRecorder recorder;

        void Start()
        {
            recorder = GetComponent<DataRecorder>();
        }

        private void Update()
        {
            if (UnityEngine.Random.value < 0.999f)
                return;
            int index = recorder.GetCurrentFrameRecord();

            var sub0 = new SubRecord();
            sub0.text = $"Test";
            sub0.indentationLevel = 0;
            recorder.AddSubRecord(sub0, index);

            var sub = new SubRecord();
            sub.text = $"1";
            sub.indentationLevel = 1;
            recorder.AddSubRecord(sub, index);
            var sub2 = new SubRecord();
            sub2.text = $"2";
            sub2.indentationLevel = 1;
            recorder.AddSubRecord(sub2, index);

            var sub3 = new SubRecord();
            sub3.text = $"child 3";
            sub3.indentationLevel = 2;
            recorder.AddSubRecord(sub3, index);

            var sub4 = new SubRecord();
            sub4.text = $"child child 4";
            sub4.indentationLevel = 3;
            recorder.AddSubRecord(sub4, index);

            var sub7 = new SubRecord();
            sub7.text = $"<color=blue>child child</color> child 7";
            sub7.indentationLevel = 4;
            recorder.AddSubRecord(sub7, index);

            var sub8 = new SubRecord();
            sub8.text = $"child child 8";
            sub8.indentationLevel = 3;
            recorder.AddSubRecord(sub8, index);

            var sub6 = new SubRecord();
            sub6.text = $"child 6";
            sub6.indentationLevel = 2;
            recorder.AddSubRecord(sub6, index);

            var sub5 = new SubRecord();
            sub5.text = $"5";
            sub5.indentationLevel = 1;
            recorder.AddSubRecord(sub5, index);
            for (int i = 0; i < 5; ++i)
            {
                recorder.AddShape(new Shape
                {
                    position = UnityEngine.Random.insideUnitSphere * 5,
                    color = (UnityEngine.Random.value > 0.5) ? Color.blue : Color.yellow,
                    size = UnityEngine.Random.Range(0.3f, 1.2f),
                    Type = ShapeType.Sphere
                }, index);
            }
        }
    }
}
