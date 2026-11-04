using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoOpArmy.WiseFeline.BlackBoards;

namespace NoOpArmy.UtilityAI.Sample
{
    public class PlayWithBlackboard : MonoBehaviour
    {
        /// <summary>
        /// The blackboard to read/write to
        /// </summary>
        public BlackBoard blackboard;

        public BlackBoardChange colorChange;
        public BlackBoardChangeer changer;

        // Start is called before the first frame update
        IEnumerator Start()
        {
            //set the blackboard keys defined to random values once in a while
            while (true)
            {
                blackboard.SetVector3("position", new Vector3(Random.Range(-4, 4), Random.value * 2, Random.Range(-6, 2)));
                blackboard.SetVector3("color", new Vector3(Random.value, Random.value, Random.value));
                blackboard.SetFloat("speed", Random.Range(3, 5));
                yield return new WaitForSeconds(1.5f);
            }
        }

        // Update is called once per frame
        void Update()
        {
            //Read the blackboard values every frame and do something with them.
            //It doesn't have to be like this and you can have callbacks to know when to read or a bool which tells you the values changed and you need to read
            if (blackboard != null)
            {
                //set color to the value defined in the inspector if you press space
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    colorChange.ApplyToBlackboard(blackboard);
                }

                //changers change values over time based on their settings
                if (changer != null)
                    changer.Update(blackboard);
                Vector3 pos = blackboard.GetVector3("position");
                Vector3 color = blackboard.GetVector3("color");
                float speed = blackboard.GetFloat("speed");
                Color c = new Color(color.x, color.y, color.z);
                transform.position = Vector3.Slerp(transform.position, pos, speed * Time.deltaTime);//not the best way to move, we have it just for demoing purposes.
                GetComponent<MeshRenderer>().material.color = c;
            }

        }
    }
}
