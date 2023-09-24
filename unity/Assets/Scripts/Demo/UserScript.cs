using AOT;
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace tcn
{
    public class UserScript : MonoBehaviour {
        [Tooltip("Enter the Subscriber Name.")]
        public string subscriber_name;
        [Tooltip("Enter the Zenoh Topic to Subscribe to.")]
        public string topic_name;
        private UnityAction<bool> haveSessionEvent;
        public GameObject Sphere;
        public String sphereUID = "Sphere";
        public GameObject Square;
        public String squareUID = "Square";
        public static string userUID = "user1";
        IDictionary<string, GameObject> objectsID2GameObjects = new Dictionary<string, GameObject>();
        IDictionary<string, Vector3> objectsID2Positions = new Dictionary<string, Vector3>();
        bool _mousePressed;
        static string _selectedObject = "";
        static GameObject _selectedGameObject;
        float frameRate = 0.04f;
        void OnEnable()
        {
            if (haveSessionEvent == null)
                haveSessionEvent = new UnityAction<bool>(SessionEventCallback);

            // Register to the event
            //
            tcn.hl2comm.RegisterForSessionEvent(haveSessionEvent);
        }

        void OnDisable()
        {
            // Unregister from the event when this object is disabled or destroyed
            //
            tcn.hl2comm.UnregisterSessionEvent(haveSessionEvent);
        }

        void Start(){
            objectsID2GameObjects.Add(sphereUID, Sphere);
            objectsID2GameObjects.Add(squareUID, Square);
            objectsID2Positions.Add(sphereUID, new Vector3(1.5f, 0f, 5));
            objectsID2Positions.Add(squareUID, new Vector3(1.5f, 0f, 5));
        }

        private void SessionEventCallback(bool status)
        {
            Debug.Log("Subscribing to topic: " + this.topic_name);
            RegisterRawZSubscriber(this.subscriber_name, this.topic_name, OnInternalMessageCallback);
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
            this.HandleMessage(s);

        }

        public virtual void HandleMessage(tcn.Sample sample)
        {
            UnityEngine.Debug.Log("received some message: " + sample.ValueToString());
            hl2comm.PutStr("demo/example/reply", sample.ValueToString() + " Replied.");
        }


    }
}
