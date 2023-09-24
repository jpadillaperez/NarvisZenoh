//******************************************************************************
// This class demonstrates how to receive and process messages from a client.
// For a sample client implementation see client_umq.py in the viewer directory.
//******************************************************************************
using AOT;
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

using Microsoft.MixedReality.Toolkit.UI;

namespace tcn
{
    public class HololensUser : MonoBehaviour
    {
        [Tooltip("Enter the Subscriber Name.")]
        private string subscriber_name = "subscriber";
        private UnityAction<bool> haveSessionEvent;
        public GameObject Sphere;
        public String sphereUID = "Sphere";
        public GameObject Square;
        public String squareUID = "Square";
        public string userUID;
        IDictionary<string, GameObject> objectsID2GameObjects = new Dictionary<string, GameObject>();
        IDictionary<string, Vector3> objectsID2Positions = new Dictionary<string, Vector3>();

        bool _mousePressed;
        string _selectedObject = "";
        GameObject _selectedGameObject;

        float frameRate = 1f;

        [Serializable]
        public struct sentMessage
        {
            public string Obj_id;
            public string User_id;
            public string Function;
            public List<float> Position;
        }

        [Serializable]
        public struct receivedMessage
        {
            public string User_id;
            public string Obj_id;
            public List<float> Position;
            public bool Active;
        }

        void OnEnable()
        {
            if (haveSessionEvent == null)
                haveSessionEvent = new UnityAction<bool>(SessionEventCallback);

            tcn.hl2comm.RegisterForSessionEvent(haveSessionEvent);
        }

        void OnDisable()
        {
            tcn.hl2comm.UnregisterSessionEvent(haveSessionEvent);
        }


        private void SessionEventCallback(bool status)
        {
            //Debug.Log("Subscribing to topic: ManagerRepliesStrings");
            RegisterRawZSubscriber(this.subscriber_name, "ManagerRepliesStrings", OnInternalMessageCallback);
        }

        void Start()
        {
            objectsID2GameObjects.Add(sphereUID, Sphere);
            objectsID2GameObjects.Add(squareUID, Square);
            objectsID2Positions.Add(sphereUID, new Vector3(1.5f, 0f, 5));
            objectsID2Positions.Add(squareUID, new Vector3(1.5f, 0f, 5));

            UserJoin();

            StartCoroutine(Interaction());
        }

        public void InteractionStarted(ManipulationEventData eventReceived)
        {
            Debug.Log("Grabbed Object");
            _selectedObject = eventReceived.ManipulationSource.transform.name;
            _selectedGameObject = eventReceived.ManipulationSource;
            createMessage("GrabObject", _selectedObject, userUID);
        }

        public void InteractionEnded(ManipulationEventData eventReceived)
        {
            Debug.Log("Released Object");
            _selectedObject = "";
            //_selectedGameObject = null;
            createMessage("ReleaseObject", eventReceived.ManipulationSource.transform.name, userUID);

        }

        private void UserJoin()
        {
            sentMessage msg = new sentMessage();
            msg.Obj_id = "";
            msg.Function = "UserJoin";
            //var sample = JsonConvert.SerializeObject(msg);
            string json = JsonConvert.SerializeObject(msg);

            print("Sent Message: " + json);
            hl2comm.PutStr("UserActionsStrings", json);
        }

        [DllImport(hl2comm.DllName, CallingConvention = CallingConvention.Cdecl)]
        static extern void RegisterRawZSubscriber(string name, string keyexpr, ZenohSubscriptionCallback cb);

        //Create string param callback delegate
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ZenohSubscriptionCallback(ref tcn.Sample.NativeType sample);
        [MonoPInvokeCallback(typeof(ZenohSubscriptionCallback))]
        internal void OnInternalMessageCallback(ref tcn.Sample.NativeType samplePtr)
        {
            tcn.Sample s = new tcn.Sample(samplePtr);
            string ss = s.ValueToString().Replace("\\", "");
            ss = ss.Replace(" ", "");
            ss = ss.Substring(1, ss.Length - 2);

            Debug.Log("Received Message: " + ss);

            this.ActivityReceived(JsonUtility.FromJson<receivedMessage>(ss));

        }

        private void createMessage(string function, string selected_obj, string user_id)
        {
            sentMessage msg = new sentMessage();
            msg.Function = function;
            msg.Obj_id = selected_obj;
            msg.User_id = user_id;

            Debug.Log("Sent Message: " + JsonConvert.SerializeObject(msg));

            hl2comm.PutStr("UserActionsStrings", JsonConvert.SerializeObject(msg));
        }

        private void createMessage(string function, string selected_obj, string user_id, Vector3 pose)
        {
            sentMessage msg = new sentMessage();
            msg.Function = function;
            msg.Obj_id = selected_obj;
            msg.User_id = user_id;
            msg.Position = new List<float>() { pose.x, pose.y, pose.z };

            Debug.Log("Sent Message: " + JsonConvert.SerializeObject(msg));

            hl2comm.PutStr("UserActionsStrings", JsonConvert.SerializeObject(msg));
        }

        IEnumerator Interaction()
        {
            while (true)
            {
                if (_selectedObject != "")
                {
                    Debug.Log("Sending position");
                    createMessage("ChangePosition", _selectedObject, userUID, _selectedGameObject.transform.position);
                }
                yield return new WaitForSeconds(frameRate);
            }
        }

        void ActivityReceived(receivedMessage msg)
        {
            Debug.Log("Is it true? " + objectsID2GameObjects.ContainsKey(msg.Obj_id));
            Debug.Log("Is it true? " + (msg.User_id != userUID));

            if ((userUID == "") && (msg.Obj_id == ""))
            {
                userUID = msg.User_id;
            }
            //else if (objectsID2GameObjects.ContainsKey(msg.Obj_id) && (msg.User_id != userUID))
            //{
            //    if (!msg.Active)
            //    {
            //        //...
            //    }
            //    else
            //    {
            //        Debug.Log("Received position");
            //        //objectsID2GameObjects[msg.Obj_id].transform.position = new Vector3(msg.Position[0], msg.Position[1], msg.Position[2]);
            //        //objectsID2Positions[msg.Obj_id] = new Vector3(msg.Position[0], msg.Position[1], msg.Position[2]);
            //    }
            //}
            //else
            //{
            //    //... Create objects
            //}
        }
    }
}
